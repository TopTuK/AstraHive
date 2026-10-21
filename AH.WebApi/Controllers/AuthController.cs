using System.Security.Claims;
using AH.WebApi.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Unleash;

namespace AH.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IAuthenticationSchemeProvider schemes,
    IOptions<CertificateLoginOptions> certificateLogin,
    IUnleash unleash,
    ILogger<AuthController> logger)
    : ControllerBase
{
    private const string DefaultReturnUrl = "/welcome";
    private const string CertificateLoginPath = "/api/auth/login/certificate";

    // Browser navigation target. On the app origin it hops to the certificate login port, whose TLS handshake
    // asks the browser for a client certificate; there the session cookie is issued (cookies ignore the port)
    // and the browser returns to the app origin. Takes precedence over login/{provider}.
    [HttpGet("login/certificate")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> LoginWithCertificate([FromQuery] string? returnUrl, [FromServices] CertificateSignIn certificateSignIn, CancellationToken cancellationToken)
    {
        var options = certificateLogin.Value;
        var localReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : DefaultReturnUrl;
        var appOrigin = options.AppOrigin?.TrimEnd('/') ?? string.Empty;

        if (!options.IsEnabled)
        {
            logger.LogWarning("Certificate sign-in requested, but it is not configured");
            return Redirect($"{appOrigin}/login?error={ExternalSignInException.CertificateUnavailable}");
        }

        if (HttpContext.Connection.LocalPort != options.LoginPort)
        {
            return Redirect($"https://{Request.Host.Host}:{options.LoginPort}{CertificateLoginPath}?returnUrl={Uri.EscapeDataString(localReturnUrl)}");
        }

        var result = await certificateSignIn.SignInAsync(HttpContext, cancellationToken);
        if (result.Principal is null)
        {
            return Redirect($"{appOrigin}/login?error={result.Error}");
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal);
        return Redirect($"{appOrigin}{localReturnUrl}");
    }

    // What the login page offers: the external providers that are configured and allowed by ah.external_login,
    // whether certificate sign-in is set up, and whether new users may register (ah.registration_enabled).
    [HttpGet("options")]
    [AllowAnonymous]
    [ProducesResponseType<SignInOptionsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SignInOptionsResponse>> Options()
    {
        List<string> providers = [];
        if (unleash.IsExternalLoginEnabled())
        {
            foreach (var provider in ExternalProviders.RouteNames)
            {
                if (ExternalProviders.TryGetScheme(provider, out var scheme) && await schemes.GetSchemeAsync(scheme) is not null)
                {
                    providers.Add(provider);
                }
            }
        }

        return new SignInOptionsResponse(providers, certificateLogin.Value.IsEnabled, unleash.IsRegistrationEnabled());
    }

    // Browser navigation target (not XHR): starts the OAuth challenge with the provider.
    [HttpGet("login/{provider}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(string provider, [FromQuery] string? returnUrl)
    {
        if (!ExternalProviders.TryGetScheme(provider, out var scheme) || await schemes.GetSchemeAsync(scheme) is null)
        {
            logger.LogWarning("Sign-in requested for provider {Provider}, which is unknown or not configured", provider);
            return Problem(title: "Unknown sign-in provider", detail: $"Provider '{provider}' is not configured.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!unleash.IsExternalLoginEnabled())
        {
            logger.LogWarning("{Provider} sign-in requested while {Flag} is off", scheme, SignInFeatures.ExternalLogin);
            return Redirect($"/login?error={ExternalSignInException.ExternalLoginDisabled}");
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

// ExternalProviders: route names for /api/auth/login/{provider} ("google", "yandex").
public sealed record SignInOptionsResponse(IReadOnlyList<string> ExternalProviders, bool Certificate, bool Registration);
