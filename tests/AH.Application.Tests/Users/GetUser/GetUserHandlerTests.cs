using AH.Application.Common;
using AH.Application.Tests.Fixtures;
using AH.Application.Users;
using AH.Application.Users.GetUser;
using AH.AppDomain.Users;

namespace AH.Application.Tests.Users.GetUser;

public sealed class GetUserHandlerTests(PostgresFixture database) : DatabaseTestBase(database)
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Result<UserResponse>> HandleAsync(int userId)
    {
        await using var db = CreateDbContext();
        return await new GetUserHandler(db).HandleAsync(new GetUserQuery(userId), Ct);
    }

    [Fact]
    public async Task HandleAsync_ExistingUser_ReturnsUser()
    {
        int id;
        await using (var db = CreateDbContext())
        {
            var user = User.Create("john@example.com", "John", "Doe", "https://example.com/a.png", Now);
            db.Users.Add(user);
            await db.SaveChangesAsync(Ct);
            id = user.Id;
        }

        var result = await HandleAsync(id);

        Assert.True(result.IsSuccess);
        Assert.Equal(new UserResponse(id, "john@example.com", "John", "Doe", "https://example.com/a.png", null, Now), result.Value);
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_ReturnsNotFound()
    {
        var result = await HandleAsync(42);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
