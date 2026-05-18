using DevStation.Configuration;
using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using DevStation.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace DevStation.ViewModels;

public class RecentSnippetItem
{
    private static readonly Dictionary<string, Color> LangColors = new()
    {
        ["javascript"] = Color.FromRgb(0xF7, 0xDF, 0x1E),
        ["js"]         = Color.FromRgb(0xF7, 0xDF, 0x1E),
        ["typescript"] = Color.FromRgb(0x31, 0x78, 0xC6),
        ["ts"]         = Color.FromRgb(0x31, 0x78, 0xC6),
        ["css"]        = Color.FromRgb(0x26, 0x4D, 0xE4),
        ["python"]     = Color.FromRgb(0x37, 0x76, 0xAB),
        ["py"]         = Color.FromRgb(0x37, 0x76, 0xAB),
        ["html"]       = Color.FromRgb(0xE3, 0x4F, 0x26),
        ["go"]         = Color.FromRgb(0x00, 0xAD, 0xD8),
        ["rust"]       = Color.FromRgb(0xCE, 0x42, 0x2B),
        ["sql"]        = Color.FromRgb(0xCC, 0x29, 0x27),
        ["csharp"]     = Color.FromRgb(0x68, 0x21, 0xEF),
        ["c#"]         = Color.FromRgb(0x68, 0x21, 0xEF),
    };

    public string Title      { get; }
    public string Badge      { get; }
    public string TimeAgo    { get; }
    public bool   IsFavorite { get; }
    public SolidColorBrush BadgeBrush { get; }

    public RecentSnippetItem(UserSnippet us)
    {
        Title      = us.Snippet?.Title ?? string.Empty;
        IsFavorite = us.IsFavorite;
        TimeAgo    = GetTimeAgo(us.AddedAt);
        var lang   = us.Snippet?.Language;
        Badge      = GetBadge(lang);
        BadgeBrush = GetBadgeBrush(lang);
    }

    private static string GetBadge(string? lang) => lang?.ToLowerInvariant() switch
    {
        "typescript"     => "TS",
        "javascript"     => "JS",
        "python"         => "PY",
        "csharp" or "c#" => "C#",
        "go"             => "GO",
        "rust"           => "RS",
        "html"           => "HTML",
        "css"            => "CSS",
        "sql"            => "SQL",
        null or ""       => "??",
        var l            => l.Length > 4 ? l[..4].ToUpperInvariant() : l.ToUpperInvariant()
    };

    private static SolidColorBrush GetBadgeBrush(string? lang)
    {
        if (lang != null && LangColors.TryGetValue(lang.ToLowerInvariant(), out var c))
            return new SolidColorBrush(c);
        return new SolidColorBrush(Color.FromRgb(0xFF, 0xB8, 0x6C));
    }

    private static string GetTimeAgo(DateTime dt)
    {
        var diff = DateTime.UtcNow - dt.ToUniversalTime();
        if (diff.TotalMinutes < 1)  return "только что";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин. назад";
        if (diff.TotalHours  < 24)  return $"{(int)diff.TotalHours} ч. назад";
        if (diff.TotalDays   < 2)   return "вчера";
        if (diff.TotalDays   < 7)   return $"{(int)diff.TotalDays} дн. назад";
        return dt.ToLocalTime().ToString("dd.MM.yyyy");
    }
}

public class HomeViewModel : ViewModelBase
{
    private readonly ISnippetService       _snippetService;
    private readonly IMdnSearchService     _mdnService;
    private readonly ICurrentUserService   _currentUser;
    private readonly Action<string>        _navigateByKey;
    private readonly Action<ViewModelBase> _navigateTo;
    private readonly AppSettings           _settings;

    private int    _snippetCount;
    private int    _authoredCount;
    private int    _favoritesCount;
    private int    _mdnCount;
    private string _lastSnippetTitle       = string.Empty;
    private string _lastSnippetCodePreview = string.Empty;
    private bool   _hasLastSnippet;
    private ObservableCollection<RecentSnippetItem> _recentSnippets = [];

