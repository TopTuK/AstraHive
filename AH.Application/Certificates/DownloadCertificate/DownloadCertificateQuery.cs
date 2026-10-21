namespace AH.Application.Certificates.DownloadCertificate;

public sealed record DownloadCertificateQuery(int WorkspaceId, int RequesterId, int UserId, int CertificateId);
