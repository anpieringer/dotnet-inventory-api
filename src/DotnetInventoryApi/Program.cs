using System.Security.Claims;
using DotnetInventoryApi.Data;
using DotnetInventoryApi.Models;
using DotnetInventoryApi.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<
        BearerSecuritySchemeTransformer>();

    options.AddOperationTransformer<
        AuthOperationTransformer>();
});

var connectionString =
    builder.Configuration.GetConnectionString(
        "InventoryDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'InventoryDatabase' was not found.");

builder.Services.AddDbContext<InventoryDbContext>(
    options => options.UseNpgsql(connectionString));

var jwtSection =
    builder.Configuration.GetSection(
        JwtOptions.SectionName);

var jwtIssuer = jwtSection["Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer was not configured.");

var jwtAudience = jwtSection["Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience was not configured.");

var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException(
        "JWT key was not configured.");

byte[] jwtKeyBytes;

try
{
    jwtKeyBytes = Convert.FromBase64String(jwtKey);
}
catch (FormatException exception)
{
    throw new InvalidOperationException(
        "JWT key must be a valid Base64 value.",
        exception);
}

if (jwtKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "JWT key must contain at least 32 bytes.");
}

builder.Services.Configure<JwtOptions>(
    jwtSection);

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        jwtKeyBytes),

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ClockSkew =
                    TimeSpan.FromSeconds(30),

                NameClaimType =
                    ClaimTypes.Name,

                RoleClaimType =
                    ClaimTypes.Role
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<
    IPasswordHasher<AppUser>,
    PasswordHasher<AppUser>>();

builder.Services.AddScoped<JwtTokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle(
                "Inventory Management API")
            .AddPreferredSecuritySchemes(
                "Bearer")
            .AddHttpAuthentication(
                "Bearer",
                authentication =>
                {
                    // El token se ingresa manualmente
                    // desde la interfaz de Scalar.
                });
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider
        authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes =
            await authenticationSchemeProvider
                .GetAllSchemesAsync();

        var bearerIsRegistered =
            authenticationSchemes.Any(
                authenticationScheme =>
                    authenticationScheme.Name ==
                    JwtBearerDefaults
                        .AuthenticationScheme);

        if (!bearerIsRegistered)
        {
            return;
        }

        document.Components ??=
            new OpenApiComponents();

        document.Components.SecuritySchemes ??=
            new Dictionary<
                string,
                IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[
            "Bearer"] =
            new OpenApiSecurityScheme
            {
                Type =
                    SecuritySchemeType.Http,

                Scheme = "bearer",

                In =
                    ParameterLocation.Header,

                BearerFormat = "JWT",

                Description =
                    "Enter the JWT access token without the Bearer prefix."
            };
    }
}

internal sealed class AuthOperationTransformer
    : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var endpointMetadata =
            context.Description
                .ActionDescriptor
                .EndpointMetadata;

        var allowsAnonymousAccess =
            endpointMetadata
                .OfType<IAllowAnonymous>()
                .Any();

        var requiresAuthorization =
            endpointMetadata
                .OfType<IAuthorizeData>()
                .Any();

        if (allowsAnonymousAccess ||
            !requiresAuthorization)
        {
            return Task.CompletedTask;
        }

        operation.Security ??=
            new List<
                OpenApiSecurityRequirement>();

        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(
                        "Bearer",
                        context.Document)
                ] = []
            });

        return Task.CompletedTask;
    }
}