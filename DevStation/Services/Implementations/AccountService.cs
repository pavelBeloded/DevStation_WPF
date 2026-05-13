using BCrypt.Net;
using DevStation.Data.DbContext;
using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevStation.Services.Implementations;

public class AccountService : IAccountService
{
    private readonly DevStationDbContext _context;

    public AccountService(DevStationDbContext context) => _context = context;

    public async Task<bool> UpdateUsernameAsync(int userId, string newUsername)
    {
        var exists = await _context.Users
            .AnyAsync(u => u.Username == newUsername && u.Id != userId);
        if (exists) return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Username = newUsername;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserStats> GetUserStatsAsync(int userId)
    {
        return new UserStats
        {
            TotalSnippets = await _context.Snippets
                .CountAsync(s => s.CreatorId == userId),

            InstalledSnippets = await _context.UserSnippets
                .CountAsync(us => us.UserId == userId && us.Snippet.CreatorId != userId),

            PublishedSnippets = await _context.Snippets
                .CountAsync(s => s.CreatorId == userId && s.Status == SnippetStatus.Published),

            PendingSnippets = await _context.Snippets
                .CountAsync(s => s.CreatorId == userId && s.Status == SnippetStatus.PendingReview),
        };
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<bool> PromoteToAdminAsync(int targetUserId, int requestingUserId)
    {
        var requester = await _context.Users.FindAsync(requestingUserId);
        if (requester?.Role != UserRole.Admin) return false;
        if (targetUserId == requestingUserId) return false;

        var target = await _context.Users.FindAsync(targetUserId);
        if (target == null) return false;

        target.Role = UserRole.Admin;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DemoteToUserAsync(int targetUserId, int requestingUserId)
    {
        var requester = await _context.Users.FindAsync(requestingUserId);
        if (requester?.Role != UserRole.Admin) return false;
        if (targetUserId == requestingUserId) return false;

        var target = await _context.Users.FindAsync(targetUserId);
        if (target == null) return false;

        target.Role = UserRole.User;
        await _context.SaveChangesAsync();
        return true;
    }
}
