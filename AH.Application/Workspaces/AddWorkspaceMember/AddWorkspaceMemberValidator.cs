using AH.AppDomain.Users;
using FluentValidation;

namespace AH.Application.Workspaces.AddWorkspaceMember;

public sealed class AddWorkspaceMemberValidator : AbstractValidator<AddWorkspaceMemberCommand>
{
    public AddWorkspaceMemberValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(User.EmailMaxLength);
        RuleFor(c => c.Role).IsInEnum();
    }
}
