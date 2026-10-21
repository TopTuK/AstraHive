using FluentValidation;

namespace AH.Application.Workspaces.ChangeWorkspaceMemberRole;

public sealed class ChangeWorkspaceMemberRoleValidator : AbstractValidator<ChangeWorkspaceMemberRoleCommand>
{
    public ChangeWorkspaceMemberRoleValidator()
    {
        RuleFor(c => c.Role).IsInEnum();
    }
}
