using ShippingExitSystem.DTOs;
using ShippingExitSystem.Models;

namespace ShippingExitSystem.Services
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(RegisterDto dto, bool callerIsAdmin);
        Task<string> LoginAsync(LoginDto dto);
        string GenerateJwtToken(User user);
    }
}