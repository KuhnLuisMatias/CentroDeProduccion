namespace CentroDeProduccion.Application.Abstractions.Security;

/// <summary>Reads the authenticated caller's identity from the current HTTP request's claims.</summary>
public interface ICurrentUser
{
    Guid? UsuarioId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
