using System.Security.Claims;
using AH.WebApi.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AH.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthenticationSchemeProvider schemes, ILogger<AuthController> logger) : ControllerBase
{
    private const string DefaultReturnUrl = "/welcome";

    // Browser navigation target (not XHR): starts the OAuth challenge with the provider.
    [HttpGet("login/{provider}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(string provider, [FromQuery] string? returnUrl)
    {
        if (!ExternalProviders.TryGetScheme(provider, out var scheme) || await schemes.GetSchemeAsync(scheme) is null)
        {
            logger.LogWarning("Sign-in requested for provider {Provider}, which is unknown or not configured", provider);
            return Problem(title: "Unknown sign-in provider", detail: $"Provider '{provider}' is not configured.", statusCode: StatusCodes.Status400BadRequest);
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : DefaultReturnUrl,
        };
        logger.LogInformation(
            "Starting {Provider} sign-in from {Origin}, return URL {ReturnUrl}",
            scheme,
            $"{Request.Scheme}://{Request.Host}",
            properties.RedirectUri);
        return Challenge(properties, scheme);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        if (!User.TryGetUserId(out var id))
        {
            return Unauthorized();
        }

        return new CurrentUserResponse(
            id,
            User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            User.FindFirstValue(AstraHiveClaimTypes.Picture));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}

public sealed record CurrentUserResponse(int Id, string Email, string FirstName, string LastName, string Name, string? AvatarUrl);
