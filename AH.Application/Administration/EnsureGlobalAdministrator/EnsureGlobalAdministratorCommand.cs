using AH.Application.Abstractions;

namespace AH.Application.Administration.EnsureGlobalAdministrator;

// From deployment configuration (GlobalAdmin section of IdentityService). Certificate is the Global
// Administrator's client certificate created by AH.CertTool; it is registered so that it can sign in.
public sealed record EnsureGlobalAdministratorCommand(string Email, string FirstName, string LastName, CertificateDetails? Certificate);
