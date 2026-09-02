using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using ProduccionEntity = CentroDeProduccion.Domain.Entities.Produccion;
using RecetaEntity = CentroDeProduccion.Domain.Entities.Receta;

namespace CentroDeProduccion.Application.Features.Produccion.Commands.CreateProduccion;

/// <summary>
/// Creates a production run in Borrador state and seeds its editable consumption lines from the
/// recipe BOM (flattened via <see cref="CostoService.ExplosionarInsumos"/>, already converted to
/// each insumo's consumption unit). The operator edits these lines freely; stock is moved only
/// at confirmation (<see cref="ConfirmProduccion.ConfirmProduccionCommandHandler"/>).
/// </summary>
public class CreateProduccionCommandHandler
{
    private readonly IProduccionRepository _produccionRepository;
    private readonly IRecetaRepository _recetaRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateProduccionCommand> _validator;

    public CreateProduccionCommandHandler(
        IProduccionRepository produccionRepository,
        IRecetaRepository recetaRepository,
        IInsumoRepository insumoRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateProduccionCommand> validator)
    {
        _produccionRepository = produccionRepository;
        _recetaRepository = recetaRepository;
        _insumoRepository = insumoRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<CreateProduccionResponse>> HandleAsync(CreateProduccionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateProduccionResponse>(errors.First());
        }

        var receta = await _recetaRepository.GetByIdWithDetallesAsync(command.RecetaId, cancellationToken);
        if (receta == null || !receta.Activo || receta.Estado != EstadoReceta.Activa)
        {
            return Result.Failure<CreateProduccionResponse>(
                Error.NotFound("RECETA_NOT_FOUND", "Receta no encontrada o inactiva"));
        }

        // Load the recipe tree (sub-recipes) for BOM explosion.
        var recetas = new Dictionary<Guid, RecetaEntity>();
        await CargarArbolAsync(receta, recetas, new HashSet<Guid>(), cancellationToken);

        // Load every direct insumo referenced by the recipe tree up-front so ExplosionarInsumos
        // can convert each line to the insumo's consumption unit (purchase-unit lines × FactorConversion).
        var insumoIds = recetas.Values
            .SelectMany(r => r.Insumos)
            .Where(d => d.InsumoId.HasValue)
            .Select(d => d.InsumoId!.Value)
            .Distinct()
            .ToList();
        var insumos = await _insumoRepository.GetByIdsAsync(insumoIds, cancellationToken);
        var insumoDict = insumos.ToDictionary(i => i.Id);

        Dictionary<Guid, decimal> cantidades;
        try
        {
            cantidades = CostoService.ExplosionarInsumos(
                receta,
                id => recetas.TryGetValue(id, out var r) ? r : null,
                id => insumoDict.GetValueOrDefault(id));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CreateProduccionResponse>(Error.Validation("BOM_UNIT_INVALID", ex.Message));
        }

        var produccion = new ProduccionEntity
        {
            Id = Guid.NewGuid(),
            RecetaId = command.RecetaId,
            Lote = string.Empty, // assigned on confirmation
            Fecha = RelojDeNegocio.Ahora,
            ResponsableId = _currentUser.UsuarioId!.Value,
            Estado = EstadoProduccion.Borrador,
            Observaciones = command.Observaciones
        };

        foreach (var (insumoId, cantidadNecesaria) in cantidades)
        {
            produccion.InsumosConsumidos.Add(new ProduccionInsumo
            {
                Id = Guid.NewGuid(),
                ProduccionId = produccion.Id,
                InsumoId = insumoId,
                Cantidad = cantidadNecesaria,
                Observaciones = null
            });
        }

        await _produccionRepository.AddAsync(produccion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateProduccionResponse(produccion.Id, produccion.Estado);
    }

    private async Task CargarArbolAsync(
        RecetaEntity receta,
        Dictionary<Guid, RecetaEntity> recetas,
        HashSet<Guid> visitados,
        CancellationToken cancellationToken)
    {
        recetas[receta.Id] = receta;

        foreach (var detalle in receta.Insumos)
        {
            if (detalle.RecetaOrigenId.HasValue && visitados.Add(detalle.RecetaOrigenId.Value))
            {
                var subReceta = await _recetaRepository.GetByIdWithDetallesAsync(detalle.RecetaOrigenId.Value, cancellationToken);
                if (subReceta is not null)
                {
                    await CargarArbolAsync(subReceta, recetas, visitados, cancellationToken);
                }
            }
        }
    }
}
