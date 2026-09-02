using System.Security.Claims;
using CentroDeProduccion.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace CentroDeProduccion.Tests.Api.Middleware;

/// <summary>
/// Verifies the Design D3 gate: a user with <c>debe_cambiar_password=true</c> is blocked (403)
/// from every authenticated endpoint except <c>POST /api/auth/change-password</c>.
/// </summary>
public class DebeCambiarPasswordMiddlewareTests
{
    private static DefaultHttpContext CreateContext(bool authenticated, bool debeCambiar)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        if (authenticated)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            if (debeCambiar)
                claims.Add(new Claim("debe_cambiar_password", "true"));

            var identity = new ClaimsIdentity(claims, "Test");
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }

    private static async Task<(int statusCode, string body)> RunAsync(DefaultHttpContext context, string path)
    {
        context.Request.Path = path;
        var middleware = new DebeCambiarPasswordMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task Invoke_DebeCambiarTrue_BlocksEndpointWith403()
    {
        var context = CreateContext(authenticated: true, debeCambiar: true);

        var (statusCode, body) = await RunAsync(context, "/api/insumos");

        statusCode.ShouldBe(403);
        body.ShouldContain("PASSWORD_CHANGE_REQUIRED");
    }

    [Fact]
    public async Task Invoke_DebeCambiarTrue_AllowsChangePasswordEndpoint()
    {
        var context = CreateContext(authenticated: true, debeCambiar: true);

        var (statusCode, _) = await RunAsync(context, "/api/auth/change-password");

        statusCode.ShouldBe(200);
    }

    [Fact]
    public async Task Invoke_DebeCambiarFalse_AllowsEndpoint()
    {
        var context = CreateContext(authenticated: true, debeCambiar: false);

        var (statusCode, _) = await RunAsync(context, "/api/insumos");

        statusCode.ShouldBe(200);
    }

    [Fact]
    public async Task Invoke_Unauthenticated_PassesThrough()
    {
        var context = CreateContext(authenticated: false, debeCambiar: false);

        var (statusCode, _) = await RunAsync(context, "/api/auth/login");

        statusCode.ShouldBe(200);
    }
}
