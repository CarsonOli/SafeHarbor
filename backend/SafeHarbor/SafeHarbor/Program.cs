using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SafeHarbor.Auth;
using SafeHarbor.Authorization;
using SafeHarbor.Data; 
using SafeHarbor.DTOs;
using SafeHarbor.Infrastructure;
using SafeHarbor.Services;
using SafeHarbor.Services.Admin;
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

var localAuthEnabled = (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("LocalAuth:Enabled"))
    || builder.Configuration.GetValue<bool>("LocalAuth:AllowInProduction");
var useInMemoryPersistence = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DevelopmentFeatures:UseInMemoryDataStore");
var authBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

// FORCE local auth logic so we don't need Azure/Microsoft ClientIDs
// We set this to true so Render uses your email/password logic
var forceLocalAuth = true; 

if (forceLocalAuth)
{
    var issuer = builder.Configuration["LocalAuth:Issuer"] ?? "safeharbor-local";
    var audience = builder.Configuration["LocalAuth:Audience"] ?? "safeharbor-local-client";
    // On Render, we'll use a fallback key if the secret isn't set
    var signingKey = builder.Configuration["LocalAuth:SigningKey"] ?? "A_Very_Long_Secret_Key_For_Testing_12345!";

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

// --- REPOSITORY REGISTRATION ---
// We remove the 'if' check to ensure Render ALWAYS uses the Postgres database.
// If you ever need to go back to In-Memory locally, you can toggle it here manually.
builder.Services.AddScoped<IResidentRepository, DbResidentRepository>();
builder.Services.AddScoped<IDonorRepository, DbDonorRepository>();
builder.Services.AddScoped<ICampaignRepository, DbCampaignRepository>();
builder.Services.AddScoped<IContributionRepository, DbContributionRepository>();

// --- SERVICE REGISTRATION ---
builder.Services.AddScoped<IResidentAdminService, ResidentAdminService>();
builder.Services.AddScoped<IDonorAdminService, DonorAdminService>();
builder.Services.AddScoped<IPublicRecordsService, PublicRecordsService>();
builder.Services.AddScoped<IDonorDashboardService, DonorDashboardService>();
builder.Services.AddScoped<IDonorAnalyticsService, DonorAnalyticsService>();

// --- DONOR IMPACT CALCULATOR ---
// This is the "Girls Helped" logic. If you have an ML model class (e.g., MlImpactCalculator), 
// swap 'RuleBasedImpactCalculator' for your new class name below.
builder.Services.AddSingleton<IDonorImpactCalculator, RuleBasedImpactCalculator>();

// --- ADDITIONAL BUSINESS LOGIC ---
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
if (app.Environment.IsDevelopment()) 
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapControllers();

app.Run();

// Expose a concrete Program type so WebApplicationFactory<Program> can boot the API in integration tests.
public partial class Program { }
