using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using System.Diagnostics;

namespace DevStation.Services.Implementations;

public class LoggingSnippetService(ISnippetService inner) : ISnippetService
{
    private static void Log(string message) =>
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] [SnippetService] {message}");

    public async Task<IEnumerable<Snippet>> GetPublicSnippetsAsync(string? searchQuery = null, string? language = null)
    {
        Log($"GetPublicSnippets(query={searchQuery}, lang={language})");
        var result = await inner.GetPublicSnippetsAsync(searchQuery, language);
        var list = result.ToList();
        Log($"GetPublicSnippets → {list.Count} results");
        return list;
    }

    public async Task<IEnumerable<Snippet>> GetPendingReviewSnippetsAsync()
    {
        Log("GetPendingReviewSnippets()");
        var result = await inner.GetPendingReviewSnippetsAsync();
        var list = result.ToList();
        Log($"GetPendingReviewSnippets → {list.Count} results");
        return list;
    }

    public async Task<IEnumerable<UserSnippet>> GetUserLibraryAsync(int userId, string? searchQuery = null, string? language = null, string? filter = null)
    {
        Log($"GetUserLibrary(userId={userId}, query={searchQuery}, filter={filter})");
        var result = await inner.GetUserLibraryAsync(userId, searchQuery, language, filter);
        var list = result.ToList();
        Log($"GetUserLibrary → {list.Count} results");
        return list;
    }

    public async Task<IEnumerable<UserSnippet>> SearchUserLibraryAsync(int userId, string query)
    {
        Log($"SearchUserLibrary(userId={userId}, query={query})");
        var result = await inner.SearchUserLibraryAsync(userId, query);
        var list = result.ToList();
        Log($"SearchUserLibrary → {list.Count} results");
        return list;
    }

    public async Task<Snippet?> GetByIdAsync(int snippetId)
    {
        Log($"GetById(id={snippetId})");
        var result = await inner.GetByIdAsync(snippetId);
        Log($"GetById → {(result != null ? $"found \"{result.Title}\"" : "not found")}");
        return result;
    }

    public async Task<Snippet> CreateSnippetAsync(int creatorId, string title, string code, string? language = null, string? description = null, string? tags = null)
    {
        Log($"CreateSnippet(creatorId={creatorId}, title=\"{title}\", lang={language})");
        var result = await inner.CreateSnippetAsync(creatorId, title, code, language, description, tags);
        Log($"CreateSnippet → created id={result.Id}");
        return result;
    }

    public async Task<Snippet?> UpdateSnippetAsync(int snippetId, string title, string code, string? language = null, string? description = null, string? tags = null)
    {
        Log($"UpdateSnippet(id={snippetId}, title=\"{title}\")");
        var result = await inner.UpdateSnippetAsync(snippetId, title, code, language, description, tags);
        Log($"UpdateSnippet → {(result != null ? "updated" : "not found")}");
        return result;
    }

    public async Task<bool> DeleteSnippetAsync(int snippetId)
    {
        Log($"DeleteSnippet(id={snippetId})");
        var result = await inner.DeleteSnippetAsync(snippetId);
        Log($"DeleteSnippet → {(result ? "deleted" : "not found")}");
        return result;
    }

    public async Task<bool> InstallSnippetAsync(int userId, int snippetId)
    {
        Log($"InstallSnippet(userId={userId}, snippetId={snippetId})");
        var result = await inner.InstallSnippetAsync(userId, snippetId);
        Log($"InstallSnippet → {(result ? "installed" : "already exists")}");
        return result;
    }

    public async Task UninstallSnippetAsync(int userId, int snippetId)
    {
        Log($"UninstallSnippet(userId={userId}, snippetId={snippetId})");
        await inner.UninstallSnippetAsync(userId, snippetId);
        Log("UninstallSnippet → done");
    }

    public Task<bool> IsInstalledAsync(int userId, int snippetId) =>
        inner.IsInstalledAsync(userId, snippetId);

    public async Task<bool> ToggleFavoriteAsync(int userId, int snippetId)
    {
        Log($"ToggleFavorite(userId={userId}, snippetId={snippetId})");
        var result = await inner.ToggleFavoriteAsync(userId, snippetId);
        Log($"ToggleFavorite → isFavorite={result}");
        return result;
    }

    public Task<int> GetUserSnippetCountAsync(int userId) =>
        inner.GetUserSnippetCountAsync(userId);

    public Task<int> GetUserAuthoredCountAsync(int userId) =>
        inner.GetUserAuthoredCountAsync(userId);

    public Task<int> GetFavoritesCountAsync(int userId) =>
        inner.GetFavoritesCountAsync(userId);

    public Task<List<UserSnippet>> GetRecentUserSnippetsAsync(int userId, int count = 4) =>
        inner.GetRecentUserSnippetsAsync(userId, count);

    public async Task SubmitForReviewAsync(int snippetId)
    {
        Log($"SubmitForReview(id={snippetId})");
        await inner.SubmitForReviewAsync(snippetId);
        Log("SubmitForReview → submitted");
    }

    public async Task ReviewSnippetAsync(int snippetId, int reviewerId, ReviewStatus status, string? comment = null)
    {
        Log($"ReviewSnippet(id={snippetId}, reviewer={reviewerId}, status={status})");
        await inner.ReviewSnippetAsync(snippetId, reviewerId, status, comment);
        Log("ReviewSnippet → done");
    }

    public async Task IncrementViewCountAsync(int snippetId)
    {
        await inner.IncrementViewCountAsync(snippetId);
        Log($"IncrementViewCount(id={snippetId})");
    }
}
