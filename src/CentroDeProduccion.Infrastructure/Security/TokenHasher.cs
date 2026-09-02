using System.Security.Cryptography;
using System.Text;
using CentroDeProduccion.Application.Abstractions.Security;

namespace CentroDeProduccion.Infrastructure.Security;

public class TokenHasher : ITokenHasher
{
    public string Hash(string plainTextToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
