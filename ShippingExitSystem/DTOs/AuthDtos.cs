using System.ComponentModel.DataAnnotations;

namespace ShippingExitSystem.DTOs
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Supervisor = "Supervisor";
        public const string Inspector = "Inspector";
        public const string Managers = Admin + "," + Supervisor;

        public static readonly string[] All = { Admin, Supervisor, Inspector };
        public static bool IsValid(string? role) => role != null && All.Contains(role);
    }

    public class RegisterDto
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string Password { get; set; } = string.Empty;

        public string? Role { get; set; }
    }

    public class LoginDto
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class UpdateUserDto
    {
        [StringLength(50, MinimumLength = 3)]
        public string? Username { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Role { get; set; }

        public bool? IsActive { get; set; }
    }
}