using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ShippingExitSystem.Data;
using ShippingExitSystem.Services;

var builder = WebApplication.CreateBuilder(args);

try
{
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
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"🔍 Connection string: {(string.IsNullOrEmpty(connectionString) ? "NO CONFIGURADA" : "Configurada (oculta)")}");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

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
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();

    // 🔥 CONFIGURACIÓN DE CORS
    var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";
    Console.WriteLine($"🔍 Frontend URL: {frontendUrl}");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(frontendUrl)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRouting();
    app.UseCors("AllowFrontend");

    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // 🔥 Probar conexión a la base de datos al iniciar
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync();
            Console.WriteLine($"🔍 Conexión a BD: {(canConnect ? "EXITOSA ✅" : "FALLIDA ❌")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔍 ERROR de conexión a BD: {ex.Message}");
        }
    }

    Console.WriteLine("✅ Aplicación iniciada correctamente. Escuchando en http://[::]:10000");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ ERROR FATAL: {ex.Message}");
    Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
    throw;
}