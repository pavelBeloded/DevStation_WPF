using DevStation.Configuration;
using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using DevStation.ViewModels.Base;
using DevStation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace DevStation.ViewModels;

public class SearchResultItem : ViewModelBase
{
    private bool _isSelected;

    public UserSnippet UserSnippet { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public SearchResultItem(UserSnippet us) => UserSnippet = us;
}

public class MainWindowViewModel : ViewModelBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthService        _authService;
    private readonly ISnippetService     _snippetService;
    private readonly IMdnSearchService   _mdnSearchService;
    private readonly IAccountService     _accountService;
    private readonly Func<LoginWindow>   _loginWindowFactory;

    private readonly AppSettings                        _settings;
    private readonly Dictionary<string, ViewModelBase> _pageCache = [];
    private ViewModelBase _currentPage;
    private string        _currentPageKey = "Home";

    private string                                _searchQuery   = string.Empty;
    private ObservableCollection<SearchResultItem>  _searchResults = [];
    private ObservableCollection<MdnSearchResult>   _mdnResults    = [];
    private bool                                   _isMdnLoading;
    private CancellationTokenSource?               _mdnDebounce;

    public User?  CurrentUser => _currentUserService.CurrentUser;
    public string DisplayName => CurrentUser?.DisplayName ?? CurrentUser?.Username ?? "User";
    public bool   IsAdmin     => _currentUserService.IsAdmin;

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }
    public string CurrentPageKey
    {
        get => _currentPageKey;
        private set => SetProperty(ref _currentPageKey, value ?? "Home");
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            SetProperty(ref _searchQuery, value);
            _ = SearchAsync(value);
        }
    }

    public ObservableCollection<SearchResultItem> SearchResults
    {
        get => _searchResults;
        private set
        {
            SetProperty(ref _searchResults, value);
            OnPropertyChanged(nameof(IsSearchOpen));
            OnPropertyChanged(nameof(HasNoResults));
            OnPropertyChanged(nameof(IsAnySearchOpen));
        }
    }

    public ObservableCollection<MdnSearchResult> MdnResults
    {
        get => _mdnResults;
        private set
        {
            SetProperty(ref _mdnResults, value);
            OnPropertyChanged(nameof(IsMdnSearchOpen));
            OnPropertyChanged(nameof(HasNoMdnResults));
            OnPropertyChanged(nameof(IsAnySearchOpen));
        }
    }

    public bool IsMdnLoading
    {
        get => _isMdnLoading;
        private set
        {
            SetProperty(ref _isMdnLoading, value);
            OnPropertyChanged(nameof(IsMdnSearchOpen));
            OnPropertyChanged(nameof(HasNoMdnResults));
            OnPropertyChanged(nameof(IsAnySearchOpen));
        }
    }

    public bool HasDollarQuery   => _searchQuery.StartsWith('$') && _searchQuery.Length > 1;
    public bool HasQuestionQuery => _searchQuery.StartsWith('?') && _searchQuery.Length > 1;

    public bool IsSearchOpen    => _searchResults.Any() && HasDollarQuery;
    public bool HasNoResults    => !_searchResults.Any() && HasDollarQuery;
    public bool IsMdnSearchOpen => (_mdnResults.Any() || _isMdnLoading) && HasQuestionQuery;
    public bool HasNoMdnResults => !_mdnResults.Any() && !_isMdnLoading && HasQuestionQuery;
    public bool IsAnySearchOpen => HasDollarQuery || HasQuestionQuery;

    public RelayCommand                    LogoutCommand         { get; }
    public RelayCommand<string>            NavigateCommand       { get; }
    public RelayCommand                    UseFirstResultCommand { get; }
    public RelayCommand<SearchResultItem>  UseResultCommand      { get; }
    public RelayCommand<MdnSearchResult>   OpenMdnResultCommand  { get; }

    public MainWindowViewModel(
        ICurrentUserService currentUserService,
        IAuthService        authService,
        ISnippetService     snippetService,
        IMdnSearchService   mdnSearchService,
        IAccountService     accountService,
        Func<LoginWindow>   loginWindowFactory,
        AppSettings         settings)
    {
        _currentUserService = currentUserService;
        _authService        = authService;
        _snippetService     = snippetService;
        _mdnSearchService   = mdnSearchService;
        _accountService     = accountService;
        _loginWindowFactory = loginWindowFactory;
        _settings           = settings;

        _currentPage = GetOrCreatePage("Home");

        LogoutCommand   = new RelayCommand(async () => await ExecuteLogoutAsync());
        NavigateCommand = new RelayCommand<string>(NavigateTo);

        // Enter in search box — use keyboard-selected item, or first
        UseFirstResultCommand = new RelayCommand(() => CopyAndClose(
            _searchResults.FirstOrDefault(r => r.IsSelected) ?? _searchResults.FirstOrDefault()));

        // Mouse click on a specific snippet item
        UseResultCommand = new RelayCommand<SearchResultItem>(item => CopyAndClose(item));

        // Click on MDN result → navigate to MDN Search page with results pre-loaded
        OpenMdnResultCommand = new RelayCommand<MdnSearchResult>(result =>
        {
            if (result == null) return;
            var queryText  = _searchQuery.StartsWith('?') ? _searchQuery[1..].Trim() : string.Empty;
            var allResults = _mdnResults.ToList();

            // Clear query directly (avoids SearchAsync re-triggering and wiping allResults)
            _searchQuery = string.Empty;
            OnPropertyChanged(nameof(SearchQuery));
            MdnResults    = [];
            IsMdnLoading  = false;

            NavigateTo("MdnSearch");
            if (CurrentPage is MdnSearchViewModel mdnVm)
                mdnVm.LoadExternalResults(queryText, allResults, result);
        });
    }

    // ── Keyboard navigation ────────────────────────────────────────────
    public void SelectNextResult()     => MoveSelection(+1);
    public void SelectPreviousResult() => MoveSelection(-1);

    public void SelectNextMdnResult()     => MoveMdnSelection(+1);
    public void SelectPreviousMdnResult() => MoveMdnSelection(-1);

    public void OpenSelectedMdnResult()
    {
        var result = _mdnResults.FirstOrDefault(r => r.IsSelected) ?? _mdnResults.FirstOrDefault();
        if (result != null)
            OpenMdnResultCommand.Execute(result);
    }

    private void MoveSelection(int delta)
    {
        if (_searchResults.Count == 0) return;

        var current = _searchResults.FirstOrDefault(r => r.IsSelected);
        int nextIdx;

        if (current == null)
        {
            nextIdx = delta > 0 ? 0 : _searchResults.Count - 1;
        }
        else
        {
            current.IsSelected = false;
            var idx = _searchResults.IndexOf(current);
            nextIdx = (idx + delta + _searchResults.Count) % _searchResults.Count;
        }

        _searchResults[nextIdx].IsSelected = true;
    }

    private void MoveMdnSelection(int delta)
    {
        if (_mdnResults.Count == 0) return;

        var current = _mdnResults.FirstOrDefault(r => r.IsSelected);
        int nextIdx;

        if (current == null)
        {
            nextIdx = delta > 0 ? 0 : _mdnResults.Count - 1;
        }
        else
        {
            current.IsSelected = false;
            var idx = _mdnResults.IndexOf(current);
            nextIdx = (idx + delta + _mdnResults.Count) % _mdnResults.Count;
        }

        _mdnResults[nextIdx].IsSelected = true;
    }

    private void CopyAndClose(SearchResultItem? item)
    {
        if (item?.UserSnippet?.Snippet?.Code is not { } code) return;
        Clipboard.SetText(code);
        SearchQuery = string.Empty;
        Utils.ToastService.Show("Copied to clipboard!");
    }

    // ── Navigation ────────────────────────────────────────────────────
    private void NavigateTo(string? pageKey)
    {
        CurrentPageKey = pageKey ?? "Home";
        CurrentPage    = GetOrCreatePage(CurrentPageKey);

        if (CurrentPage is AccountViewModel accountVm)
            _ = accountVm.LoadAllAsync();
    }

    public void NavigateToVm(ViewModelBase vm) => CurrentPage = vm;

    private ViewModelBase GetOrCreatePage(string key)
    {
        if (_pageCache.TryGetValue(key, out var cached))
            return cached;

        var page = key switch
        {
            "Home"           => (ViewModelBase)new HomeViewModel(_snippetService, _mdnSearchService, _currentUserService, NavigateTo, NavigateToVm, _settings),
            "Converter"      => new ConverterViewModel(),
            "MdnSearch"      => new MdnSearchViewModel(_mdnSearchService),
            "ImageOptimizer" => new ImageOptimizerViewModel(_settings),
            "MockGenerator"  => new MockGeneratorViewModel(),
            "Snippets"       => new SnippetsViewModel(_snippetService, _currentUserService, NavigateToVm, _settings),
            "Account"        => new AccountViewModel(_accountService, _currentUserService),
            "Documentation"  => new DocumentationViewModel(),
            _                => new HomeViewModel(_snippetService, _mdnSearchService, _currentUserService, NavigateTo, NavigateToVm, _settings)
        };

        _pageCache[key] = page;
        return page;
    }

    private async Task SearchAsync(string query)
    {
        OnPropertyChanged(nameof(HasDollarQuery));
        OnPropertyChanged(nameof(HasQuestionQuery));
        OnPropertyChanged(nameof(IsSearchOpen));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(IsMdnSearchOpen));
        OnPropertyChanged(nameof(HasNoMdnResults));
        OnPropertyChanged(nameof(IsAnySearchOpen));

        if (query.StartsWith('$') && query.Length > 1 && _currentUserService.CurrentUser != null)
        {
            _mdnDebounce?.Cancel();
            MdnResults   = [];
            IsMdnLoading = false;

            var actualQuery = query[1..].Trim();
            if (string.IsNullOrWhiteSpace(actualQuery)) { SearchResults = []; return; }

            var results = await _snippetService.SearchUserLibraryAsync(
                _currentUserService.CurrentUser.Id, actualQuery);
            SearchResults = new ObservableCollection<SearchResultItem>(
                results.Select(us => new SearchResultItem(us)));
        }
        else if (query.StartsWith('?') && query.Length > 1)
        {
            SearchResults = [];
            var actualQuery = query[1..].Trim();
            if (string.IsNullOrWhiteSpace(actualQuery))
            {
                _mdnDebounce?.Cancel();
                MdnResults   = [];
                IsMdnLoading = false;
                return;
            }
            await SearchMdnWithDebounceAsync(actualQuery);
        }
        else
        {
            _mdnDebounce?.Cancel();
            SearchResults = [];
            MdnResults    = [];
            IsMdnLoading  = false;
        }
    }

    private async Task SearchMdnWithDebounceAsync(string query)
    {
        _mdnDebounce?.Cancel();
        _mdnDebounce = new CancellationTokenSource();
        var token = _mdnDebounce.Token;
        try
        {
            IsMdnLoading = true;
            await Task.Delay(_settings.DebounceDelayMs, token);
            if (token.IsCancellationRequested) return;

            var results = await _mdnSearchService.SearchAsync(query);
            if (token.IsCancellationRequested) return;

            MdnResults = new ObservableCollection<MdnSearchResult>(results.Take(_settings.MdnSearchLimit));
        }
        catch (TaskCanceledException) { }
        catch { MdnResults = []; }
        finally
        {
            if (!(_mdnDebounce?.IsCancellationRequested ?? true))
                IsMdnLoading = false;
        }
    }

    private async Task ExecuteLogoutAsync()
    {
        _pageCache.Clear();
        await _authService.LogoutAsync();

        var loginWindow = _loginWindowFactory();
        loginWindow.Show();
        Application.Current.Windows.OfType<MainWindow>().FirstOrDefault()?.Close();
    }
}