    public int SnippetCount
    {
        get => _snippetCount;
        private set => SetProperty(ref _snippetCount, value);
    }
    public int AuthoredCount
    {
        get => _authoredCount;
        private set => SetProperty(ref _authoredCount, value);
    }
    public int FavoritesCount
    {
        get => _favoritesCount;
        private set => SetProperty(ref _favoritesCount, value);
    }
    public int MdnCount
    {
        get => _mdnCount;
        private set => SetProperty(ref _mdnCount, value);
    }
    public string LastSnippetTitle
    {
        get => _lastSnippetTitle;
        private set => SetProperty(ref _lastSnippetTitle, value);
    }
    public string LastSnippetCodePreview
    {
        get => _lastSnippetCodePreview;
        private set => SetProperty(ref _lastSnippetCodePreview, value);
    }
    public bool HasLastSnippet
    {
        get => _hasLastSnippet;
        private set => SetProperty(ref _hasLastSnippet, value);
    }
    public ObservableCollection<RecentSnippetItem> RecentSnippets
    {
        get => _recentSnippets;
        private set
        {
            SetProperty(ref _recentSnippets, value);
            OnPropertyChanged(nameof(HasRecentSnippets));
        }
    }
    public bool HasRecentSnippets => _recentSnippets.Count > 0;

    public string UserName => _currentUser.CurrentUser?.DisplayName
                           ?? _currentUser.CurrentUser?.Username
                           ?? "User";

    public string UserInitials
    {
        get
        {
            var name = UserName;
            if (string.IsNullOrWhiteSpace(name)) return "U";
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length >= 2
                ? $"{words[0][0]}{words[^1][0]}".ToUpperInvariant()
                : name[..Math.Min(2, name.Length)].ToUpperInvariant();
        }
    }

    public RelayCommand         CreateSnippetCommand { get; }
    public RelayCommand<string> NavigateCommand      { get; }

    public HomeViewModel(
        ISnippetService       snippetService,
        IMdnSearchService     mdnService,
        ICurrentUserService   currentUser,
        Action<string>        navigateByKey,
        Action<ViewModelBase> navigateTo,
        AppSettings           settings)
    {
        _snippetService = snippetService;
        _mdnService     = mdnService;
        _currentUser    = currentUser;
        _navigateByKey  = navigateByKey;
        _navigateTo     = navigateTo;
        _settings       = settings;

        NavigateCommand = new RelayCommand<string>(key =>
        {
            if (!string.IsNullOrEmpty(key)) _navigateByKey(key);
        });

        CreateSnippetCommand = new RelayCommand(() =>
        {
            var vm = new CreateSnippetViewModel(
                null, _snippetService, _currentUser,
                () => { _ = LoadDataAsync(); _navigateTo(this); },
                _navigateTo, _settings);
            _navigateTo(vm);
        });

        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        if (_currentUser.CurrentUser == null) return;
        var uid = _currentUser.CurrentUser.Id;

        SnippetCount   = await _snippetService.GetUserSnippetCountAsync(uid);
        AuthoredCount  = await _snippetService.GetUserAuthoredCountAsync(uid);
        FavoritesCount = await _snippetService.GetFavoritesCountAsync(uid);
        MdnCount       = await _mdnService.GetTotalSearchCountAsync();

        var recent = await _snippetService.GetRecentUserSnippetsAsync(uid, 4);
        RecentSnippets = new ObservableCollection<RecentSnippetItem>(
            recent.Select(us => new RecentSnippetItem(us)));

        if (recent.Count > 0 && recent[0].Snippet != null)
        {
            LastSnippetTitle       = recent[0].Snippet.Title;
            var code               = recent[0].Snippet.Code ?? string.Empty;
            LastSnippetCodePreview = string.Join("\n", code.Split('\n').Take(3));
            HasLastSnippet         = true;
        }
        else
        {
            LastSnippetTitle       = string.Empty;
            LastSnippetCodePreview = string.Empty;
            HasLastSnippet         = false;
        }
    }
}
