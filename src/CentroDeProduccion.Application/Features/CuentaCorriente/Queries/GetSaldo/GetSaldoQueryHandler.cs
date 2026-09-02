using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Queries.GetSaldo;

public sealed record GetSaldoQuery(Guid ProveedorId);

public class GetSaldoQueryHandler
{
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository;
    private readonly IProveedorRepository _proveedorRepository;

    public GetSaldoQueryHandler(
        ICuentaCorrienteProveedorRepository cuentaCorrienteRepository,
        IProveedorRepository proveedorRepository)
    {
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _proveedorRepository = proveedorRepository;
    }

    public async Task<Result<decimal>> HandleAsync(GetSaldoQuery query, CancellationToken cancellationToken = default)
    {
        var proveedor = await _proveedorRepository.GetByIdAsync(query.ProveedorId, cancellationToken);
        if (proveedor == null)
        {
            return Result.Failure<decimal>(Error.NotFound("PROVEEDOR_NOT_FOUND", "Proveedor no encontrado"));
        }

        var saldo = await _cuentaCorrienteRepository.GetSaldoAsync(query.ProveedorId, cancellationToken);
        return Result.Success(saldo);
    }
}