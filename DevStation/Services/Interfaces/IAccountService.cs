using DevStation.Data.Models;

namespace DevStation.Services.Interfaces;

public class UserStats
{
    public int TotalSnippets     { get; set; }
    public int InstalledSnippets { get; set; }
    public int PublishedSnippets { get; set; }
    public int PendingSnippets   { get; set; }
}

public interface IAccountService
{
    Task<bool>      UpdateUsernameAsync(int userId, string newUsername);
    Task<bool>      UpdatePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<UserStats> GetUserStatsAsync(int userId);
    Task<List<User>> GetAllUsersAsync();
    Task<bool>      PromoteToAdminAsync(int targetUserId, int requestingUserId);
    Task<bool>      DemoteToUserAsync(int targetUserId, int requestingUserId);
}
