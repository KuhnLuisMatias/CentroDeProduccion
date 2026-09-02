using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Middleware;

/// <summary>
/// Design D3 gate: while a user has <c>DebeCambiarPassword = true</c>, every authenticated
/// endpoint except <c>POST /api/auth/change-password</c> returns 403. The flag is carried in
/// the <c>debe_cambiar_password</c> claim issued at login/refresh; after a password change all
/// refresh tokens are revoked, so the user must re-authenticate and receive a fresh claim.
/// </summary>
public class DebeCambiarPasswordMiddleware
{
    private const string ClaimName = "debe_cambiar_password";
    private const string ExemptPath = "/api/auth/change-password";

    private readonly RequestDelegate _next;

    public DebeCambiarPasswordMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;

        if (user.Identity?.IsAuthenticated == true &&
            !context.Request.Path.StartsWithSegments(ExemptPath, StringComparison.OrdinalIgnoreCase))
        {
            var claim = user.FindFirst(ClaimName);
            if (claim is not null && string.Equals(claim.Value, "true", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "PASSWORD_CHANGE_REQUIRED",
                    Detail = "Debe cambiar su contraseña antes de continuar.",
                    Extensions = { ["errorCode"] = "PASSWORD_CHANGE_REQUIRED" }
                });
                return;
            }
        }

        await _next(context);
    }
}
