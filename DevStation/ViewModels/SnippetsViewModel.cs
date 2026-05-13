using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using DevStation.Utils;
using DevStation.ViewModels.Base;
using System.Collections.ObjectModel;

namespace DevStation.ViewModels;

public class SnippetCardItem : ViewModelBase
{
    private bool _isInstalled;

    public Snippet Snippet    { get; }
    public bool IsInstalled
    {
        get => _isInstalled;
        set => SetProperty(ref _isInstalled, value);
    }

    public SnippetCardItem(Snippet snippet, bool isInstalled)
    {
        Snippet      = snippet;
        _isInstalled = isInstalled;
    }

    public IEnumerable<string> TagItems =>
        Snippet.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? [];

    public bool   HasTags      => !string.IsNullOrWhiteSpace(Snippet.Tags);
    public string AuthorName   => Snippet.Creator?.DisplayName ?? Snippet.Creator?.Username ?? "—";
    public string FileName     => Snippet.Title ?? "snippet";
    public string FileExtension => LanguageToExt(Snippet.Language);
    public int    InstallCount => Snippet.UserSnippets?.Count ?? 0;

    private static string LanguageToExt(string? lang) => lang?.ToLowerInvariant() switch
    {
        "typescript" or "ts" => ".ts",
        "javascript" or "js" => ".js",
        "python" or "py"     => ".py",
        "csharp" or "c#"     => ".cs",
        "java"               => ".java",
        "go"                 => ".go",
        "rust"               => ".rs",
        "css"                => ".css",
        "html"               => ".html",
        "sql"                => ".sql",
        _                    => ".txt"
    };
}

public class SnippetsViewModel : ViewModelBase
{
    private readonly ISnippetService       _snippetService;
    private readonly ICurrentUserService   _currentUser;
    private readonly Action<ViewModelBase> _navigateTo;

    private ObservableCollection<SnippetCardItem> _publicSnippets = [];
    private ObservableCollection<UserSnippet>     _myLibrary      = [];
    private ObservableCollection<Snippet>         _pendingReviews = [];

    private string _searchQuery     = string.Empty;
    private string _selectedLanguage = "All";
    private string _selectedFilter   = "All";
    private bool   _isGlobalTab      = true;
    private bool   _isLoading;
    private int     _totalSnippetsCount;
    private int     _installedCount;
    private int     _authoredCount;

    public bool IsAdmin       => _currentUser.IsAdmin;
    public bool IsMyLibraryTab => !_isGlobalTab;

