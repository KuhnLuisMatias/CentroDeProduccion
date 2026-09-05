using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Domain.Services;
using FluentValidation;
using ProduccionEntity = CentroDeProduccion.Domain.Entities.Produccion;

namespace CentroDeProduccion.Application.Features.Produccion.Commands.CreateProduccion;

/// <summary>
/// Creates a production run in Borrador state and seeds its editable consumption lines from the
/// recipe's OWN <c>receta.Insumos</c> (single level, NO BOM explosion): insumo lines are converted
/// to the insumo's consumption unit, sub-recipe lines are kept as <see cref="ProduccionInsumo.RecetaOrigenId"/>
/// lines whose finished product gets consumed at confirmation. The operator edits these lines
/// freely; stock is moved only at confirmation (<see cref="ConfirmProduccion.ConfirmProduccionCommandHandler"/>).
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

        // Load the direct insumos referenced by the recipe's own lines so each line can be
        // converted to the insumo's consumption unit (purchase-unit lines × FactorConversion).
        var insumoIds = receta.Insumos
            .Where(d => d.InsumoId.HasValue)
            .Select(d => d.InsumoId!.Value)
            .Distinct()
            .ToList();
        var insumos = (await _insumoRepository.GetByIdsAsync(insumoIds, cancellationToken))
            .ToDictionary(i => i.Id);

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

        // One consumption line per recipe line. Sub-recipe quantities stay in the sub-recipe's
        // result unit; enforcement of sub-PT existence/stock happens at confirmation.
        foreach (var detalle in receta.Insumos)
        {
            if (detalle.InsumoId.HasValue)
            {
                var cantidad = detalle.CantidadNecesaria;
                var insumo = insumos.GetValueOrDefault(detalle.InsumoId.Value);
                if (insumo is not null && detalle.UnidadMedidaId != insumo.UnidadConsumoId)
                {
                    if (detalle.UnidadMedidaId == insumo.UnidadCompraId)
                    {
                        cantidad *= insumo.FactorConversion;
                    }
                    else
                    {
                        return Result.Failure<CreateProduccionResponse>(Error.Validation(
                            "BOM_UNIT_INVALID",
                            $"La unidad de la línea de receta no coincide con las unidades del insumo {insumo.Nombre}"));
                    }
                }

                produccion.InsumosConsumidos.Add(new ProduccionInsumo
                {
                    InsumoId = detalle.InsumoId.Value,
                    Cantidad = cantidad,
                    Observaciones = null
                });
            }
            else if (detalle.RecetaOrigenId.HasValue)
            {
                if (detalle.RecetaOrigenId.Value == receta.Id)
                {
                    return Result.Failure<CreateProduccionResponse>(Error.Validation(
                        "BOM_SELF_REFERENCE",
                        $"La receta {receta.Nombre} no puede consumirse a sí misma"));
                }

                produccion.InsumosConsumidos.Add(new ProduccionInsumo
                {
                    RecetaOrigenId = detalle.RecetaOrigenId.Value,
                    Cantidad = detalle.CantidadNecesaria, // whole batches of the sub-recipe
                    Observaciones = null
                });
            }
        }

        await _produccionRepository.AddAsync(produccion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateProduccionResponse(produccion.Id, produccion.Estado);
    }
}
