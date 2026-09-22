using LeetPOTD.Configuration;
using LeetPOTD.Data;
using LeetPOTD.Repositories;
using LeetPOTD.Services;
using LeetPOTD.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.Configure<Timezone>(
    builder.Configuration.GetSection("Bot"));

builder.Services.AddDbContext<LeetPotdDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("LeetPotd")
        ?? "Data Source=leetpotd.db";

    options.UseSqlite(connectionString);
});

builder.Services.AddHttpClient<ILeetCodeService, LeetCodeService>();
builder.Services.AddScoped<IPotdRepository, EfPotdRepository>();
builder.Services.AddScoped<ISubmissionRepository, EfSubmissionRepository>();
builder.Services.AddScoped<IPotdOrchestrator, PotdOrchestrator>();
builder.Services.AddHostedService<MainWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LeetPotdDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.MapPost("/api/potd/run", async (
    IPotdOrchestrator orchestrator,
    CancellationToken cancellationToken) =>
{
    var run = await orchestrator.RunAsync(cancellationToken);
    return Results.Ok(run);
});

await app.RunAsync();
