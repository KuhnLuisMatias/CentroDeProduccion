using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CentroDeProduccion.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<Usuario> _hasher = new();

    public string Hash(string password)
        => _hasher.HashPassword(null!, password);

    public bool Verify(string hash, string password)
        => _hasher.VerifyHashedPassword(null!, hash, password) == PasswordVerificationResult.Success;
}
