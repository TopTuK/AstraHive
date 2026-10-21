using AH.Application.Certificates;
using AH.Application.Certificates.DownloadCertificate;
using AH.Application.Certificates.GetUserCertificates;
using AH.Application.Certificates.IssueCertificate;
using AH.Application.Certificates.RevokeCertificate;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AH.IdentityService.Services;

public sealed class RpcCertificateService(
    IssueCertificateHandler issueCertificate,
    GetUserCertificatesHandler getUserCertificates,
    DownloadCertificateHandler downloadCertificate,
    RevokeCertificateHandler revokeCertificate,
    ILogger<RpcCertificateService> logger)
    : CertificateServiceRpc.CertificateServiceRpcBase
{
    public override async Task<IssueCertificateReply> IssueCertificate(UserCertificatesRequest request, ServerCallContext context)
    {
        var command = new IssueCertificateCommand(request.WorkspaceId, request.RequesterId, request.UserId);

        return (await issueCertificate.HandleAsync(command, context.CancellationToken)).Unwrap(
            issued => new IssueCertificateReply { Certificate = ToProto(issued.Certificate), Password = issued.Password },
            logger,
            nameof(IssueCertificate));
    }

    public override async Task<CertificateListReply> GetUserCertificates(UserCertificatesRequest request, ServerCallContext context)
    {
        var query = new GetUserCertificatesQuery(request.WorkspaceId, request.RequesterId, request.UserId);

        return (await getUserCertificates.HandleAsync(query, context.CancellationToken)).Unwrap(ToReply, logger, nameof(GetUserCertificates));
    }

    public override async Task<CertificateFileReply> DownloadCertificate(CertificateRequest request, ServerCallContext context)
    {
        var query = new DownloadCertificateQuery(request.WorkspaceId, request.RequesterId, request.UserId, request.CertificateId);

        return (await downloadCertificate.HandleAsync(query, context.CancellationToken)).Unwrap(
            file => new CertificateFileReply { FileName = file.FileName, Content = ByteString.CopyFrom(file.Content) },
            logger,
            nameof(DownloadCertificate));
    }

    public override async Task<CertificateListReply> RevokeCertificate(CertificateRequest request, ServerCallContext context)
    {
        var command = new RevokeCertificateCommand(request.WorkspaceId, request.RequesterId, request.UserId, request.CertificateId);

        return (await revokeCertificate.HandleAsync(command, context.CancellationToken)).Unwrap(ToReply, logger, nameof(RevokeCertificate));
    }

    private static CertificateListReply ToReply(IReadOnlyList<CertificateResponse> certificates) => new()
    {
        Certificates = { certificates.Select(ToProto) },
    };

    private static Certificate ToProto(CertificateResponse certificate) => new()
    {
        Id = certificate.Id,
        UserId = certificate.UserId,
        Thumbprint = certificate.Thumbprint,
        SerialNumber = certificate.SerialNumber,
        NotBefore = Timestamp.FromDateTimeOffset(certificate.NotBefore),
        NotAfter = Timestamp.FromDateTimeOffset(certificate.NotAfter),
        IssuedAt = Timestamp.FromDateTimeOffset(certificate.IssuedAt),
        RevokedAt = certificate.RevokedAt is { } revokedAt ? Timestamp.FromDateTimeOffset(revokedAt) : null,
        CanDownload = certificate.CanDownload,
    };
}
