using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quartz;
using Scalar.AspNetCore;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Infrastructure.Scryfall;
using Wander.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("Default"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<WanderDbContext>(options =>
    options.UseNpgsql(dataSource));

// Scryfall sync
builder.Services.AddHttpClient<ScryfallBulkDataService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Wander/1.0 (mtg-deck-manager)");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromMinutes(10);  // bulk download can be slow
});

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("ScryfallSync");

    q.AddJob<ScryfallSyncJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ScryfallSync-Trigger")
        .WithCronSchedule("0 0 3 ? * SUN")  // every Sunday at 3am UTC
        .StartNow());  // also fire immediately on startup (first run only)
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

app.Run();
