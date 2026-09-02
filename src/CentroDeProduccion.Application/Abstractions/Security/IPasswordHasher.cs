namespace CentroDeProduccion.Application.Abstractions.Security;

/// <summary>
/// Hashes and verifies user passwords. Implemented in Infrastructure via
/// <c>Microsoft.Extensions.Identity.Core</c>'s <c>PasswordHasher&lt;Usuario&gt;</c> (see D2 in
/// the design doc) — not full ASP.NET Core Identity.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>True if <paramref name="password"/> matches <paramref name="hash"/>.</summary>
    bool Verify(string hash, string password);
}
