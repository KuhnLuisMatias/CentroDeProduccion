using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries.GetSaldo;

public sealed record GetSaldoQuery(Guid BarId);

public class GetSaldoQueryHandler
{
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteRepository;
    private readonly IBarRepository _barRepository;

    public GetSaldoQueryHandler(
        ICuentaCorrienteBarRepository cuentaCorrienteRepository,
        IBarRepository barRepository)
    {
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _barRepository = barRepository;
    }

    public async Task<Result<decimal>> HandleAsync(GetSaldoQuery query, CancellationToken cancellationToken = default)
    {
        var bar = await _barRepository.GetByIdAsync(query.BarId, cancellationToken);
        if (bar == null)
        {
            return Result.Failure<decimal>(Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        var saldo = await _cuentaCorrienteRepository.GetSaldoAsync(query.BarId, cancellationToken);
        return Result.Success(saldo);
    }
}