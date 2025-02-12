using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using cddo_users.Api;
using cddo_users.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using cddo_users.Services.ApplicationInsights;
using System.Text.Json;
using cddo_users.Interface;
using cddo_users.Repositories;
using cddo_users.Logic;
using System.IdentityModel.Tokens.Jwt;
using cddo_users.Services.Database;
using cddo_users.Services;
using Notify.Client;
using Notify.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("Users API Health Check", () => HealthCheckResult.Healthy("API is up and running."))
    .AddCheck<CustomSqlHealthCheck>("SQL Database Health Check", tags: new[] { "db", "sql" });

builder.Services.AddSingleton<CustomSqlHealthCheck>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new CustomSqlHealthCheck(connectionString);
});

builder.Services.AddLogsQueryClient();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CDDO Users", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});
builder.Services.AddTransient<INotificationClient>(provider =>
    {
        var apiKey = builder.Configuration["NotifyApiKey"];

        if(string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("NotifyApiKey is not configured");
        }

        return new NotificationClient(apiKey);
});

// Define the test user flag
bool testUserEnabled = builder.Configuration.GetValue<bool>("TestUser:Enabled");

// Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "InteractiveScheme"; // Default scheme for authentication
    options.DefaultAuthenticateScheme = "InteractiveScheme";
    options.DefaultChallengeScheme = "InteractiveScheme";
})
.AddJwtBearer("InteractiveScheme", options =>
{
    if (testUserEnabled)
    {
        // Test user validation settings
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])), // Test user's signing key
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero
        };
    }
    else
    {
        //Normal validation settings for real users

       var secretKey = builder.Configuration["AGMJwtSettings:SecretKey"];
        var issuer = builder.Configuration["BaseUrl"];
        var previewIssuer = "https://preview.datamarketplace.gov.uk/"; // Add preview base URL

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), // Use the same key to sign the JWT

            ValidateIssuer = true,
            ValidIssuers = new[] // Allow both base URLs for issuer validation
            {
            issuer,
            previewIssuer
        },

            ValidateAudience = true,
            ValidAudiences = new[] // Allow both base and preview URLs for audience validation
            {
            $"{issuer}api",      // Base URL audience
            $"{previewIssuer}api" // Preview URL audience
        },

            ValidateLifetime = true, // Ensure the token has not expired
            ClockSkew = TimeSpan.Zero // Remove default clock skew
        };

       // Optionally handle token validation and error handling
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var utcNow = DateTime.UtcNow;
                var issuedAtClaim = context.Principal.FindFirstValue(JwtRegisteredClaimNames.Iat);

                if (issuedAtClaim != null)
                {
                    // Ensure the issued at claim is properly parsed as a Unix timestamp
                    var issuedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(issuedAtClaim)).UtcDateTime;

                    if (issuedAt.Date != utcNow.Date)
                    {
                        context.Fail("Token was not issued today.");
                    }
                }

                return Task.CompletedTask;
            }
        };
    }
})
.AddJwtBearer("ApiAuthScheme", options =>
{
    // Validation for API requests
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Authentication:ApiKey"])), // Your API signing key
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Authentication:ApiIssuer"], // Your API issuer
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Authentication:ClientId"], // Your API audience
        ClockSkew = TimeSpan.Zero // Optional: Set clock skew to zero
    };
});

builder.Services.AddAuthorization(options =>
{
    // Policy for 'publish' scope
    options.AddPolicy("PublishScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "publish");
    });

    // Policy for 'discover' scope
    options.AddPolicy("DiscoverScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "discover");
    });

    // Policy for 'delete' scope
    options.AddPolicy("DeleteScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "delete");
    });
});

// Registering dependencies
builder.Services.AddScoped<IOrganisationRepository, OrganisationRepository>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IDomainRepository, DomainRepository>();
builder.Services.AddScoped<IApplicationInsightsService, ApplicationInsightsService>();
builder.Services.AddHttpClient<ApplicationInsightsService>();
builder.Services.AddScoped<IAzureLogsQueryClient, AzureLogsQueryClient>();
builder.Services.AddScoped<ILogsQueryResultsBuilder, LogsQueryResultsBuilder>();
builder.Services.AddScoped<ILogsQueryBuilder, LogsQueryBuilder>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserInformationPresenter, UserInformationPresenter>();
builder.Services.AddScoped<IEmailManager, EmailManager>();

builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IClientAuthRepository, ClientAuthRepository>();
builder.Services.AddScoped<IClientAuthService, ClientAuthService>();

builder.Services.AddScoped<ILogsQueryFilterProvision, LogsQueryFilterProvision>();
builder.Services.AddTransient<IAnonymizedUserInformationPopulation, AnonymizedUserInformationPopulation>();

builder.Services.AddLogsQueryClient();

builder.Services.AddApplicationInsightsTelemetry(new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions
{
    ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CDDO Users API V1");
    });
}

// Middleware to handle the test user backdoor and set the id_token
app.Use(async (context, next) =>
{
    bool enableTestUserBackdoor = builder.Configuration.GetValue<bool>("TestUser:Enabled");
    if (enableTestUserBackdoor && context.Request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        var token = authHeader.ToString().Split(' ').Last();
        context.Request.Headers["Authorization"] = $"Bearer {token}";
    }
    await next();
});

app.MapGet("/", () => Results.Ok("API is up and running."))
    .WithMetadata(new AllowAnonymousAttribute());

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            details = report.Entries.Select(entry => new
            {
                key = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                data = entry.Value.Data,
                duration = entry.Value.Duration.ToString()
            })
        };
        await context.Response.WriteAsJsonAsync(result, new JsonSerializerOptions { WriteIndented = true });
    }
}).WithMetadata(new AllowAnonymousAttribute());

app.Run();
