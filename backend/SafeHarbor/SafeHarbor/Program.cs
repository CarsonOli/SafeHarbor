using System.Security.Claims;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SafeHarbor.Auth;
using SafeHarbor.Authorization;
using SafeHarbor.Data; 
using SafeHarbor.DTOs;
using SafeHarbor.Infrastructure;
using SafeHarbor.Models.Entities;
using SafeHarbor.Services;
using SafeHarbor.Services.Admin;
using SafeHarbor.Services.Auth;
using SafeHarbor.Services.DonorImpact;
using SafeHarbor.Services.Public;

var builder = WebApplication.CreateBuilder(args);

// NOTE: ASP.NET's default host builder already loads appsettings + environment variables + user-secrets
// (in Development). We add optional Azure Key Vault on top so deployed environments can avoid file-based secrets.
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

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

var passwordPolicy = builder.Configuration.GetSection(PasswordPolicyOptions.SectionName).Get<PasswordPolicyOptions>()
    ?? new PasswordPolicyOptions();
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        // NOTE: We bind explicit classroom/lab policy values here so auth behavior stays predictable
        // across local, CI, and deployed environments instead of relying on framework defaults.
        options.Password.RequiredLength = passwordPolicy.RequiredLength;
        options.Password.RequiredUniqueChars = passwordPolicy.RequiredUniqueChars;
        options.Password.RequireDigit = passwordPolicy.RequireDigit;
        options.Password.RequireLowercase = passwordPolicy.RequireLowercase;
        options.Password.RequireUppercase = passwordPolicy.RequireUppercase;
        options.Password.RequireNonAlphanumeric = passwordPolicy.RequireNonAlphanumeric;

        options.Lockout.MaxFailedAccessAttempts = passwordPolicy.MaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(passwordPolicy.DefaultLockoutMinutes);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<SafeHarborDbContext>()
    .AddSignInManager();

var localAuthEnabled = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("LocalAuth:Enabled");
var useInMemoryPersistence = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DevelopmentFeatures:UseInMemoryDataStore");
var issuer = jwtOptions.Issuer;
var audience = jwtOptions.Audience;
var signingKey = jwtOptions.SigningKey;

// Non-development environments must provide explicit JWT settings so startup fails fast
// instead of silently accepting an invalid auth configuration.
if (!builder.Environment.IsDevelopment()
    && (string.IsNullOrWhiteSpace(issuer)
        || string.IsNullOrWhiteSpace(audience)
        || string.IsNullOrWhiteSpace(signingKey)))
{
    throw new InvalidOperationException("Jwt:Issuer, Jwt:Audience, and Jwt:SigningKey are required outside Development.");
}

if (string.IsNullOrWhiteSpace(issuer))
{
    issuer = "safeharbor-local";
}

if (string.IsNullOrWhiteSpace(audience))
{
    audience = "safeharbor-local-client";
}

if (string.IsNullOrWhiteSpace(signingKey))
{
    signingKey = "development-signing-key-change-me";
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim mapping aligned with LocalAuthController token emission (ClaimTypes.Role + "role"/"roles")
        // so role policies continue to work predictably across environments.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role
        };
    });

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
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// Services Registration
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddSingleton<IDataRetentionRedactionService, DataRetentionRedactionService>();

if (useInMemoryPersistence)
{
    // NOTE: In-memory persistence is an explicit development-only fallback for demos or local debugging.
    // Deployed environments must remain DB-backed to preserve durable data and operational consistency.
    builder.Services.AddSingleton<InMemoryDataStore>();
    builder.Services.AddScoped<IResidentRepository, InMemoryResidentRepository>();
    builder.Services.AddScoped<IDonorRepository, InMemoryDonorRepository>();
    builder.Services.AddScoped<ICampaignRepository, InMemoryCampaignRepository>();
    builder.Services.AddScoped<IContributionRepository, InMemoryContributionRepository>();
}
else
{
    builder.Services.AddScoped<IResidentRepository, DbResidentRepository>();
    builder.Services.AddScoped<IDonorRepository, DbDonorRepository>();
    builder.Services.AddScoped<ICampaignRepository, DbCampaignRepository>();
    builder.Services.AddScoped<IContributionRepository, DbContributionRepository>();
}

