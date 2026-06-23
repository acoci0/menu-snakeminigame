using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using YemekliYilan.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token gir. Örnek: Bearer token_degeri"
    });

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

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var homePath = Environment.GetEnvironmentVariable("HOME");

    var databaseFolder = !string.IsNullOrWhiteSpace(homePath)
        ? Path.Combine(homePath, "data")
        : AppContext.BaseDirectory;

    Directory.CreateDirectory(databaseFolder);

    var databasePath = Path.Combine(databaseFolder, "yemekli_yilan_live.db");

    options.UseSqlite($"Data Source={databasePath}");
});

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new Exception("JWT Key appsettings.json içinde tanımlı değil.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.EnsureCreated();

        db.Database.ExecuteSqlRaw("""
            DROP TABLE IF EXISTS "GameSessions";
        """);

        db.Database.ExecuteSqlRaw("""
            CREATE TABLE "GameSessions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_GameSessions" PRIMARY KEY AUTOINCREMENT,
                "AppUserId" INTEGER NOT NULL,
                "StartedAt" TEXT NOT NULL,
                "FinishedAt" TEXT NULL,
                "LastFoodAt" TEXT NULL,
                "IsCompleted" INTEGER NOT NULL DEFAULT 0,
                "LastSubmittedScore" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT "FK_GameSessions_Users_AppUserId"
                    FOREIGN KEY ("AppUserId")
                    REFERENCES "Users" ("Id")
                    ON DELETE CASCADE
            );
        """);

        db.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_GameSessions_AppUserId"
            ON "GameSessions" ("AppUserId");
        """);

        app.Logger.LogInformation("SQLite veritabanı ve GameSessions tablosu doğru Users foreign key ile hazırlandı.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "SQLite veritabanı hazırlanırken hata oluştu.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();