using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShippingExitSystem.Data;
using ShippingExitSystem.DTOs;
using ShippingExitSystem.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShippingExitSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<User> RegisterAsync(RegisterDto dto, bool callerIsAdmin)
        {
            // El primer usuario del sistema se crea como Admin; después solo un Admin puede crear usuarios
            var isFirstUser = !await _context.Users.AnyAsync();
            if (!isFirstUser && !callerIsAdmin)
                throw new UnauthorizedAccessException("Solo un administrador puede crear usuarios.");

            var role = isFirstUser ? Roles.Admin : (dto.Role ?? Roles.Inspector);
            if (!Roles.IsValid(role))
                throw new InvalidOperationException($"Rol inválido. Usa: {string.Join(", ", Roles.All)}.");

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Email);

            if (existingUser != null)
                throw new InvalidOperationException("El usuario o email ya existe");

            var user = new User
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Usuario o contraseña incorrectos");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Usuario inactivo");

            return GenerateJwtToken(user);
        }

        public string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["JwtSettings:Secret"]
                ?? throw new InvalidOperationException("JwtSettings:Secret no está configurado.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? Roles.Inspector),
                new Claim("userId", user.Id.ToString())
            };

            var expirationMinutes = _configuration.GetValue<int?>("JwtSettings:ExpirationMinutes") ?? 480;

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"] ?? "BarcodeShippingSystem",
                audience: _configuration["JwtSettings:Audience"] ?? "BarcodeShippingSystemClient",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
