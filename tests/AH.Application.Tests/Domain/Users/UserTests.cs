using AH.AppDomain.Users;

namespace AH.Application.Tests.Domain.Users;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_MixedCaseEmailWithSpaces_StoresTrimmedLowercaseEmail()
    {
        var user = User.Create("  John.Doe@Example.COM ", "John", "Doe", null, Now);

        Assert.Equal("john.doe@example.com", user.Email);
    }

    [Fact]
    public void Create_ValidInput_SetsCreatedAndLastLoginToNow()
    {
        var user = User.Create("john@example.com", "John", "Doe", "https://example.com/a.png", Now);

        Assert.Equal(Now, user.CreatedAt);
        Assert.Equal(Now, user.LastLoginAt);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("https://example.com/a.png", user.AvatarUrl);
        Assert.Null(user.PreferredLanguage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankEmail_ThrowsArgumentException(string email)
    {
        Assert.ThrowsAny<ArgumentException>(() => User.Create(email, "John", "Doe", null, Now));
    }

    [Fact]
    public void SyncFromProvider_StoredNames_KeepsNamesAndRefreshesAvatar()
    {
        var user = User.Create("john@example.com", "John", "Doe", "https://example.com/a.png", Now);

        user.SyncFromProvider("Johnny", "Smith", " https://example.com/b.png ");

        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("https://example.com/b.png", user.AvatarUrl);
    }

    [Fact]
    public void SyncFromProvider_EmptyStoredLastName_FillsTrimmedLastName()
    {
        var user = User.Create("john@example.com", "John", "", null, Now);

        user.SyncFromProvider("Johnny", "  Doe ", null);

        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
    }

    [Fact]
    public void SyncFromProvider_BlankValues_KeepsExistingProfile()
    {
        var user = User.Create("john@example.com", "John", "Doe", "https://example.com/a.png", Now);

        user.SyncFromProvider("", "   ", null);

        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("https://example.com/a.png", user.AvatarUrl);
    }

    [Fact]
    public void Rename_ValuesWithSpaces_StoresTrimmedNames()
    {
        var user = User.Create("john@example.com", "John", "Doe", null, Now);

        user.Rename("  Jane ", " Smith  ");

        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Smith", user.LastName);
    }

    [Fact]
    public void Rename_BlankLastName_ClearsLastName()
    {
        var user = User.Create("john@example.com", "John", "Doe", null, Now);

        user.Rename("John", "  ");

        Assert.Equal(string.Empty, user.LastName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_BlankFirstName_ThrowsArgumentException(string firstName)
    {
        var user = User.Create("john@example.com", "John", "Doe", null, Now);

        Assert.ThrowsAny<ArgumentException>(() => user.Rename(firstName, "Doe"));
        Assert.Equal("John", user.FirstName);
    }

    [Fact]
    public void Rename_TooLongFirstName_ThrowsArgumentOutOfRangeException()
    {
        var user = User.Create("john@example.com", "John", "Doe", null, Now);

        Assert.Throws<ArgumentOutOfRangeException>(() => user.Rename(new string('a', User.NameMaxLength + 1), "Doe"));
    }

    [Fact]
    public void CreatePending_Email_IsPendingWithNormalizedEmailAndNoNames()
    {
        var user = User.CreatePending(" Jane@Example.com ", Now);

        Assert.True(user.IsPending);
        Assert.Null(user.LastLoginAt);
        Assert.Equal("jane@example.com", user.Email);
        Assert.Equal(string.Empty, user.FirstName);
        Assert.Equal(Now, user.CreatedAt);
    }

    [Fact]
    public void RecordLogin_PendingUser_IsNoLongerPending()
    {
        var user = User.CreatePending("jane@example.com", Now);

        user.RecordLogin(Now.AddDays(1));

        Assert.False(user.IsPending);
        Assert.Equal(Now.AddDays(1), user.LastLoginAt);
    }

    [Fact]
    public void RecordLogin_LaterTime_UpdatesOnlyLastLoginAt()
    {
        var user = User.Create("john@example.com", "John", "Doe", null, Now);
        var later = Now.AddDays(3);

        user.RecordLogin(later);

        Assert.Equal(later, user.LastLoginAt);
        Assert.Equal(Now, user.CreatedAt);
    }

    [Fact]
    public void CreateGlobalAdministrator_ValidInput_IsPendingGlobalAdministrator()
    {
        var user = User.CreateGlobalAdministrator(" Admin@Example.com ", "Ada", "Admin", Now);

        Assert.True(user.IsGlobalAdministrator);
        Assert.True(user.IsPending);
        Assert.Equal("admin@example.com", user.Email);
        Assert.Equal("Ada", user.FirstName);
    }

    [Fact]
    public void PromoteToGlobalAdministrator_RegularUser_BecomesGlobalAdministrator()
    {
        var user = User.Create("john@example.com", "John", "Doe", null, Now);

        user.PromoteToGlobalAdministrator();

        Assert.True(user.IsGlobalAdministrator);
    }
}
