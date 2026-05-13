using DevStation.Data.DbContext;
using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevStation.Services.Implementations;

public class SnippetService : ISnippetService
{
    private readonly DevStationDbContext _context;

    public SnippetService(DevStationDbContext context) => _context = context;

    public async Task<IEnumerable<Snippet>> GetPublicSnippetsAsync(string? searchQuery = null, string? language = null)
    {
        var query = _context.Snippets
            .Where(s => s.IsPublic && s.Status == SnippetStatus.Published)
            .Include(s => s.Creator)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
            query = query.Where(s => s.Title.Contains(searchQuery) ||
                                     (s.Description != null && s.Description.Contains(searchQuery)) ||
                                     (s.Tags != null && s.Tags.Contains(searchQuery)));

        if (!string.IsNullOrWhiteSpace(language))
            query = query.Where(s => s.Language == language);

        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<Snippet>> GetPendingReviewSnippetsAsync()
    {
        return await _context.Snippets
            .Where(s => s.Status == SnippetStatus.PendingReview)
            .Include(s => s.Creator)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserSnippet>> GetUserLibraryAsync(
        int userId, string? searchQuery = null, string? language = null, string? filter = null)
    {
        var query = _context.UserSnippets
            .Where(us => us.UserId == userId)
            .Include(us => us.Snippet).ThenInclude(s => s.Creator)
            .AsQueryable();

        query = filter switch
        {
            "Global"    => query.Where(us => us.Snippet.IsPublic && us.Snippet.CreatorId != userId),
            "Local"     => query.Where(us => us.Snippet.CreatorId == userId),
            "Pending"   => query.Where(us => us.Snippet.CreatorId == userId && us.Snippet.Status == SnippetStatus.PendingReview),
            "Published" => query.Where(us => us.Snippet.CreatorId == userId && us.Snippet.Status == SnippetStatus.Published),
            _           => query
        };

        if (!string.IsNullOrWhiteSpace(searchQuery))
            query = query.Where(us => us.Snippet.Title.Contains(searchQuery) ||
                                      (us.Snippet.Tags != null && us.Snippet.Tags.Contains(searchQuery)));

        if (!string.IsNullOrWhiteSpace(language))
            query = query.Where(us => us.Snippet.Language == language);

        return await query.OrderByDescending(us => us.AddedAt).ToListAsync();
    }

    public async Task<IEnumerable<UserSnippet>> SearchUserLibraryAsync(int userId, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        return await _context.UserSnippets
            .Where(us => us.UserId == userId &&
                         (us.Snippet.Title.Contains(query) ||
                          (us.Snippet.Tags != null && us.Snippet.Tags.Contains(query))))
            .Include(us => us.Snippet)
            .Take(8)
            .ToListAsync();
    }

    public async Task<Snippet?> GetByIdAsync(int snippetId)
    {
        return await _context.Snippets
            .Include(s => s.Creator)
            .Include(s => s.Reviews).ThenInclude(r => r.Reviewer)
            .FirstOrDefaultAsync(s => s.Id == snippetId);
    }

    public async Task<Snippet> CreateSnippetAsync(int creatorId, string title, string code,
        string? language = null, string? description = null, string? tags = null)
    {
        var snippet = new Snippet
        {
            CreatorId = creatorId,
            Title = title,
            Code = code,
            Language = language,
            Description = description,
            Tags = tags
        };
        _context.Snippets.Add(snippet);
        await _context.SaveChangesAsync();

        _context.UserSnippets.Add(new UserSnippet { UserId = creatorId, SnippetId = snippet.Id });
        await _context.SaveChangesAsync();
        return snippet;
    }

    public async Task<Snippet?> UpdateSnippetAsync(int snippetId, string title, string code,
        string? language = null, string? description = null, string? tags = null)
    {
        var snippet = await _context.Snippets.FindAsync(snippetId);
        if (snippet == null) return null;

        snippet.Title = title;
        snippet.Code = code;
        snippet.Language = language;
        snippet.Description = description;
        snippet.Tags = tags;
        snippet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return snippet;
    }

    public async Task<bool> DeleteSnippetAsync(int snippetId)
    {
        var snippet = await _context.Snippets.FindAsync(snippetId);
        if (snippet == null) return false;
        _context.Snippets.Remove(snippet);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> InstallSnippetAsync(int userId, int snippetId)
    {
        var exists = await _context.UserSnippets
            .AnyAsync(us => us.UserId == userId && us.SnippetId == snippetId);
        if (exists) return false;

        _context.UserSnippets.Add(new UserSnippet { UserId = userId, SnippetId = snippetId });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task UninstallSnippetAsync(int userId, int snippetId)
    {
        var us = await _context.UserSnippets
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SnippetId == snippetId);
        if (us != null)
        {
            _context.UserSnippets.Remove(us);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsInstalledAsync(int userId, int snippetId)
    {
        return await _context.UserSnippets
            .AnyAsync(us => us.UserId == userId && us.SnippetId == snippetId);
    }

    public async Task<bool> ToggleFavoriteAsync(int userId, int snippetId)
    {
        var us = await _context.UserSnippets
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SnippetId == snippetId);
        if (us == null) return false;
        us.IsFavorite = !us.IsFavorite;
        await _context.SaveChangesAsync();
        return us.IsFavorite;
    }

    public async Task SubmitForReviewAsync(int snippetId)
    {
        var snippet = await _context.Snippets.FindAsync(snippetId);
        if (snippet == null) return;
        snippet.Status = SnippetStatus.PendingReview;
        snippet.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ReviewSnippetAsync(int snippetId, int reviewerId, ReviewStatus status, string? comment = null)
    {
        var snippet = await _context.Snippets.FindAsync(snippetId);
        if (snippet == null) return;

        snippet.Status = status == ReviewStatus.Approved ? SnippetStatus.Published : SnippetStatus.Rejected;
        if (status == ReviewStatus.Approved) snippet.IsPublic = true;
        snippet.UpdatedAt = DateTime.UtcNow;

        _context.SnippetReviews.Add(new SnippetReview
        {
            SnippetId = snippetId,
            ReviewerId = reviewerId,
            Status = status,
            ReviewComment = comment,
            ReviewedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task IncrementViewCountAsync(int snippetId)
    {
        var snippet = await _context.Snippets.FindAsync(snippetId);
        if (snippet == null) return;
        snippet.ViewCount++;
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetUserSnippetCountAsync(int userId) =>
        await _context.UserSnippets.CountAsync(us => us.UserId == userId);

    public async Task<int> GetUserAuthoredCountAsync(int userId) =>
        await _context.Snippets.CountAsync(s => s.CreatorId == userId);

    public async Task<int> GetFavoritesCountAsync(int userId) =>
        await _context.UserSnippets.CountAsync(us => us.UserId == userId && us.IsFavorite);

    public async Task<List<UserSnippet>> GetRecentUserSnippetsAsync(int userId, int count = 4) =>
        await _context.UserSnippets
            .Where(us => us.UserId == userId)
            .Include(us => us.Snippet)
            .OrderByDescending(us => us.AddedAt)
            .Take(count)
            .ToListAsync();
}
