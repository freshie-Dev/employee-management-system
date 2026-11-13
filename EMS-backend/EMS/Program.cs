
using System.Security.Claims;
using System.Text;

using EMS.DB;
using EMS.Models;
using EMS.Services;
using EMS.Services.Interfaces;
using EMS.DB.SPs;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


// ============================================================
// 1. APPLICATION BUILDER
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers and OpenAPI services.
builder.Services.AddControllers();
builder.Services.AddOpenApi();


// ============================================================
// 2. DATABASE CONFIGURATION
// ============================================================

// Register Entity Framework Core with SQL Server.
// The connection string is read from application configuration.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DbConnection")
    )
);


// ============================================================
// 3. DEPENDENCY INJECTION (APPLICATION SERVICES)
// ============================================================

// Register application services with a scoped lifetime.
// A new instance is created for each HTTP request scope.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IScheduleSettingsService, ScheduleSettingsService>();
builder.Services.AddScoped<IEmployeeShiftService, EmployeeShiftService>();
builder.Services.AddScoped<IScheduleOverrideService, ScheduleOverrideService>();
builder.Services.AddScoped<IHolidayService, HolidayService>();

// Register ASP.NET Core Identity's password hashing service.
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();

// Shared stored procedure executor
builder.Services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();





// ============================================================
// 4. JWT CONFIGURATION
// ============================================================

// Read JWT configuration from appsettings, user-secrets,
// or environment variables.
var secret = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Missing Jwt:Key. Configure it with dotnet user-secrets or environment variables."
    );

// Require a minimum signing key length of 32 UTF-8 bytes.
if (Encoding.UTF8.GetByteCount(secret) < 32)
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 bytes."
    );

var issuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Missing Jwt:Issuer");

var audience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Missing Jwt:Audience");


// ============================================================
// 5. JWT AUTHENTICATION
// ============================================================

// Configure JWT Bearer as the default authentication scheme.
// Incoming requests must provide a valid JWT access token
// when accessing endpoints that require authentication.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Preserve original JWT claim names instead of
        // automatically mapping them to Microsoft claim types.
        options.MapInboundClaims = false;

        // Define how incoming JWT tokens are validated.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Verify that the token was issued by our application.
            ValidateIssuer = true,
            ValidIssuer = issuer,

            // Verify that the token is intended for our API.
            ValidateAudience = true,
            ValidAudience = audience,

            // Verify the token's digital signature.
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret)
            ),

            // Reject expired tokens.
            ValidateLifetime = true,

            // Allow a small time difference between servers.
            ClockSkew = TimeSpan.FromSeconds(30),

            // Specify which claims identify the user and roles.
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };


        // ====================================================
        // 5.1 CUSTOM TOKEN VALIDATION
        // ====================================================

        // This event executes after the JWT passes standard
        // validation (signature, issuer, audience, expiry).
        //
        // Additional database checks ensure that:
        // 1. The user still exists.
        // 2. The user account is active.
        // 3. The user's roles have not changed since login.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                // Extract the user ID from the JWT claims.
                var idString = context.Principal?
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                // Reject tokens with an invalid user ID.
                if (!int.TryParse(idString, out var id))
                {
                    context.Fail("Invalid user.");
                    return;
                }

                // Resolve the database context from the
                // current HTTP request's service provider.
                var db = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();

                // Retrieve the user along with assigned roles.
                // AsNoTracking is used because this is read-only.
                var user = await db.UserAccounts
                    .AsNoTracking()
                    .Include(x => x.UserRoles)
                        .ThenInclude(x => x.Role)
                    .FirstOrDefaultAsync(x => x.UserId == id);

                // Reject authentication if the user was deleted
                // or the account has been deactivated.
                if (user is null || !user.IsActive)
                {
                    context.Fail("Account disabled.");
                    return;
                }

                // Get the user's current roles from the database.
                var assigned = user.UserRoles
                    .Select(x => x.Role.Name)
                    .OrderBy(x => x)
                    .ToArray();

                // Get the roles stored inside the JWT.
                var tokenRoles = context.Principal!
                    .FindAll(ClaimTypes.Role)
                    .Select(x => x.Value)
                    .OrderBy(x => x)
                    .ToArray();

                // Invalidate the token if database roles and
                // JWT roles no longer match.
                // The user must log in again to get updated roles.
                if (!assigned.SequenceEqual(tokenRoles))
                {
                    context.Fail("Roles changed. Login again.");
                }
            }
        };
    });


// ============================================================
// 6. AUTHORIZATION
// ============================================================

// Register ASP.NET Core authorization services.
//
// Authorization determines whether an authenticated user
// is permitted to access a specific endpoint.
//
// Examples:
// [Authorize]                 -> Any authenticated user
// [Authorize(Roles = "Admin")] -> Only users with Admin role
builder.Services.AddAuthorization();


// ============================================================
// 7. SWAGGER / OPENAPI CONFIGURATION
// ============================================================

// Register endpoint discovery for Swagger.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // API documentation information.
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EMS API",
        Version = "v1",
        Description = "Employee Management System API"
    });

    // Configure Swagger to support JWT Bearer authentication.
    // This enables the "Authorize" button in Swagger UI.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    // Apply the Bearer security scheme to API documentation.
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


// ============================================================
// 8. BUILD APPLICATION
// ============================================================

// Build the application using the registered services.
var app = builder.Build();


// ============================================================
// 9. INITIAL ADMIN ACCOUNT BOOTSTRAPPING
// ============================================================

// Running the application with --bootstrap-admin creates
// the initial administrator account.
//
// This mode performs the bootstrap operation and exits
// without starting the HTTP API server.
if (args.Contains("--bootstrap-admin"))
{
    var username = builder.Configuration["Bootstrap:Username"];
    var password = builder.Configuration["Bootstrap:Password"];

    // Validate the bootstrap administrator credentials.
    if (string.IsNullOrWhiteSpace(username) ||
        username.Length < 3 ||
        string.IsNullOrWhiteSpace(password) ||
        password.Length < 12)
    {
        throw new InvalidOperationException(
            "Set Bootstrap:Username and a password of at least 12 characters."
        );
    }

    // Execute the administrator bootstrap process.
    await AdminBootstrapper.RunAsync(
        app.Services,
        username,
        password
    );

    Console.WriteLine("Administrator account created.");

    return;
}


// ============================================================
// 10. DEVELOPMENT ENVIRONMENT CONFIGURATION
// ============================================================

// Enable Swagger UI only in the Development environment.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "EMS API v1"
        );

        options.RoutePrefix = "swagger";
    });
}


// ============================================================
// 11. HTTP MIDDLEWARE PIPELINE
// ============================================================

// Redirect HTTP requests to HTTPS.
app.UseHttpsRedirection();

// Authenticate requests using the configured JWT scheme.
// On success, HttpContext.User contains the token claims.
app.UseAuthentication();

// Apply authorization rules based on endpoint attributes,
// authenticated identity, roles, and configured policies.
app.UseAuthorization();


// ============================================================
// 12. ENDPOINT MAPPING & APPLICATION STARTUP
// ============================================================

// Map controller actions to their configured API routes.
app.MapControllers();

// Start the application and listen for incoming HTTP requests.
app.Run();
