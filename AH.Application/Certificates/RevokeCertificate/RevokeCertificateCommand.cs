namespace AH.Application.Certificates.RevokeCertificate;

public sealed record RevokeCertificateCommand(int WorkspaceId, int RequesterId, int UserId, int CertificateId);
