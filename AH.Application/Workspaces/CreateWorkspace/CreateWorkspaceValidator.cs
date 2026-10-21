using AH.AppDomain.Workspaces;
using FluentValidation;

namespace AH.Application.Workspaces.CreateWorkspace;

public sealed class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceValidator()
    {
        RuleFor(c => c.Name).NotEmpty().Must(n => n.Trim().Length <= Workspace.NameMaxLength)
            .WithMessage($"The name must be at most {Workspace.NameMaxLength} characters.");
    }
}
