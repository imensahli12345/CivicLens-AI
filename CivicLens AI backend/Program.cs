using CivicLens.API.Data;
using CivicLens.API.Repository.Classes;
using CivicLens.API.Repository.Interfaces;
using CivicLens.API.Services.Classes;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════════
// 1. BASE DE DONNÉES — PostgreSQL
// ══════════════════════════════════════════════════════════════
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration
        .GetConnectionString("DefaultConnection")));

// ══════════════════════════════════════════════════════════════
// 2. INJECTION DE DÉPENDANCES (Dependency Injection)
// ══════════════════════════════════════════════════════════════
// C'est le même système que dans le projet de référence (Program.cs + Startup.cs) :
//
//   services.AddScoped<IUserService, UserService>();
//   services.AddScoped<IUserRepository, UserRepository>();
//
// AddScoped = une nouvelle instance par requête HTTP.
// Quand un Controller demande IAuthService → le framework donne AuthService.
// Quand AuthService demande IAuthRepository → le framework donne AuthRepository.
// ══════════════════════════════════════════════════════════════
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IBureauDeControleRepository, BureauDeControleRepository>();
builder.Services.AddScoped<IBureauDeControleService, BureauDeControleService>();

// ── Email + Inactivity background worker ──
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<InactivityCheckService>();


// ══════════════════════════════════════════════════════════════
// 3. AUTHENTIFICATION JWT
// ══════════════════════════════════════════════════════════════
// C'est le même code que dans Startup.cs du projet de référence :
//
//   services.AddAuthentication(options =>
//   {
//       options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//       options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//   })
//   .AddJwtBearer(options => {
//       options.TokenValidationParameters = new TokenValidationParameters
//       {
//           ValidateIssuer = false,
//           ValidateAudience = false,
//           ValidateIssuerSigningKey = true,
//           IssuerSigningKey = new SymmetricSecurityKey(
//               Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
//       };
//   });
//
// C'est identique, adapté à .NET 8 (builder au lieu de services).
// ══════════════════════════════════════════════════════════════
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,              // Pas de validation Issuer (comme la référence)
        ValidateAudience = false,            // Pas de validation Audience (comme la référence)
        ValidateIssuerSigningKey = true,     // Valide la signature ← IMPORTANT
        ValidateLifetime = true,             // Vérifie l'expiration du token
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "http://localhost:4201",
                "https://localhost:4201",
                "http://localhost:4202",
                "https://localhost:4202"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


var app = builder.Build();

// ══════════════════════════════════════════════════════════════
// 4. MIGRATION AUTOMATIQUE + SEED
// ══════════════════════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DataSeeder.Seed(db);
}

// ══════════════════════════════════════════════════════════════
// 5. PIPELINE HTTP
// ══════════════════════════════════════════════════════════════
// L'ordre est TRÈS important (identique au projet de référence) :
//
//   app.UseAuthentication();   ← Vérifie le token JWT
//   app.UseAuthorization();    ← Vérifie les rôles [Authorize(Roles = "...")]
//
// UseAuthentication DOIT être avant UseAuthorization !
// ══════════════════════════════════════════════════════════════
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

app.UseAuthentication();   // ← AVANT UseAuthorization (comme la référence)
app.UseAuthorization();

app.MapControllers();
app.Run();
