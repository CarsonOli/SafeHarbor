using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore; 
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SafeHarbor.Authorization;
using SafeHarbor.Data; 
using SafeHarbor.Infrastructure;
using SafeHarbor.Services;
using SafeHarbor.Services.DonorImpact;
using SafeHarbor.Services.LocalAuth;

var builder = WebApplication.CreateBuilder(args);

// Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});

// --- DATABASE REGISTRATION START ---
// Updated to use PostgreSQL (Npgsql) instead of SQL Server
builder.Services.AddDbContext<SafeHarborDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// --- DATABASE REGISTRATION END ---

var localAuthEnabled = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("LocalAuth:Enabled");
var authBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

if (localAuthEnabled)
{
    // Development-only JWT validation path. This keeps local auth deterministic while preserving
    // the same bearer-token middleware used in production with Entra ID.
    var issuer = builder.Configuration["LocalAuth:Issuer"] ?? "safeharbor-local";
    var audience = builder.Configuration["LocalAuth:Audience"] ?? "safeharbor-local-client";
    var signingKey = builder.Configuration["LocalAuth:SigningKey"]
        ?? throw new InvalidOperationException("LocalAuth:SigningKey is required when LocalAuth:Enabled=true.");

    authBuilder.AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
}
else
{
    authBuilder.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.AdminOnly, policy => policy.RequireRole("Admin"));
    options.AddPolicy(PolicyNames.StaffOrAdmin, policy => policy.RequireRole("Admin", "SocialWorker"));
    options.AddPolicy(PolicyNames.SocialWorkerOnly, policy => policy.RequireRole("SocialWorker"));
    options.AddPolicy(PolicyNames.AuthenticatedUser, policy => policy.RequireAuthenticatedUser());

    // DonorOnly restricts donor dashboard endpoints to users with the "Donor" role.
    // Currently the donor controller uses [AllowAnonymous] for local dev; this policy
    // will be enforced once Entra ID authentication is fully wired.
    options.AddPolicy(PolicyNames.DonorOnly, policy => policy.RequireRole("Donor"));
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Services Registration
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddSingleton<IDataRetentionRedactionService, DataRetentionRedactionService>();
builder.Services.AddSingleton<ILocalAccountStore, InMemoryLocalAccountStore>();
builder.Services.AddSingleton<InMemoryDataStore>();

// Donor impact calculator — used by DonorDashboardController to compute "girls helped" metric.
// TO SWAP IN AN ML MODEL: replace RuleBasedImpactCalculator with your MlImpactCalculator class here.
// The controller and frontend are unaffected by this change.
builder.Services.AddSingleton<IDonorImpactCalculator, RuleBasedImpactCalculator>();

// NOTE: Live/ready probes support platform health checks and safer blue/green swaps.
builder.Services.AddHealthChecks();

// Telemetry configuration
var telemetryServiceName = builder.Configuration["Telemetry:ServiceName"] ?? "safeharbor-api";
var otlpEndpoint = builder.Configuration["Telemetry:OtlpEndpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(telemetryServiceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
            //.AddEntityFrameworkCoreInstrumentation(); // Enabled this to help you debug DB queries!

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    });

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed the in-memory store with test donors, a campaign, and contribution history.
// This allows the donor dashboard to render immediately without a real database.
// See Infrastructure/DonorDashboardSeeder.cs for test credentials and amounts.
// TODO: Remove once a real database with migration seeds is in place.
DonorDashboardSeeder.Seed(app.Services.GetRequiredService<InMemoryDataStore>());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseCors();
//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapControllers();

app.Run();

// Expose a concrete Program type so WebApplicationFactory<Program> can boot the API in integration tests.
public partial class Program { }
