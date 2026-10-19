using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.Application.Users;
using AH.Application.Users.UpdateUserProfile;
using AH.AppDomain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AH.Application.Tests.Users.UpdateUserProfile;

public sealed class UpdateUserProfileHandlerTests(PostgresFixture database) : DatabaseTestBase(database)
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Result<UserResponse>> HandleAsync(UpdateUserProfileCommand command)
    {
        await using var db = CreateDbContext();
        var handler = new UpdateUserProfileHandler(db, new UpdateUserProfileValidator(), NullLogger<UpdateUserProfileHandler>.Instance);
        return await handler.HandleAsync(command, Ct);
    }

    private async Task<int> SeedUserAsync()
    {
        await using var db = CreateDbContext();
        var user = User.Create("john@example.com", "John", "Doe", "https://example.com/a.png", Now);
        db.Users.Add(user);
        await db.SaveChangesAsync(Ct);
        return user.Id;
    }

    private async Task<User> LoadUserAsync(int id)
    {
        await using var db = CreateDbContext();
        return await db.Users.AsNoTracking().SingleAsync(u => u.Id == id, Ct);
    }

    [Fact]
    public async Task HandleAsync_ValidNames_PersistsTrimmedNames()
    {
        var id = await SeedUserAsync();

        var result = await HandleAsync(new UpdateUserProfileCommand(id, " Jane ", " Smith "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane", result.Value.FirstName);
        Assert.Equal("Smith", result.Value.LastName);
        var user = await LoadUserAsync(id);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Smith", user.LastName);
        Assert.Equal("john@example.com", user.Email);
        Assert.Equal("https://example.com/a.png", user.AvatarUrl);
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_ReturnsNotFound()
    {
        var result = await HandleAsync(new UpdateUserProfileCommand(42, "Jane", "Smith"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task HandleAsync_BlankFirstName_ReturnsValidationErrorAndKeepsNames()
    {
        var id = await SeedUserAsync();

        var result = await HandleAsync(new UpdateUserProfileCommand(id, "  ", "Smith"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        var user = await LoadUserAsync(id);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
    }
}
