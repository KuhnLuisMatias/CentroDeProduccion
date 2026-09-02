using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Rol Rol { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }

    // NOTE: CentroProduccionNombre dropped (user decision — single-tenant confirmed). A
    // per-user string was never a real tenancy mechanism. The production-center display name
    // needs a home in a future configuration module (spec §12) — do NOT add a `configuracion`
    // table in this change; that is separate scope.

    /// <summary>Design D3: forces the change-password flow after admin bootstrap or an
    /// admin-issued reset. While true, every endpoint except POST /api/auth/change-password
    /// returns 403.</summary>
    public bool DebeCambiarPassword { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Refresh tokens
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
