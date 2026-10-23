using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShippingExitSystem.Data;
using ShippingExitSystem.DTOs;
using ShippingExitSystem.Models;
using ShippingExitSystem.Services;
using Xunit;

namespace ShippingExitSystem.Tests
{
    public class AuthServiceTests
    {
        private (ApplicationDbContext, IConfiguration) GetContextAndConfig(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var inMemorySettings = new Dictionary<string, string> {
                {"JwtSettings:Secret", "test-only-secret-not-used-anywhere-else-000000"},
                {"JwtSettings:Issuer", "BarcodeShippingSystem"},
                {"JwtSettings:Audience", "BarcodeShippingSystemClient"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            return (context, configuration);
        }

        [Fact]
        public async Task RegisterAsync_ShouldRegisterSuccessfully()
        {
            // Arrange
            var (context, config) = GetContextAndConfig("Register_Success");
            var service = new AuthService(context, config);

            context.Users.Add(new User { Username = "admin", Email = "admin@test.com", PasswordHash = "hash", Role = "Admin" });
            await context.SaveChangesAsync();

            var dto = new RegisterDto
            {
                Username = "newuser",
                Email = "newuser@test.com",
                Password = "SecretPassword123"
            };

            // Act
            var user = await service.RegisterAsync(dto, callerIsAdmin: true);

            // Assert
            Assert.NotNull(user);
            Assert.Equal("newuser", user.Username);
            Assert.Equal("newuser@test.com", user.Email);
            Assert.Equal("Inspector", user.Role); // Default role
            Assert.True(BCrypt.Net.BCrypt.Verify("SecretPassword123", user.PasswordHash));
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrow_WhenUsernameExists()
        {
            // Arrange
            var (context, config) = GetContextAndConfig("Register_Duplicate");
            var service = new AuthService(context, config);

            context.Users.Add(new User
            {
                Username = "existinguser",
                Email = "old@test.com",
                PasswordHash = "hash"
            });
            await context.SaveChangesAsync();

            var dto = new RegisterDto
            {
                Username = "existinguser",
                Email = "new@test.com",
                Password = "password"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RegisterAsync(dto, callerIsAdmin: true));

            Assert.Contains("ya existe", exception.Message);
        }

        [Fact]
        public async Task RegisterAsync_FirstUser_BecomesAdmin()
        {
            var (context, config) = GetContextAndConfig("Register_Bootstrap");
            var service = new AuthService(context, config);

            var user = await service.RegisterAsync(new RegisterDto
            {
                Username = "owner",
                Email = "owner@test.com",
                Password = "SecretPassword123",
                Role = "Inspector"
            }, callerIsAdmin: false);

            Assert.Equal("Admin", user.Role);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReject_WhenCallerIsNotAdmin()
        {
            var (context, config) = GetContextAndConfig("Register_NotAdmin");
            var service = new AuthService(context, config);

            context.Users.Add(new User { Username = "admin", Email = "admin@test.com", PasswordHash = "hash", Role = "Admin" });
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.RegisterAsync(new RegisterDto
                {
                    Username = "attacker",
                    Email = "attacker@test.com",
                    Password = "SecretPassword123",
                    Role = "Admin"
                }, callerIsAdmin: false));

            Assert.False(await context.Users.AnyAsync(u => u.Username == "attacker"));
        }

        [Fact]
        public async Task RegisterAsync_ShouldReject_InvalidRole()
        {
            var (context, config) = GetContextAndConfig("Register_InvalidRole");
            var service = new AuthService(context, config);

            context.Users.Add(new User { Username = "admin", Email = "admin@test.com", PasswordHash = "hash", Role = "Admin" });
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RegisterAsync(new RegisterDto
                {
                    Username = "guard",
                    Email = "guard@test.com",
                    Password = "SecretPassword123",
                    Role = "SuperAdmin"
                }, callerIsAdmin: true));
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var (context, config) = GetContextAndConfig("Login_Success");
            var service = new AuthService(context, config);

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("UserPassword123");
            context.Users.Add(new User
            {
                Username = "activeuser",
                Email = "active@test.com",
                PasswordHash = passwordHash,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var dto = new LoginDto
            {
                Username = "activeuser",
                Password = "UserPassword123"
            };

            // Act
            var token = await service.LoginAsync(dto);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenCredentialsAreInvalid()
        {
            // Arrange
            var (context, config) = GetContextAndConfig("Login_Fail");
            var service = new AuthService(context, config);

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("UserPassword123");
            context.Users.Add(new User
            {
                Username = "activeuser",
                Email = "active@test.com",
                PasswordHash = passwordHash,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var dto = new LoginDto
            {
                Username = "activeuser",
                Password = "WrongPassword"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync(dto));

            Assert.Contains("incorrectos", exception.Message);
        }
    }
}
