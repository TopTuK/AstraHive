using AH.Application.Workspaces.AddWorkspaceMember;
using AH.AppDomain.Workspaces;

namespace AH.Application.Tests.Workspaces.AddWorkspaceMember;

public sealed class AddWorkspaceMemberValidatorTests
{
    private readonly AddWorkspaceMemberValidator _validator = new();

    private static AddWorkspaceMemberCommand ValidCommand() => new(1, 1, "jane@example.com", WorkspaceRole.Viewer);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_BadEmail_Fails(string email)
    {
        Assert.False(_validator.Validate(ValidCommand() with { Email = email }).IsValid);
    }

    [Fact]
    public void Validate_UndefinedRole_Fails()
    {
        Assert.False(_validator.Validate(ValidCommand() with { Role = (WorkspaceRole)42 }).IsValid);
    }
}
