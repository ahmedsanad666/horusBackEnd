using BackEnd.Data;
using BackEnd.Interfaces;
using BackEnd.Modules;
using BackEnd.Repositories;
using BackEnd.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// ---- Kestrel / Port (container listens on 8080) ----
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(int.Parse(port)));
builder.WebHost.CaptureStartupErrors(true).UseSetting("detailedErrors", "true");

// ---- Services ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BackEnd", Version = "v1" });
});

// ---- SQL Server DbContext ----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Example env var in docker-compose.yml:
// ConnectionStrings__DefaultConnection="Server=mssql,1433;Database=HorusDb;User Id=sa;Password=ChangeMe!12345!;Encrypt=False;TrustServerCertificate=True;"
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseSqlServer(connectionString));

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
})
.AddEntityFrameworkStores<ApplicationDBContext>()
.AddDefaultTokenProviders();

// JWT auth
var signingKey = builder.Configuration["JWT:SigningKey"]
                 ?? throw new InvalidOperationException("JWT:SigningKey not configured");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            NameClaimType = "email"
        };
    });

// DI
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();

// CORS
var corsOrigins = (builder.Configuration["CORS__Origins"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (corsOrigins.Length > 0)
            policy.WithOrigins(corsOrigins)          // exact origins (scheme+host+optional port)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();               // only if you use cookies/auth tokens in credentials
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); // fallback for dev
    });
});

var app = builder.Build();

// ---- Trust proxy headers from Caddy ----
var fwd = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// when running in containers, clear defaults so all proxies are allowed
fwd.KnownNetworks.Clear();
fwd.KnownProxies.Clear();
app.UseForwardedHeaders(fwd);

// ---- Logging basics ----
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Environment: {env} | Port: {port}", app.Environment.EnvironmentName, port);

// ---- Pipeline ----
// Make the generated Swagger doc use the forwarded scheme/host
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((doc, req) =>
    {
        var scheme = req.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? req.Scheme;
        var host = req.Headers["X-Forwarded-Host"].FirstOrDefault() ?? req.Host.Value;
        doc.Servers = new List<OpenApiServer> { new() { Url = $"{scheme}://{host}" } };
    });
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BackEnd v1");
    c.RoutePrefix = "swagger"; // default; ok to keep
});

app.UseStaticFiles();
// app.UseHttpsRedirection(); // keep off unless you terminate TLS in front (e.g., Nginx)

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ---- Auto-apply migrations on startup ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
    // optional: sql retry is already configured in AddDbContext
    if (db.Database.GetPendingMigrations().Any())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated(); // fallback if you don't have migrations yet
}

app.Run();
