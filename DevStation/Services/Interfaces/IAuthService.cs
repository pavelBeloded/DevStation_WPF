using DevStation.Data.Models;

namespace DevStation.Services.Interfaces;

public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);

    Task<User> RegisterAsync(string username, string email, string password, UserRole role = UserRole.User);

    Task LogoutAsync();

    Task<bool> TryRestoreSessionAsync();
}
