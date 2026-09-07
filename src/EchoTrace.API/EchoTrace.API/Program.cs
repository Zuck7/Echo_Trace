using System.Text;
using EchoTrace.API.Middleware;
using EchoTrace.Application.Common.Behaviors;
using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Interfaces.Repositories;
using EchoTrace.Domain.Interfaces.Services;
using EchoTrace.Infrastructure.ExternalServices;
using EchoTrace.Infrastructure.Persistence;
using EchoTrace.Infrastructure.Persistence.Repositories;
using EchoTrace.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ── Services ───────────────────────────────────────────────────────────────
var services = builder.Services;

// Database
services.AddDbContext<EchoTraceDbContext>((sp, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.CommandTimeout(30).EnableRetryOnFailure(3));
});

// MediatR — scans Application assembly for all handlers
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ValidationBehavior<,>).Assembly));

// MediatR pipeline: validation runs before every handler
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// FluentValidation — auto-registers all validators in Application
services.AddValidatorsFromAssembly(typeof(ValidationBehavior<,>).Assembly);

// HTTP context accessor (needed by CurrentTenantService)
services.AddHttpContextAccessor();

// In-memory cache backing IUploadRequestStore (Milestone 1.5) — fine for a single-instance Phase
// 1 deployment; a multi-instance deployment would need Redis instead (Phase 3).
services.AddMemoryCache();

// Infrastructure services
services.AddScoped<ICycleDetectionService, CycleDetectionService>();
services.AddScoped<IAuditService, AuditService>();
services.AddScoped<ICurrentTenantService, CurrentTenantService>();
services.AddScoped<IPasswordHasher, PasswordHasher>();
services.AddScoped<IJwtTokenService, JwtTokenService>();
services.AddSingleton<IUploadRequestStore, UploadRequestStore>();

// Repository registrations
services.AddScoped<IOrganizationRepository, OrganizationRepository>();
services.AddScoped<ISupplyChainRepository, SupplyChainRepository>();
services.AddScoped<IAuditLogRepository, AuditLogRepository>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<ITenantRepository, TenantRepository>();
services.AddScoped<IDocumentRepository, DocumentRepository>();

// File Service (Node.js, internal-only) — see docs/ADR/003-file-microservice.md
var fileServiceBaseUrl = builder.Configuration["FileService:BaseUrl"];
if (string.IsNullOrWhiteSpace(fileServiceBaseUrl))
{
    fileServiceBaseUrl = builder.Environment.IsEnvironment("Testing")
        ? "http://fileservice.invalid"
        : throw new InvalidOperationException("FileService:BaseUrl is not configured.");
}

var fileServiceInternalKey = builder.Configuration["FileService:InternalKey"];
if (string.IsNullOrWhiteSpace(fileServiceInternalKey))
{
    fileServiceInternalKey = builder.Environment.IsEnvironment("Testing")
        ? "test-internal-key"
        : throw new InvalidOperationException("FileService:InternalKey is not configured.");
}

services.AddHttpClient<IFileServiceClient, FileServiceClient>(client =>
{
    client.BaseAddress = new Uri(fileServiceBaseUrl);
    client.DefaultRequestHeaders.Add("X-Internal-Key", fileServiceInternalKey);
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = builder.Environment.IsEnvironment("Testing")
        ? "this-is-a-long-test-key-for-jwt-signing-only"
        : throw new InvalidOperationException("Jwt:Key is not configured.");
}

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

services.AddAuthorization();
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Echo-Trace API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ── App pipeline ───────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Echo-Trace API v1"));

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EchoTraceDbContext>();
    await db.Database.MigrateAsync();

    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbInitializer.SeedAsync(db, hasher, builder.Configuration, seedLogger);
}

app.UseHttpsRedirection();
app.UseCors();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous();

await app.RunAsync();

public partial class Program;
