using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.Auth.Commands.Register;
using CentroDeProduccion.Application.Features.Auth.Commands.Bootstrap;
using CentroDeProduccion.Application.Features.Auth.Commands.Login;
using CentroDeProduccion.Application.Features.Auth.Commands.Refresh;
using CentroDeProduccion.Application.Features.Auth.Commands.ChangePassword;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RegisterCommandHandler _registerHandler;
    private readonly BootstrapCommandHandler _bootstrapHandler;
    private readonly LoginCommandHandler _loginHandler;
    private readonly RefreshTokenCommandHandler _refreshTokenHandler;
    private readonly ChangePasswordCommandHandler _changePasswordHandler;
    private readonly ICurrentUser _currentUser;

    public AuthController(
        RegisterCommandHandler registerHandler,
        BootstrapCommandHandler bootstrapHandler,
        LoginCommandHandler loginHandler,
        RefreshTokenCommandHandler refreshTokenHandler,
        ChangePasswordCommandHandler changePasswordHandler,
        ICurrentUser currentUser)
    {
        _registerHandler = registerHandler;
        _bootstrapHandler = bootstrapHandler;
        _loginHandler = loginHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _changePasswordHandler = changePasswordHandler;
        _currentUser = currentUser;
    }

    /// <summary>Design D3 bootstrap: creates the first administrator while the system has no users.</summary>
    [AllowAnonymous]
    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap([FromBody] BootstrapCommand command, CancellationToken cancellationToken)
    {
        var result = await _bootstrapHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    /// <summary>Admin-only account creation (spec §2.1/§2.2).</summary>
    [Authorize(Roles = "Administrador")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _registerHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _loginHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _refreshTokenHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.UsuarioId.HasValue)
            return Unauthorized();

        var result = await _changePasswordHandler.HandleAsync(command, _currentUser.UsuarioId.Value, cancellationToken);
        return result.ToActionResult(this);
    }
}
