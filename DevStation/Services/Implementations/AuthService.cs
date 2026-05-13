using DevStation.Data.DbContext;
using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;

namespace DevStation.Services.Implementations;

public class AuthService : IAuthService
{
    private static readonly string SessionFilePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevStation", "session.json");

    private readonly DevStationDbContext  _context;
    private readonly ICurrentUserService _currentUserService;

    public AuthService(DevStationDbContext context, ICurrentUserService currentUserService)
    {
        _context            = context;
        _currentUserService = currentUserService;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _currentUserService.SetUser(user);
        await SaveSessionAsync(user.Id);
        return user;
    }

    public async Task<User> RegisterAsync(string username, string email, string password, UserRole role = UserRole.User)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException("Username already taken.");

        if (await _context.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Email already registered.");

        var isFirstUser = !await _context.Users.AnyAsync();

        var user = new User
        {
            Username     = username,
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role         = isFirstUser ? UserRole.Admin : UserRole.User,
            DisplayName  = username
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _currentUserService.SetUser(user);
        await SaveSessionAsync(user.Id);
        return user;
    }

    public Task LogoutAsync()
    {
        _currentUserService.SetUser(null);
        ClearSession();
        return Task.CompletedTask;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            if (!File.Exists(SessionFilePath)) return false;

            var json   = await File.ReadAllTextAsync(SessionFilePath);
            var doc    = JsonDocument.Parse(json);
            var userId = doc.RootElement.GetProperty("userId").GetInt32();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _currentUserService.SetUser(user);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task SaveSessionAsync(int userId)
    {
        var dir = Path.GetDirectoryName(SessionFilePath)!;
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(SessionFilePath,
            JsonSerializer.Serialize(new { userId }));
    }

    private static void ClearSession()
    {
        try
        {
            if (File.Exists(SessionFilePath))
                File.Delete(SessionFilePath);
        }
        catch { }
    }
}
