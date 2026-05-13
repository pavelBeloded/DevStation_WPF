using DevStation.Data.Models;

namespace DevStation.Services.Interfaces;

public interface ISnippetService
{
    Task<IEnumerable<Snippet>> GetPublicSnippetsAsync(string? searchQuery = null, string? language = null);
    Task<IEnumerable<Snippet>> GetPendingReviewSnippetsAsync();
    Task<IEnumerable<UserSnippet>> GetUserLibraryAsync(int userId, string? searchQuery = null, string? language = null, string? filter = null);
    Task<IEnumerable<UserSnippet>> SearchUserLibraryAsync(int userId, string query);

    Task<Snippet?> GetByIdAsync(int snippetId);
    Task<Snippet> CreateSnippetAsync(int creatorId, string title, string code, string? language = null, string? description = null, string? tags = null);
    Task<Snippet?> UpdateSnippetAsync(int snippetId, string title, string code, string? language = null, string? description = null, string? tags = null);
    Task<bool> DeleteSnippetAsync(int snippetId);

    Task<bool> InstallSnippetAsync(int userId, int snippetId);
    Task UninstallSnippetAsync(int userId, int snippetId);
    Task<bool> IsInstalledAsync(int userId, int snippetId);
    Task<bool> ToggleFavoriteAsync(int userId, int snippetId);
    Task<int> GetUserSnippetCountAsync(int userId);
    Task<int> GetUserAuthoredCountAsync(int userId);
    Task<int> GetFavoritesCountAsync(int userId);
    Task<List<UserSnippet>> GetRecentUserSnippetsAsync(int userId, int count = 4);

    Task SubmitForReviewAsync(int snippetId);
    Task ReviewSnippetAsync(int snippetId, int reviewerId, ReviewStatus status, string? comment = null);
    Task IncrementViewCountAsync(int snippetId);
}
