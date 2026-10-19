using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.Application.Users;
using AH.Application.Users.GetOrCreateUser;
using AH.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace AH.Application.Tests.Users.GetOrCreateUser;

public sealed class GetOrCreateUserHandlerTests(PostgresFixture database) : DatabaseTestBase(database)
{
    private static readonly DateTimeOffset Start = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(Start);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Result<UserResponse>> HandleAsync(GetOrCreateUserCommand command)
    {
        // A fresh context per call, like a gRPC request scope in IdentityService.
        await using var db = CreateDbContext();
        var handler = new GetOrCreateUserHandler(db, new GetOrCreateUserValidator(), _time, NullLogger<GetOrCreateUserHandler>.Instance);
        return await handler.HandleAsync(command, Ct);
    }

    private async Task<List<AH.AppDomain.Users.User>> LoadUsersAsync()
    {
        await using AppDbContext db = CreateDbContext();
        return await db.Users.AsNoTracking().ToListAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_NewEmail_CreatesUser()
    {
        var result = await HandleAsync(new GetOrCreateUserCommand("John@Example.com", "John", "Doe", "https://example.com/a.png"));

        Assert.True(result.IsSuccess);
        var user = Assert.Single(await LoadUsersAsync());
        Assert.Equal(result.Value.Id, user.Id);
        Assert.Equal("john@example.com", user.Email);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("https://example.com/a.png", user.AvatarUrl);
        Assert.Equal(Start, user.CreatedAt);
        Assert.Equal(Start, user.LastLoginAt);
    }

    [Fact]
    public async Task HandleAsync_ExistingEmailDifferentCase_ReturnsSameUserKeepsNamesAndUpdatesAvatar()
    {
        var created = await HandleAsync(new GetOrCreateUserCommand("john@example.com", "John", "Doe", null));
        _time.Advance(TimeSpan.FromHours(5));

        var again = await HandleAsync(new GetOrCreateUserCommand("JOHN@example.com", "Johnny", "Doe", "https://example.com/new.png"));

        Assert.Equal(created.Value.Id, again.Value.Id);
        var user = Assert.Single(await LoadUsersAsync());
        Assert.Equal("John", user.FirstName);
        Assert.Equal("https://example.com/new.png", user.AvatarUrl);
        Assert.Equal(Start, user.CreatedAt);
        Assert.Equal(Start.AddHours(5), user.LastLoginAt);
    }

    [Fact]
    public async Task HandleAsync_ExistingUserBlankNames_KeepsStoredNames()
    {
        await HandleAsync(new GetOrCreateUserCommand("john@example.com", "John", "Doe", "https://example.com/a.png"));

        var result = await HandleAsync(new GetOrCreateUserCommand("john@example.com", "", "", null));

        Assert.Equal("John", result.Value.FirstName);
        Assert.Equal("Doe", result.Value.LastName);
        Assert.Equal("https://example.com/a.png", result.Value.AvatarUrl);
    }

    [Fact]
    public async Task HandleAsync_RenamedUserSignsInAgain_KeepsEditedNames()
    {
        var created = await HandleAsync(new GetOrCreateUserCommand("john@example.com", "John", "Doe", null));
        await using (var db = CreateDbContext())
        {
            var stored = await db.Users.SingleAsync(u => u.Id == created.Value.Id, Ct);
            stored.Rename("Jonathan", "Smith");
            await db.SaveChangesAsync(Ct);
        }

        var result = await HandleAsync(new GetOrCreateUserCommand("john@example.com", "John", "Doe", null));

        Assert.Equal("Jonathan", result.Value.FirstName);
        Assert.Equal("Smith", result.Value.LastName);
    }

    [Fact]
    public async Task HandleAsync_InvalidEmail_ReturnsValidationErrorAndPersistsNothing()
    {
        var result = await HandleAsync(new GetOrCreateUserCommand("not-an-email", "John", "Doe", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Empty(await LoadUsersAsync());
    }
}
