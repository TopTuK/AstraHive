using AH.IdentityService;
using AH.WebApi.Auth;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AH.WebApi.Controllers;

// Client certificates of main-workspace members, managed by the Global Administrator (IdentityService checks
// that on every call). The PFX password is in the issue response only; downloads return the same PFX.
[ApiController]
[Route("api/workspaces/{workspaceId:int}/members/{userId:int}/certificates")]
[Authorize]
public sealed class CertificatesController(CertificateServiceRpc.CertificateServiceRpcClient certificates) : ControllerBase
{
    private const string PfxContentType = "application/x-pkcs12";

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CertificateResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ActionResult<IReadOnlyList<CertificateResponse>>> GetAll(int workspaceId, int userId, CancellationToken cancellationToken) =>
        CallAsync(
            async requesterId => ToResponse(await certificates.GetUserCertificatesAsync(
                new UserCertificatesRequest { WorkspaceId = workspaceId, RequesterId = requesterId, UserId = userId },
                cancellationToken: cancellationToken)),
            "Cannot load the certificates");

    [HttpPost]
    [ProducesResponseType<IssuedCertificateResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IssuedCertificateResponse>> Issue(int workspaceId, int userId, CancellationToken cancellationToken)
    {
        var result = await CallAsync(
            async requesterId =>
            {
                var reply = await certificates.IssueCertificateAsync(
                    new UserCertificatesRequest { WorkspaceId = workspaceId, RequesterId = requesterId, UserId = userId },
                    cancellationToken: cancellationToken);
                return new IssuedCertificateResponse(ToResponse(reply.Certificate), reply.Password);
            },
            "Cannot issue the certificate");

        return result.Value is { } issued
            ? CreatedAtAction(nameof(Download), new { workspaceId, userId, certificateId = issued.Certificate.Id }, issued)
            : result;
    }

    [HttpGet("{certificateId:int}/download")]
    [Produces(PfxContentType)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, PfxContentType)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int workspaceId, int userId, int certificateId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var requesterId))
        {
            return Unauthorized();
        }

        try
        {
            var file = await certificates.DownloadCertificateAsync(
                new CertificateRequest { WorkspaceId = workspaceId, RequesterId = requesterId, UserId = userId, CertificateId = certificateId },
                cancellationToken: cancellationToken);
            return File(file.Content.ToByteArray(), PfxContentType, file.FileName);
        }
        catch (RpcException ex) when (RpcProblems.IsExpected(ex))
        {
            return this.ToProblem(ex, "Cannot download the certificate");
        }
    }

    // Answers with the member's remaining certificates (the revoked one included, marked revoked).
    [HttpDelete("{certificateId:int}")]
    [ProducesResponseType<IReadOnlyList<CertificateResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<IReadOnlyList<CertificateResponse>>> Revoke(int workspaceId, int userId, int certificateId, CancellationToken cancellationToken) =>
        CallAsync(
            async requesterId => ToResponse(await certificates.RevokeCertificateAsync(
                new CertificateRequest { WorkspaceId = workspaceId, RequesterId = requesterId, UserId = userId, CertificateId = certificateId },
                cancellationToken: cancellationToken)),
            "Cannot revoke the certificate");

    private async Task<ActionResult<T>> CallAsync<T>(Func<int, Task<T>> call, string problemTitle)
    {
        if (!User.TryGetUserId(out var requesterId))
        {
            return Unauthorized();
        }

        try
        {
            return await call(requesterId);
        }
        catch (RpcException ex) when (RpcProblems.IsExpected(ex))
        {
            return this.ToProblem(ex, problemTitle);
        }
    }

    private static IReadOnlyList<CertificateResponse> ToResponse(CertificateListReply reply) => [.. reply.Certificates.Select(ToResponse)];

    private static CertificateResponse ToResponse(Certificate certificate) => new(
        certificate.Id,
        certificate.UserId,
        certificate.Thumbprint,
        certificate.SerialNumber,
        certificate.NotBefore.ToDateTimeOffset(),
        certificate.NotAfter.ToDateTimeOffset(),
        certificate.IssuedAt.ToDateTimeOffset(),
        certificate.RevokedAt?.ToDateTimeOffset(),
        certificate.CanDownload);
}

public sealed record CertificateResponse(
    int Id,
    int UserId,
    string Thumbprint,
    string SerialNumber,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    DateTimeOffset IssuedAt,
    DateTimeOffset? RevokedAt,
    bool CanDownload);

// Password opens the PFX; it is shown once and not stored.
public sealed record IssuedCertificateResponse(CertificateResponse Certificate, string Password);
