using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Produccion.Commands.EditarInsumosProduccion;

/// <summary>
/// Replaces the full consumption list of a Borrador production run (Producción simple: the
/// operator freely adds, removes, or edits quantities). Lines may reference any active insumo or
/// any active sub-recipe (whose finished product is consumed at confirmation). Confirmation
/// deducts exactly these lines.
/// </summary>
public class EditarInsumosProduccionCommandHandler
{
    private readonly IProduccionRepository _produccionRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IRecetaRepository _recetaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<EditarInsumosProduccionCommand> _validator;

    public EditarInsumosProduccionCommandHandler(
        IProduccionRepository produccionRepository,
        IInsumoRepository insumoRepository,
        IRecetaRepository recetaRepository,
        IUnitOfWork unitOfWork,
        IValidator<EditarInsumosProduccionCommand> validator)
    {
        _produccionRepository = produccionRepository;
        _insumoRepository = insumoRepository;
        _recetaRepository = recetaRepository;
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

        // Full replace: deduplicate identical origins by summing their quantities.
        var cantidadesPorInsumo = new Dictionary<Guid, decimal>();
        var cantidadesPorReceta = new Dictionary<Guid, decimal>();
        var observacionesPorInsumo = new Dictionary<Guid, string?>();
        var observacionesPorReceta = new Dictionary<Guid, string?>();

        foreach (var linea in command.Lineas)
        {
            if (linea.InsumoId.HasValue)
            {
                var id = linea.InsumoId.Value;
                cantidadesPorInsumo[id] = cantidadesPorInsumo.GetValueOrDefault(id) + linea.Cantidad;
                if (!string.IsNullOrWhiteSpace(linea.Observaciones))
                {
                    observacionesPorInsumo[id] = linea.Observaciones;
                }
            }
            else if (linea.RecetaOrigenId.HasValue)
            {
                var id = linea.RecetaOrigenId.Value;
                if (id == produccion.RecetaId)
                {
                    return Result.Failure<EditarInsumosProduccionResponse>(Error.Validation(
                        "BOM_SELF_REFERENCE",
                        "La producción no puede consumir la subreceta de la que forma parte"));
                }

                cantidadesPorReceta[id] = cantidadesPorReceta.GetValueOrDefault(id) + linea.Cantidad;
                if (!string.IsNullOrWhiteSpace(linea.Observaciones))
                {
                    observacionesPorReceta[id] = linea.Observaciones;
                }
            }
        }

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

        foreach (var recetaId in cantidadesPorReceta.Keys)
        {
            var subReceta = await _recetaRepository.GetByIdAsync(recetaId, cancellationToken);
            if (subReceta == null || !subReceta.Activo)
            {
                return Result.Failure<EditarInsumosProduccionResponse>(
                    Error.NotFound("RECETA_NOT_FOUND", $"Subreceta {recetaId} no encontrada o inactiva"));
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

        foreach (var (recetaId, cantidad) in cantidadesPorReceta)
        {
            produccion.InsumosConsumidos.Add(new ProduccionInsumo
            {
                RecetaOrigenId = recetaId,
                Cantidad = cantidad,
                Observaciones = observacionesPorReceta.GetValueOrDefault(recetaId)
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EditarInsumosProduccionResponse(produccion.Id, produccion.InsumosConsumidos.Count);
    }
}
