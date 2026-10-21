using AH.AppDomain.Users;
using FluentValidation;

namespace AH.Application.Administration.EnsureGlobalAdministrator;

public sealed class EnsureGlobalAdministratorValidator : AbstractValidator<EnsureGlobalAdministratorCommand>
{
    public EnsureGlobalAdministratorValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(User.EmailMaxLength);
        RuleFor(c => c.FirstName).MaximumLength(User.NameMaxLength);
        RuleFor(c => c.LastName).MaximumLength(User.NameMaxLength);
        RuleFor(c => c.Certificate!.Thumbprint).Length(UserCertificate.ThumbprintLength).When(c => c.Certificate is not null);
    }
}
