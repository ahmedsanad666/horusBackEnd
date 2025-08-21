// using BackEnd.Data;
// using Swashbuckle.AspNetCore;
// using Microsoft.EntityFrameworkCore;
// using BackEnd.Interfaces;
// using BackEnd.Repositories;
// using BackEnd.Modules;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.IdentityModel.Tokens;
// using BackEnd.Service;
// using Microsoft.OpenApi.Models;
// using Microsoft.Extensions.Logging;


// var builder = WebApplication.CreateBuilder(args);

// // Configure Kestrel for Azure deployment
// var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
// builder.WebHost.ConfigureKestrel(serverOptions =>
// {
//     serverOptions.ListenAnyIP(int.Parse(port));
// });

// // Add at the beginning of Program.cs
// builder.WebHost.CaptureStartupErrors(true);
// builder.WebHost.UseSetting("detailedErrors", "true");

// // Add services to the container.
// // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
// builder.Services.AddControllers();
// builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
// builder.Services.AddScoped<ITokenService, TokenService>();



// builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
// {
//     options.Password.RequireDigit = true;
//     options.Password.RequireLowercase = true;
//     options.Password.RequireUppercase = true;
//     options.Password.RequireNonAlphanumeric = true;
//     options.Password.RequiredLength = 12;
// }).AddEntityFrameworkStores<ApplicationDBContext>().AddDefaultTokenProviders();

// builder.Services.AddAuthentication(options =>
// {
//     options.DefaultAuthenticateScheme =
//     options.DefaultChallengeScheme =
//     options.DefaultForbidScheme =
//     options.DefaultScheme =
//     options.DefaultSignInScheme =
//     options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
// }).AddJwtBearer(options =>
// {
//     options.TokenValidationParameters = new TokenValidationParameters
//     {
//         ValidateIssuer = false, // Set to false for testing
//         ValidateAudience = false, // Set to false for testing
//         ValidateIssuerSigningKey = true,
//         IssuerSigningKey = new SymmetricSecurityKey(
//         System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"])
//     ),
//         NameClaimType = "email"
//     };
// });

// builder.Services.AddSwaggerGen(option =>
// {
//     option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
//     option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         In = ParameterLocation.Header,
//         Description = "Please enter a valid token",
//         Name = "Authorization",
//         Type = SecuritySchemeType.Http,
//         BearerFormat = "JWT",
//         Scheme = "Bearer"
//     });
//     option.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference
//                 {
//                     Type=ReferenceType.SecurityScheme,
//                     Id="Bearer"
//                 }
//             },
//             new string[]{}
//         }
//     });
// });



// //builder.Services.AddDbContext<ApplicationDBContext>(options =>
// //{
// //    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
// //});
// // Change from SQL Server to PostgreSQL
// builder.Services.AddDbContext<ApplicationDBContext>(options =>
// {
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
// });

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowFrontend",
//         policy =>
//         {
//             policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "https://hoursteck.web.app")
//                   .AllowAnyHeader()
//                   .AllowAnyMethod()
//                   .AllowCredentials();
//         });
// });

// var app = builder.Build();
//     app.UseSwagger();
//     app.UseSwaggerUI();

// // Log startup information
// var logger = app.Services.GetRequiredService<ILogger<Program>>();
// logger.LogInformation($"Environment: {app.Environment.EnvironmentName}");
// logger.LogInformation($"Port: {port}");

// // Enable CORS for all origins in production for testing
// if (!app.Environment.IsDevelopment())
// {
//     logger.LogInformation("Using production CORS policy");
//     app.UseCors(builder => builder
//         .AllowAnyOrigin()
//         .AllowAnyMethod()
//         .AllowAnyHeader());
// }
// else
// {
//     logger.LogInformation("Using development CORS policy");
//     app.UseCors("AllowFrontend");
// }

// // Configure the HTTP request pipeline.
// // if (app.Environment.IsDevelopment())
// // {

// // }
// app.UseStaticFiles();

// // app.UseHttpsRedirection();
// app.UseAuthentication();
// app.UseAuthorization();

// app.MapControllers();

// app.Run();



using BackEnd.Data;
using BackEnd.Interfaces;
using BackEnd.Modules;
using BackEnd.Repositories;
using BackEnd.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---- Kestrel / Port (container listens on 8080) ----
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(int.Parse(port)));
builder.WebHost.CaptureStartupErrors(true).UseSetting("detailedErrors", "true");

// ---- Services ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger (+ JWT auth in Swagger)
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Horus API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "https://hoursteck.web.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// ---- Logging basics ----
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Environment: {env} | Port: {port}", app.Environment.EnvironmentName, port);

// ---- Pipeline ----
app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();
// app.UseHttpsRedirection(); // keep off unless you terminate TLS in front (e.g., Nginx)

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowFrontend");
}
else
{
    app.UseCors(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
