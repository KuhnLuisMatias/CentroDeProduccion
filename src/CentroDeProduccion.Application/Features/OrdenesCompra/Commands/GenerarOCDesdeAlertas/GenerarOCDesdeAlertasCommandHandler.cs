using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.GenerarOCDesdeAlertas;

/// <summary>
/// Generates one Borrador OrdenCompra per ProveedorPrincipal from the selected stock-alert
/// insumos (StockActual &lt;= StockMinimo). Suggested quantity = StockMinimo - StockActual
/// (min 1); price = PrecioUltimaCompra (0 when unknown).
/// </summary>
public class GenerarOCDesdeAlertasCommandHandler
{
    private readonly IInsumoRepository _insumoRepository;
    private readonly IOrdenCompraRepository _ordenCompraRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<GenerarOCDesdeAlertasCommand> _validator;

    public GenerarOCDesdeAlertasCommandHandler(
        IInsumoRepository insumoRepository,
        IOrdenCompraRepository ordenCompraRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<GenerarOCDesdeAlertasCommand> validator)
    {
        _insumoRepository = insumoRepository;
        _ordenCompraRepository = ordenCompraRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<GenerarOCDesdeAlertasResponse>> HandleAsync(
        GenerarOCDesdeAlertasCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<GenerarOCDesdeAlertasResponse>(errors.First());
        }

        var insumos = await _insumoRepository.GetByIdsAsync(command.InsumoIds.Distinct().ToList(), cancellationToken);
        var insumoDict = insumos.ToDictionary(i => i.Id);

        foreach (var insumoId in command.InsumoIds.Distinct())
        {
            if (!insumoDict.TryGetValue(insumoId, out var insumo) || !insumo.Activo)
            {
                return Result.Failure<GenerarOCDesdeAlertasResponse>(
                    Error.NotFound("INSUMO_NOT_FOUND", $"Insumo {insumoId} no encontrado o inactivo"));
            }

            if (insumo.StockActual > insumo.StockMinimo)
            {
                return Result.Failure<GenerarOCDesdeAlertasResponse>(
                    Error.Validation("INSUMO_SIN_ALERTA", $"El insumo {insumo.Nombre} no está por debajo del stock mínimo"));
            }

            if (!insumo.ProveedorPrincipalId.HasValue)
            {
                return Result.Failure<GenerarOCDesdeAlertasResponse>(
                    Error.Validation("SIN_PROVEEDOR_PRINCIPAL", $"El insumo {insumo.Nombre} no tiene proveedor principal asignado"));
            }
        }

        var grupos = command.InsumoIds.Distinct()
            .Select(id => insumoDict[id])
            .GroupBy(i => i.ProveedorPrincipalId!.Value);

        // Pending adds are not visible to Max() before SaveChanges, so compute the
        // starting numero once and assign sequential numbers to avoid duplicates.
        var numeroBase = await _ordenCompraRepository.GetNextNumeroAsync(cancellationToken);

        var ordenes = new List<OrdenCompra>();
        var numeroSiguiente = numeroBase;
        foreach (var grupo in grupos)
        {
            var ordenCompra = new OrdenCompra
            {
                Id = Guid.NewGuid(),
                Numero = numeroSiguiente++,
                ProveedorId = grupo.Key,
                Estado = EstadoOrdenCompra.Borrador,
                FechaCreacion = RelojDeNegocio.Ahora,
                Observaciones = "Generada automáticamente desde alertas de stock",
                CreadoPor = _currentUser.UsuarioId!.Value,
                Items = grupo.Select(insumo => new OrdenCompraItem
                {
                    Id = Guid.NewGuid(),
                    InsumoId = insumo.Id,
                    CantidadPedida = Math.Max(insumo.StockMinimo - insumo.StockActual, 1),
                    PrecioUnitario = insumo.PrecioUltimaCompra
                }).ToList()
            };

            await _ordenCompraRepository.AddAsync(ordenCompra, cancellationToken);
            ordenes.Add(ordenCompra);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = ordenes.Select(oc => new OrdenCompraGeneradaResponse(
            oc.Id,
            oc.Numero,
            oc.ProveedorId,
            oc.Proveedor?.NombreRazonSocial ?? string.Empty,
            oc.Items.Count,
            oc.Estado)).ToList();

        return Result.Success(new GenerarOCDesdeAlertasResponse(response));
    }
}