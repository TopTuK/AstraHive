namespace AH.Application.Certificates.GetUserCertificates;

public sealed record GetUserCertificatesQuery(int WorkspaceId, int RequesterId, int UserId);