    public ObservableCollection<SnippetCardItem> PublicSnippets
    {
        get => _publicSnippets;
        private set => SetProperty(ref _publicSnippets, value);
    }
    public ObservableCollection<UserSnippet> MyLibrary
    {
        get => _myLibrary;
        private set => SetProperty(ref _myLibrary, value);
    }
    public ObservableCollection<Snippet> PendingReviews
    {
        get => _pendingReviews;
        private set { SetProperty(ref _pendingReviews, value); OnPropertyChanged(nameof(PendingCount)); }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set { SetProperty(ref _searchQuery, value); _ = LoadDataAsync(); }
    }
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set { SetProperty(ref _selectedLanguage, value); _ = LoadDataAsync(); }
    }
    public string SelectedFilter
    {
        get => _selectedFilter;
        set { SetProperty(ref _selectedFilter, value); _ = LoadDataAsync(); }
    }
    public bool IsGlobalTab
    {
        get => _isGlobalTab;
        set { SetProperty(ref _isGlobalTab, value); OnPropertyChanged(nameof(IsMyLibraryTab)); }
    }
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    public int TotalSnippetsCount
    {
        get => _totalSnippetsCount;
        private set => SetProperty(ref _totalSnippetsCount, value);
    }
    public int InstalledCount
    {
        get => _installedCount;
        private set => SetProperty(ref _installedCount, value);
    }
    public int AuthoredCount
    {
        get => _authoredCount;
        private set => SetProperty(ref _authoredCount, value);
    }

    public string PendingCount => PendingReviews.Count.ToString("D2");

    public List<string> Languages      { get; } = ["All", "JavaScript", "TypeScript", "CSS", "HTML", "Python", "SQL"];
    public List<string> FilterOptions  { get; } = ["All", "Global", "Local", "Pending", "Published"];

    public RelayCommand              ShowGlobalTabCommand      { get; }
    public RelayCommand              ShowMyLibraryTabCommand   { get; }
    public RelayCommand<string>      SelectFilterCommand       { get; }
    public RelayCommand<SnippetCardItem> InstallCommand        { get; }
    public RelayCommand<UserSnippet> UseSnippetCommand         { get; }
    public RelayCommand<SnippetCardItem> OpenDetailCommand     { get; }
    public RelayCommand<UserSnippet> OpenLibraryDetailCommand  { get; }
    public RelayCommand<Snippet>     OpenPendingDetailCommand  { get; }
    public RelayCommand<Snippet>     AdminEditPendingCommand   { get; }
    public RelayCommand              CreateSnippetCommand      { get; }
    public RelayCommand<Snippet>     ApproveCommand            { get; }
    public RelayCommand<Snippet>     RejectCommand             { get; }

    public SnippetsViewModel(
        ISnippetService       snippetService,
        ICurrentUserService   currentUser,
        Action<ViewModelBase> navigateTo)
    {
        _snippetService = snippetService;
        _currentUser    = currentUser;
        _navigateTo     = navigateTo;

        ShowGlobalTabCommand    = new RelayCommand(() => IsGlobalTab = true);
        ShowMyLibraryTabCommand = new RelayCommand(() => IsGlobalTab = false);
        SelectFilterCommand     = new RelayCommand<string>(f => SelectedFilter = f ?? "All");

        InstallCommand = new RelayCommand<SnippetCardItem>(async card =>
        {
            if (card == null || _currentUser.CurrentUser == null) return;
            await _snippetService.InstallSnippetAsync(_currentUser.CurrentUser.Id, card.Snippet.Id);
            card.IsInstalled = true;
            ToastService.Show("Added to your library!");
            await LoadDataAsync();
        });

        UseSnippetCommand = new RelayCommand<UserSnippet>(us =>
        {
            if (us?.Snippet.Code is { } code)
            {
                System.Windows.Clipboard.SetText(code);
                ToastService.Show("Copied to clipboard!");
                _ = _snippetService.IncrementViewCountAsync(us.SnippetId);
            }
        });

        OpenDetailCommand = new RelayCommand<SnippetCardItem>(async card =>
        {
            if (card == null) return;
            var isFav = card.IsInstalled
                ? _myLibrary.FirstOrDefault(us => us.SnippetId == card.Snippet.Id)?.IsFavorite ?? false
                : false;
            await OpenDetailAsync(card.Snippet, card.IsInstalled, isFav);
        });

        OpenLibraryDetailCommand = new RelayCommand<UserSnippet>(async us =>
        {
            if (us == null) return;
            await OpenDetailAsync(us.Snippet, isInstalled: true, us.IsFavorite);
        });

        OpenPendingDetailCommand = new RelayCommand<Snippet>(async s =>
        {
            if (s == null) return;
            var fresh = await _snippetService.GetByIdAsync(s.Id) ?? s;
            var detailVm = new SnippetDetailViewModel(
                fresh, isInstalled: false, isFavorite: false,
                _snippetService, _currentUser,
                () => { _ = LoadDataAsync(); _navigateTo(this); }, _navigateTo);
            _navigateTo(detailVm);
        });

        AdminEditPendingCommand = new RelayCommand<Snippet>(async s =>
        {
            if (s == null) return;
            var fresh = await _snippetService.GetByIdAsync(s.Id) ?? s;
            var editVm = new CreateSnippetViewModel(
                fresh, _snippetService, _currentUser,
                () => { _ = LoadDataAsync(); _navigateTo(this); },
                _navigateTo);
            _navigateTo(editVm);
        });

        CreateSnippetCommand = new RelayCommand(OpenCreatePage);

        ApproveCommand = new RelayCommand<Snippet>(async s =>
        {
            if (s == null || _currentUser.CurrentUser == null) return;
            await _snippetService.ReviewSnippetAsync(s.Id, _currentUser.CurrentUser.Id, ReviewStatus.Approved);
            ToastService.Show("Snippet approved and published!");
            await LoadDataAsync();
        });
        RejectCommand = new RelayCommand<Snippet>(async s =>
        {
            if (s == null || _currentUser.CurrentUser == null) return;
            await _snippetService.ReviewSnippetAsync(s.Id, _currentUser.CurrentUser.Id, ReviewStatus.Rejected);
            ToastService.Show("Snippet rejected.");
            await LoadDataAsync();
        });

        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var lang  = SelectedLanguage == "All" ? null : SelectedLanguage;
            var query = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;

            // Load installed IDs for the current user to mark cards
            var installedIds = new HashSet<int>();
            if (_currentUser.CurrentUser != null)
            {
                var lib = await _snippetService.GetUserLibraryAsync(_currentUser.CurrentUser.Id);
                foreach (var us in lib) installedIds.Add(us.SnippetId);
            }

            var snippets = await _snippetService.GetPublicSnippetsAsync(query, lang);
            PublicSnippets = new ObservableCollection<SnippetCardItem>(
                snippets.Select(s => new SnippetCardItem(s, installedIds.Contains(s.Id))));
            TotalSnippetsCount = snippets.Count();
            AuthoredCount      = snippets.Count(s => s.CreatorId == _currentUser.CurrentUser?.Id);

            if (_currentUser.CurrentUser != null)
            {
                var myLib = await _snippetService.GetUserLibraryAsync(
                    _currentUser.CurrentUser.Id, query, lang, SelectedFilter);
                MyLibrary = new ObservableCollection<UserSnippet>(myLib);
                InstalledCount = myLib.Count();
            }

            if (IsAdmin)
            {
                var pending = await _snippetService.GetPendingReviewSnippetsAsync();
                PendingReviews = new ObservableCollection<Snippet>(pending);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OpenDetailAsync(Snippet snippet, bool isInstalled, bool isFavorite = false)
    {
        await _snippetService.IncrementViewCountAsync(snippet.Id);
        var fresh = await _snippetService.GetByIdAsync(snippet.Id) ?? snippet;

        var detailVm = new SnippetDetailViewModel(
            fresh, isInstalled, isFavorite, _snippetService, _currentUser,
            () => { _ = LoadDataAsync(); _navigateTo(this); }, _navigateTo);

        _navigateTo(detailVm);
    }

    private void OpenCreatePage()
    {
        var createVm = new CreateSnippetViewModel(
            null, _snippetService, _currentUser,
            () => { _ = LoadDataAsync(); _navigateTo(this); },
            _navigateTo);
        _navigateTo(createVm);
    }
}
