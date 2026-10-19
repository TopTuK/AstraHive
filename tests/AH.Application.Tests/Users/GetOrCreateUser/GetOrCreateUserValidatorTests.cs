using AH.Application.Users.GetOrCreateUser;
using AH.AppDomain.Users;

namespace AH.Application.Tests.Users.GetOrCreateUser;

public sealed class GetOrCreateUserValidatorTests
{
    private readonly GetOrCreateUserValidator _validator = new();

    private static GetOrCreateUserCommand ValidCommand() =>
        new("john@example.com", "John", "Doe", "https://example.com/a.png");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyNamesAndNoAvatar_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { FirstName = "", LastName = "", AvatarUrl = null });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_InvalidEmail_FailsOnEmail(string email)
    {
        var result = _validator.Validate(ValidCommand() with { Email = email });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetOrCreateUserCommand.Email));
    }

    [Fact]
    public void Validate_EmailTooLong_FailsOnEmail()
    {
        var email = new string('a', User.EmailMaxLength) + "@example.com";

        var result = _validator.Validate(ValidCommand() with { Email = email });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetOrCreateUserCommand.Email));
    }

    [Fact]
    public void Validate_NamesTooLong_FailsOnBothNames()
    {
        var name = new string('a', User.NameMaxLength + 1);

        var result = _validator.Validate(ValidCommand() with { FirstName = name, LastName = name });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetOrCreateUserCommand.FirstName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetOrCreateUserCommand.LastName));
    }

    [Fact]
    public void Validate_AvatarUrlTooLong_FailsOnAvatarUrl()
    {
        var url = "https://example.com/" + new string('a', User.AvatarUrlMaxLength);

        var result = _validator.Validate(ValidCommand() with { AvatarUrl = url });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetOrCreateUserCommand.AvatarUrl));
    }
}
