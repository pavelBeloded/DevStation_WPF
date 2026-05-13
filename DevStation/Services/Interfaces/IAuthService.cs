using DevStation.Data.Models;

namespace DevStation.Services.Interfaces;

public interface IAuthService
{
    /// <summary>Authenticates user and sets current session.</summary>
    Task<User?> LoginAsync(string username, string password);

    /// <summary>Creates a new user account.</summary>
    Task<User> RegisterAsync(string username, string email, string password, UserRole role = UserRole.User);

    /// <summary>Clears the current session.</summary>
    Task LogoutAsync();

    /// <summary>Restores a previously saved session from disk. Returns true if successful.</summary>
    Task<bool> TryRestoreSessionAsync();
}
