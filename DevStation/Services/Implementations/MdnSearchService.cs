using DevStation.Data.DbContext;
using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevStation.Services.Implementations;

file class MdnApiResponse
{
    [JsonPropertyName("documents")]
    public List<MdnDocument> Documents { get; set; } = [];
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

file class MdnDocument
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("mdn_url")]
    public string Mdn_Url { get; set; } = string.Empty;
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

file class MdnDocumentResponse
{
    [JsonPropertyName("doc")]
    public MdnDoc? Doc { get; set; }
}

file class MdnDoc
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
    [JsonPropertyName("body")]
    public List<MdnBodyItem>? Body { get; set; }
}

file class MdnBodyItem
{
    [JsonPropertyName("value")]
    public MdnBodyValue? Value { get; set; }
}

file class MdnBodyValue
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public class MdnSearchService : IMdnSearchService
{
    private readonly DevStationDbContext _context;
    private readonly HttpClient          _httpClient;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MdnSearchService(DevStationDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context    = context;
        _httpClient = httpClientFactory.CreateClient("MDN");
    }

    public async Task<List<MdnSearchResult>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var cached = await _context.MdnCache
            .Where(m => m.SearchTerm.Contains(query) || m.Title.Contains(query))
            .OrderByDescending(m => m.CachedAt)
            .Take(10)
            .ToListAsync();

        if (cached.Count > 0)
        {
            return cached.Select(c => new MdnSearchResult
            {
                Title       = c.Title,
                Url         = c.Url,
                Summary     = c.Summary,
                Category    = c.Category,
                IsFromCache = true
            }).ToList();
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"https://developer.mozilla.org/api/v1/search?q={Uri.EscapeDataString(query)}&locale=en-US");

            if (!response.IsSuccessStatusCode)
                return [];

            var json        = await response.Content.ReadAsStringAsync();
            var mdnResponse = JsonSerializer.Deserialize<MdnApiResponse>(json, _jsonOptions);

            if (mdnResponse?.Documents == null || mdnResponse.Documents.Count == 0)
                return [];

            var results = new List<MdnSearchResult>();

            foreach (var doc in mdnResponse.Documents.Take(10))
            {
                var category = ExtractCategory(doc.Mdn_Url);
                var exists   = await _context.MdnCache
                    .AnyAsync(m => m.SearchTerm == query && m.Url == doc.Mdn_Url);

                if (!exists)
                {
                    _context.MdnCache.Add(new MdnCache
                    {
                        SearchTerm = query,
                        Title      = doc.Title,
                        Url        = doc.Mdn_Url,
                        Summary    = doc.Summary,
                        Category   = category,
                        CachedAt   = DateTime.UtcNow
                    });
                }

                results.Add(new MdnSearchResult
                {
                    Title       = doc.Title,
                    Url         = doc.Mdn_Url,
                    Summary     = doc.Summary,
                    Category    = category,
                    IsFromCache = false
                });
            }

            await _context.SaveChangesAsync();
            return results;
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<MdnDetails> GetDetailsAsync(string url)
    {
        var slug = ExtractSlug(url);
        if (string.IsNullOrEmpty(slug))
            return await FallbackDetailsAsync(url);

        try
        {
            var response = await _httpClient.GetAsync(
                $"https://developer.mozilla.org/en-US/docs/{slug}/index.json");

            if (response.IsSuccessStatusCode)
            {
                var json    = await response.Content.ReadAsStringAsync();
                var details = ParseDetails(json, slug);
                if (details != null) return details;
            }
        }
        catch { }

        return await FallbackDetailsAsync(url, slug);
    }

    private async Task<MdnDetails> FallbackDetailsAsync(string url, string? slug = null)
    {
        var cached = await _context.MdnCache.FirstOrDefaultAsync(m => m.Url == url);
        return new MdnDetails
        {
            Title    = cached?.Title    ?? string.Empty,
            Url      = url,
            Summary  = cached?.Summary,
            Category = cached?.Category,
            Slug     = slug ?? ExtractSlug(url)
        };
    }

    private MdnDetails? ParseDetails(string json, string slug)
    {
        try
        {
            var doc = JsonSerializer.Deserialize<MdnDocumentResponse>(json, _jsonOptions);
            if (doc?.Doc == null) return null;

            var syntaxSection      = doc.Doc.Body?
                .FirstOrDefault(b => b.Value?.Title == "Syntax")?.Value?.Content;
            var descriptionSection = doc.Doc.Body?
                .FirstOrDefault(b => b.Value?.Title == "Description")?.Value?.Content;
            var examplesSection    = doc.Doc.Body?
                .FirstOrDefault(b => b.Value?.Title == "Examples")?.Value?.Content;

            return new MdnDetails
            {
                Title       = doc.Doc.Title ?? string.Empty,
                Slug        = slug,
                Summary     = StripHtml(doc.Doc.Summary),
                Syntax      = StripHtml(syntaxSection),
                Description = StripHtml(descriptionSection),
                Examples    = StripHtml(examplesSection)
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nodes = doc.DocumentNode.SelectNodes("//br|//p|//li|//dt|//dd");
        if (nodes != null)
            foreach (var node in nodes)
                node.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), node);

        var text = doc.DocumentNode.InnerText;
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }

    private static string? ExtractSlug(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        const string marker = "/docs/";
        var idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? url[(idx + marker.Length)..] : null;
    }

    public async Task<int> GetTotalSearchCountAsync() =>
        await _context.MdnCache.Select(m => m.SearchTerm).Distinct().CountAsync();

    private static string ExtractCategory(string url)
    {
        if (url.Contains("/JavaScript/")) return "javascript";
        if (url.Contains("/CSS/"))        return "css";
        if (url.Contains("/HTML/"))       return "html";
        if (url.Contains("/API/"))        return "web-api";
        return "other";
    }
}
