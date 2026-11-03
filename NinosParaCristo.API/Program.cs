using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NinosParaCristo.API.Hubs;
using NinosParaCristo.API.Services;
using NinosParaCristo.Application.Interfaces;
using NinosParaCristo.Infrastructure.Persistence;
using NinosParaCristo.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<INinoService, NinoService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ReminderService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<WebPushNotificationService>();
builder.Services.AddHostedService<ReminderBackgroundService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Configure Jwt:Key en appsettings.json.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "NinosParaCristo";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "NinosParaCristo";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/updates"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<UpdatesHub>("/hubs/updates");
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Garantiza el esquema completo tanto para una instalación nueva como para
    // una base creada durante las primeras etapas del TCU.
    await DatabaseBootstrapper.EnsureFullSchemaAsync(db);

    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var adminEmail = builder.Configuration["InitialAdmin:Email"] ?? "admin@ninosparacristo.local";
    var adminPassword = builder.Configuration["InitialAdmin:Password"] ?? "Admin123!";
    var adminName = builder.Configuration["InitialAdmin:Nombre"] ?? "Administradora";
    await SeedData.EnsureAdminAsync(db, hasher, adminEmail, adminPassword, adminName);
    await SeedData.EnsureGruposClaseAsync(db);
}

app.Run();
