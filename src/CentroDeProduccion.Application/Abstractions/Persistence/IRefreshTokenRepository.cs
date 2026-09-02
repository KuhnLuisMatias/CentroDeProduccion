using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> GetByFamiliaIdAsync(Guid familiaId, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllByUsuarioIdAsync(Guid usuarioId, string motivo, CancellationToken cancellationToken = default);
}
