using Api.Infrastructure;
using Api.Services;
using DataAccess;
using Microsoft.Extensions.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging (Serilog) with correlation-id enrichment from the log context.
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// RFC 9457 Problem Details for all errors; typed domain exceptions → status codes (with a traceId).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemExceptionHandler>();

// Data access + services. The connection string is read from config at resolve time (never
// hard-coded) — 'ConnectionStrings:Sql' points the app at its database.
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
    new SqlConnectionFactory(sp.GetRequiredService<IConfiguration>().GetConnectionString("Sql")!));
builder.Services.AddScoped<IDistrictRepository, DistrictRepository>();
builder.Services.AddScoped<ISalespersonRepository, SalespersonRepository>();
builder.Services.AddScoped<IDistrictService, DistrictService>();
builder.Services.AddScoped<DbInitializer>();

// Dev-only: let the Angular dev server (its own origin) call the API cross-origin.
const string DevClientCors = "dev-client";
builder.Services.AddCors(options => options.AddPolicy(DevClientCors, policy => policy
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DevClientCors);

    // Bring an empty database up to a runnable, seeded state (idempotent). Opt-in via
    // 'Seed:OnStartup' so it fires for real runs (dotnet run / the E2E) but NOT for the in-process
    // test host (WebApplicationFactory), whose contract tests use a placeholder connection string.
    if (app.Configuration.GetValue<bool>("Seed:OnStartup"))
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DbInitializer>().EnsureSeededAsync();
    }
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed so the integration tier can host the API in-process (WebApplicationFactory<Program>).
public partial class Program;
