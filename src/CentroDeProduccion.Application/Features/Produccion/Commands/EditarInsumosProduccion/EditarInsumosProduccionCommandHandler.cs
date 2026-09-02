using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Produccion.Commands.EditarInsumosProduccion;

/// <summary>
/// Replaces the full insumo-consumption list of a Borrador production run (Producción simple:
/// the operator freely adds, removes, or edits quantities). Confirmation deducts exactly these
/// lines. Lines may reference any active insumo, not only the recipe's template.
/// </summary>
public class EditarInsumosProduccionCommandHandler
{
    private readonly IProduccionRepository _produccionRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<EditarInsumosProduccionCommand> _validator;

    public EditarInsumosProduccionCommandHandler(
        IProduccionRepository produccionRepository,
        IInsumoRepository insumoRepository,
        IUnitOfWork unitOfWork,
        IValidator<EditarInsumosProduccionCommand> validator)
    {
        _produccionRepository = produccionRepository;
        _insumoRepository = insumoRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<EditarInsumosProduccionResponse>> HandleAsync(EditarInsumosProduccionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<EditarInsumosProduccionResponse>(errors.First());
        }

        var produccion = await _produccionRepository.GetByIdWithSalidasAsync(command.ProduccionId, cancellationToken);
        if (produccion == null)
        {
            return Result.Failure<EditarInsumosProduccionResponse>(Error.NotFound("PRODUCCION_NOT_FOUND", "Producción no encontrada"));
        }

        if (produccion.Estado != EstadoProduccion.Borrador)
        {
            return Result.Failure<EditarInsumosProduccionResponse>(
                Error.Conflict("PRODUCCION_NO_EDITABLE", "Solo se pueden editar los insumos de una producción en estado borrador"));
        }

        // Full replace: deduplicate identical insumo ids by summing their quantities.
        var cantidadesPorInsumo = new Dictionary<Guid, decimal>();
        foreach (var linea in command.Lineas)
        {
            cantidadesPorInsumo[linea.InsumoId] = cantidadesPorInsumo.GetValueOrDefault(linea.InsumoId) + linea.Cantidad;
        }

        var observacionesPorInsumo = command.Lineas
            .Where(l => !string.IsNullOrWhiteSpace(l.Observaciones))
            .GroupBy(l => l.InsumoId)
            .ToDictionary(g => g.Key, g => g.Last().Observaciones);

        var insumos = await _insumoRepository.GetByIdsAsync(cantidadesPorInsumo.Keys.ToList(), cancellationToken);
        var insumoDict = insumos.ToDictionary(i => i.Id);
        foreach (var insumoId in cantidadesPorInsumo.Keys)
        {
            if (!insumoDict.TryGetValue(insumoId, out var insumo) || !insumo.Activo)
            {
                return Result.Failure<EditarInsumosProduccionResponse>(
                    Error.NotFound("INSUMO_NOT_FOUND", $"Insumo {insumoId} no encontrado o inactivo"));
            }
        }

        produccion.InsumosConsumidos.Clear();
        foreach (var (insumoId, cantidad) in cantidadesPorInsumo)
        {
            // No explicit Id/FK here: EF propagates the generated key and the FK when the line
            // is discovered through the tracked parent's collection (a preset Guid key would
            // make EF treat the new line as an existing row → UPDATE of a non-existent row).
            produccion.InsumosConsumidos.Add(new ProduccionInsumo
            {
                InsumoId = insumoId,
                Cantidad = cantidad,
                Observaciones = observacionesPorInsumo.GetValueOrDefault(insumoId)
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EditarInsumosProduccionResponse(produccion.Id, produccion.InsumosConsumidos.Count);
    }
}
