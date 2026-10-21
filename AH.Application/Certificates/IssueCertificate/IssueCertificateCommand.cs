namespace AH.Application.Certificates.IssueCertificate;

public sealed record IssueCertificateCommand(int WorkspaceId, int RequesterId, int UserId);
