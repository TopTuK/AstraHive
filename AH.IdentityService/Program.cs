using AH.Application;
using AH.IdentityService.Services;
using AH.Infrastructure;
using AH.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    builder.Services.AddApplication();
    builder.AddInfrastructure();

    builder.Services.AddGrpc();

    var app = builder.Build();

    // IdentityService owns the users schema; in development apply migrations on startup
    // so `aspire run` gives a ready database. Production migrations are applied explicitly.
    if (app.Environment.IsDevelopment())
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    app.MapDefaultEndpoints();

    app.UseSerilogRequestLogging();

    app.MapGrpcService<RpcUserService>();

    app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

    await app.RunAsync();
}
// Last-chance fatal log for startup failures; HostAbortedException is how EF tooling stops the host.
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