builder.Services.AddScoped<IResidentAdminService, ResidentAdminService>();
builder.Services.AddScoped<IDonorAdminService, DonorAdminService>();
builder.Services.AddScoped<IPublicRecordsService, PublicRecordsService>();
builder.Services.AddScoped<IDonorDashboardService, DonorDashboardService>();
builder.Services.AddScoped<IDonorAnalyticsService, DonorAnalyticsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();


// Donor impact calculator — used by DonorDashboardController to compute "girls helped" metric.
// TO SWAP IN AN ML MODEL: replace RuleBasedImpactCalculator with your MlImpactCalculator class here.
// The controller and frontend are unaffected by this change.
builder.Services.AddSingleton<IDonorImpactCalculator, RuleBasedImpactCalculator>();
builder.Services.AddScoped<ICaseloadInventoryService, CaseloadInventoryService>();
builder.Services.AddScoped<IProcessRecordingService, ProcessRecordingService>();
builder.Services.AddScoped<IVisitationConferenceService, VisitationConferenceService>();
builder.Services.AddScoped<IDonorContributionService, DonorContributionService>();
builder.Services.AddScoped<IReportsAnalyticsService, ReportsAnalyticsService>();
builder.Services.AddScoped<IImpactAggregateService, ImpactAggregateService>();

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

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Keep create/update validation failures in a stable envelope shape for frontend forms.
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(msg => !string.IsNullOrWhiteSpace(msg))
                ?? "Request payload validation failed.";

            return new BadRequestObjectResult(new ApiErrorEnvelope("ValidationError", firstError, context.HttpContext.TraceIdentifier));
        };
    });
builder.Services.AddOpenApi();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Cloud ingress/edge proxies terminate TLS and forward scheme/ip headers to the app.
    // Enabling both headers keeps request metadata and HTTPS redirect behavior correct behind Azure/App Gateway.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // NOTE: We intentionally clear known proxy restrictions only outside Development because
    // cloud egress IPs can rotate; in-production trust is bounded at the platform/network layer.
    if (!builder.Environment.IsDevelopment())
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

var app = builder.Build();

if (useInMemoryPersistence)
{
    // Seed dev-only in-memory data only when the explicit feature flag is enabled.
    DonorDashboardSeeder.Seed(app.Services.GetRequiredService<InMemoryDataStore>());
}

if (localAuthEnabled)
{
    await IdentityDevelopmentSeeder.SeedAsync(app.Services);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(); // Must be first — adds CORS headers to ALL responses including OPTIONS preflight

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

var contentSecurityPolicy = string.Join("; ",
[
    "default-src 'self'",
    "script-src 'self'",
    "style-src 'self' https://fonts.googleapis.com",
    "img-src 'self' data:",
    "font-src 'self' https://fonts.gstatic.com data:",
    // Azure Entra sign-in metadata and token exchange calls require outbound connect access.
    "connect-src 'self' https://login.microsoftonline.com",
    "object-src 'none'",
    "base-uri 'self'",
    "frame-ancestors 'none'"
]);

app.Use(async (context, next) =>
{
    // Set a single CSP for every response so browser-enforced defaults stay consistent across endpoints.
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["Content-Security-Policy"] = contentSecurityPolicy;
        return Task.CompletedTask;
    });

    await next();
});

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseCors();
// Forwarded headers must run before HTTPS redirection so X-Forwarded-Proto is honored behind reverse proxies.
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapControllers();

app.Run();

// Expose a concrete Program type so WebApplicationFactory<Program> can boot the API in integration tests.
public partial class Program { }
