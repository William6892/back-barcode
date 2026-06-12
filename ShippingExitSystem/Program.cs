using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ShippingExitSystem.Data;
using ShippingExitSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Cargar configuración local (no se sube a GitHub)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. \r\n\r\n Ingresa 'Bearer' [espacio] y luego tu token.\r\n\r\nEjemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Database - PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtKey = builder.Configuration["JwtSettings:Secret"] ?? "SuperSecretKey12345678901234567890";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "BarcodeShippingSystem",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "BarcodeShippingSystemClient",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();

// 🔥 CONFIGURACIÓN DE CORS CORREGIDA
// Permite explícitamente el frontend en Render
var frontendUrl = builder.Configuration["FrontendUrl"] ?? "https://frontend-barcode.onrender.com";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendUrl)           // URL del frontend en Render
              .AllowAnyMethod()                    // GET, POST, PUT, DELETE, etc.
              .AllowAnyHeader()                    // Content-Type, Authorization, etc.
              .AllowCredentials();                 // Permite cookies/tokens
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔥 IMPORTANTE: El orden importa
// 1. UseRouting debe estar antes de CORS si lo usas
app.UseRouting();

// 2. CORS va ANTES de Authentication y Authorization
app.UseCors("AllowFrontend");

// 3. Redirección HTTPS (solo en desarrollo o si tienes certificado)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 4. Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

// 5. Mapeo de controladores
app.MapControllers();

// 6. Manejador de errores global opcional (para producción)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\": \"Ocurrió un error en el servidor\"}");
        });
    });
}

app.Run();