using DevStation.ViewModels.Base;

namespace DevStation.Services.Interfaces;

public class MdnSearchResult : ViewModelBase
{
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string  Title       { get; set; } = string.Empty;
    public string? Url         { get; set; }
    public string? Summary     { get; set; }
    public string? Category    { get; set; }
    public bool    IsFromCache { get; set; }

    public string FullUrl => string.IsNullOrEmpty(Url)
        ? string.Empty
        : Url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? Url
          : $"https://developer.mozilla.org{Url}";

    public string? Slug
    {
        get
        {
            if (string.IsNullOrEmpty(Url)) return null;
            const string marker = "/docs/";
            var idx = Url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? Url[(idx + marker.Length)..] : null;
        }
    }

    public string BreadcrumbText
    {
        get
        {
            if (string.IsNullOrEmpty(Url)) return Category ?? string.Empty;
            var parts   = Url.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var webIdx  = Array.IndexOf(parts, "Web");
            if (webIdx < 0 || webIdx + 2 >= parts.Length) return Category ?? string.Empty;
            var crumbs  = parts.Skip(webIdx + 1).SkipLast(1).ToArray();
            return string.Join(" › ", crumbs);
        }
    }

    public bool HasBreadcrumb => !string.IsNullOrEmpty(BreadcrumbText);

    // True when the title looks like a method/function call
    public bool HasCodeHint => !string.IsNullOrEmpty(Title) &&
                               (Title.Contains('(') || Title.Contains('.'));
}

public class MdnDetails
{
    public string  Title       { get; set; } = string.Empty;
    public string? Url         { get; set; }
    public string? Summary     { get; set; }
    public string? Category    { get; set; }
    public string? Slug        { get; set; }
    public string? Syntax      { get; set; }
    public string? Description { get; set; }
    public string? Examples    { get; set; }
}

public interface IMdnSearchService
{
    Task<List<MdnSearchResult>> SearchAsync(string query);
    Task<MdnDetails>            GetDetailsAsync(string url);
    Task<int>                   GetTotalSearchCountAsync();
}
