using System.Security.Claims;
using System.Text;
using EMS.DB;
using EMS.Models;
using EMS.Services;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DbConnection")));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();

var secret = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException(
    "Missing Jwt:Key. Configure it with dotnet user-secrets or environment variables.");
if (Encoding.UTF8.GetByteCount(secret) < 32) throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
var issuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer");
var audience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = issuer,
        ValidateAudience = true, ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var idString = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idString, out var id)) { context.Fail("Invalid user."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await db.UserAccounts.AsNoTracking().Include(x => x.UserRoles)
                .ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.UserId == id);
            if (user is null || !user.IsActive) { context.Fail("Account disabled."); return; }
            var assigned = user.UserRoles.Select(x => x.Role.Name).OrderBy(x => x).ToArray();
            var tokenRoles = context.Principal!.FindAll(ClaimTypes.Role).Select(x => x.Value)
                .OrderBy(x => x).ToArray();
            if (!assigned.SequenceEqual(tokenRoles)) context.Fail("Roles changed. Login again.");
        }
    };
});
builder.Services.AddAuthorization();
var app = builder.Build();

if (args.Contains("--bootstrap-admin"))
{
    var username = builder.Configuration["Bootstrap:Username"];
    var password = builder.Configuration["Bootstrap:Password"];
    if (string.IsNullOrWhiteSpace(username) || username.Length < 3 ||
        string.IsNullOrWhiteSpace(password) || password.Length < 12)
        throw new InvalidOperationException("Set Bootstrap:Username and a password of at least 12 characters.");
    await AdminBootstrapper.RunAsync(app.Services, username, password);
    Console.WriteLine("Administrator account created.");
    return;
}

if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
