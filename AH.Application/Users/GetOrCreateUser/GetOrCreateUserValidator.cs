using AH.AppDomain.Users;
using FluentValidation;

namespace AH.Application.Users.GetOrCreateUser;

public sealed class GetOrCreateUserValidator : AbstractValidator<GetOrCreateUserCommand>
{
    public GetOrCreateUserValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(User.EmailMaxLength);
        RuleFor(c => c.FirstName).MaximumLength(User.NameMaxLength);
        RuleFor(c => c.LastName).MaximumLength(User.NameMaxLength);
        RuleFor(c => c.AvatarUrl).MaximumLength(User.AvatarUrlMaxLength);
        RuleFor(c => c.Method).IsInEnum();
        RuleFor(c => c.CertificateThumbprint).NotEmpty().When(c => c.Method == SignInMethod.Certificate);
    }
}
