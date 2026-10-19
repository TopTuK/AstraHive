using AH.Application.Users.UpdateUserProfile;
using AH.AppDomain.Users;

namespace AH.Application.Tests.Users.UpdateUserProfile;

public sealed class UpdateUserProfileValidatorTests
{
    private readonly UpdateUserProfileValidator _validator = new();

    private static UpdateUserProfileCommand ValidCommand() => new(1, "John", "Doe");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyLastName_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { LastName = "" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankFirstName_Fails(string firstName)
    {
        var result = _validator.Validate(ValidCommand() with { FirstName = firstName });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateUserProfileCommand.FirstName));
    }

    [Fact]
    public void Validate_TooLongFirstName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { FirstName = new string('a', User.NameMaxLength + 1) });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateUserProfileCommand.FirstName));
    }

    [Fact]
    public void Validate_TooLongLastName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { LastName = new string('a', User.NameMaxLength + 1) });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateUserProfileCommand.LastName));
    }

    [Fact]
    public void Validate_MaxLengthNameWithSurroundingSpaces_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { FirstName = $"  {new string('a', User.NameMaxLength)}  " });

        Assert.True(result.IsValid);
    }
}
