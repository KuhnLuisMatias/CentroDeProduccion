namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// Design D4: rotation with family-wide reuse detection. Only the SHA-256 hash of the opaque
/// token value is ever persisted — the plaintext is returned to the client once and never
/// stored. Reuse of an already-rotated token revokes every row sharing <see cref="FamiliaId"/>.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime FechaExpiracion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool Revocado { get; set; }

    /// <summary>Constant across an entire rotation chain; reuse detection revokes by this id.</summary>
    public Guid FamiliaId { get; set; }

    /// <summary>Self-FK to the token that replaced this one after rotation, if any.</summary>
    public Guid? ReemplazadoPorId { get; set; }
    public RefreshToken? ReemplazadoPor { get; set; }

    public DateTime? FechaRevocacion { get; set; }
    public string? MotivoRevocacion { get; set; }
}
