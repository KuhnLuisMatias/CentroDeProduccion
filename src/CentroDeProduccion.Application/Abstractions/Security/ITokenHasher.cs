namespace CentroDeProduccion.Application.Abstractions.Security;

/// <summary>
/// Hashes opaque refresh-token secrets for storage (D4: only <c>TokenHash</c> is persisted,
/// the plaintext value is returned to the client once and never stored).
/// </summary>
public interface ITokenHasher
{
    string Hash(string plainTextToken);
}
