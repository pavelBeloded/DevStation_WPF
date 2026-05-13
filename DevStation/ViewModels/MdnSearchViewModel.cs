using DevStation.Services.Interfaces;
using DevStation.Utils;
using DevStation.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace DevStation.ViewModels;

public class MdnSearchViewModel : ViewModelBase
{
    private readonly IMdnSearchService _mdnService;

    private string                               _searchQuery    = string.Empty;
    private ObservableCollection<MdnSearchResult> _searchResults = [];
    private MdnSearchResult?                     _selectedResult;
    private MdnDetails?                          _selectedDetails;
    private bool                                 _isLoading;
    private bool                                 _isLoadingDetails;
    private bool                                 _isOffline;
    private string                               _errorMessage   = string.Empty;
    private CancellationTokenSource?             _debounceToken;

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            SetProperty(ref _searchQuery, value);
            _ = SearchWithDebounceAsync(value);
        }
    }

    public ObservableCollection<MdnSearchResult> SearchResults
    {
        get => _searchResults;
        private set
        {
            SetProperty(ref _searchResults, value);
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(HasNoResults));
        }
    }

    public MdnSearchResult? SelectedResult
    {
        get => _selectedResult;
        private set
        {
            if (_selectedResult != null) _selectedResult.IsSelected = false;
            SetProperty(ref _selectedResult, value);
            if (_selectedResult != null) _selectedResult.IsSelected = true;
            OnPropertyChanged(nameof(IsResultSelected));
            OnPropertyChanged(nameof(FullMdnUrl));
        }
    }

    public MdnDetails? SelectedDetails
    {
        get => _selectedDetails;
        private set
        {
            SetProperty(ref _selectedDetails, value);
            OnPropertyChanged(nameof(HasSyntax));
            OnPropertyChanged(nameof(HasDescription));
            OnPropertyChanged(nameof(HasExamples));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsLoadingDetails
    {
        get => _isLoadingDetails;
        private set => SetProperty(ref _isLoadingDetails, value);
    }

    public bool IsOffline
    {
        get => _isOffline;
        private set => SetProperty(ref _isOffline, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool HasResults      => _searchResults.Count > 0;
    public bool HasNoResults    => _searchResults.Count == 0 && !string.IsNullOrEmpty(_searchQuery) && !_isLoading;
    public bool IsResultSelected => _selectedResult != null;
    public bool HasSyntax       => !string.IsNullOrWhiteSpace(_selectedDetails?.Syntax);
    public bool HasDescription  => !string.IsNullOrWhiteSpace(_selectedDetails?.Description);
    public bool HasExamples     => !string.IsNullOrWhiteSpace(_selectedDetails?.Examples);

    public string? FullMdnUrl => _selectedResult?.Slug != null
        ? $"https://developer.mozilla.org/en-US/docs/{_selectedResult.Slug}"
        : null;

    public RelayCommand                    ClearCommand          { get; }
    public RelayCommand                    CopyUrlCommand        { get; }
    public RelayCommand                    CopySummaryCommand    { get; }
    public RelayCommand                    CopyExamplesCommand   { get; }
    public RelayCommand                    OpenInBrowserCommand  { get; }
    public RelayCommand<MdnSearchResult>   SelectResultCommand   { get; }

    public MdnSearchViewModel(IMdnSearchService mdnService)
    {
        _mdnService = mdnService;

        ClearCommand = new RelayCommand(() =>
        {
            _debounceToken?.Cancel();
            _searchQuery    = string.Empty;
            OnPropertyChanged(nameof(SearchQuery));
            SearchResults   = [];
            SelectedResult  = null;
            SelectedDetails = null;
            IsLoading       = false;
        });

        CopyUrlCommand = new RelayCommand(
            () =>
            {
                Clipboard.SetText(FullMdnUrl ?? _selectedResult?.FullUrl ?? string.Empty);
                ToastService.Show("URL copied!");
            },
            () => _selectedResult != null);

        CopySummaryCommand = new RelayCommand(
            () =>
            {
                Clipboard.SetText(_selectedDetails?.Summary ?? _selectedResult?.Summary ?? string.Empty);
                ToastService.Show("Summary copied!");
            },
            () => _selectedResult != null);

        CopyExamplesCommand = new RelayCommand(
            () =>
            {
                Clipboard.SetText(_selectedDetails?.Examples ?? string.Empty);
                ToastService.Show("Examples copied!");
            },
            () => HasExamples);

        OpenInBrowserCommand = new RelayCommand(
            () =>
            {
                var url = FullMdnUrl ?? _selectedResult?.FullUrl;
                if (string.IsNullOrEmpty(url)) return;
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                ToastService.Show("Opening in browser…");
            },
            () => _selectedResult != null);

        SelectResultCommand = new RelayCommand<MdnSearchResult>(async result =>
        {
            SelectedResult = result;
            if (result != null)
                await LoadDetailsAsync(result);
        });
    }

    public void LoadExternalResults(string query, List<MdnSearchResult> results, MdnSearchResult? selected = null)
    {
        _debounceToken?.Cancel();
        _searchQuery = query;
        OnPropertyChanged(nameof(SearchQuery));
        SearchResults   = new ObservableCollection<MdnSearchResult>(results);
        SelectedResult  = selected ?? SearchResults.FirstOrDefault();
        SelectedDetails = null;
        IsLoading       = false;

        if (SelectedResult != null)
            _ = LoadDetailsAsync(SelectedResult);
    }

    private async Task LoadDetailsAsync(MdnSearchResult result)
    {
        IsLoadingDetails = true;
        SelectedDetails  = null;
        try
        {
            var details = await _mdnService.GetDetailsAsync(result.Url ?? string.Empty);
            SelectedDetails = details;
        }
        catch { }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task SearchWithDebounceAsync(string query)
    {
        _debounceToken?.Cancel();
        _debounceToken = new CancellationTokenSource();
        var token = _debounceToken.Token;

        try
        {
            await Task.Delay(400, token);

            if (string.IsNullOrWhiteSpace(query))
            {
                SearchResults   = [];
                SelectedResult  = null;
                SelectedDetails = null;
                IsLoading       = false;
                return;
            }

            IsLoading    = true;
            ErrorMessage = string.Empty;

            var results = await _mdnService.SearchAsync(query);

            if (token.IsCancellationRequested) return;

            SearchResults  = new ObservableCollection<MdnSearchResult>(results);
            SelectedResult = SearchResults.FirstOrDefault();
            IsOffline      = results.Count > 0 && results.All(r => r.IsFromCache);

            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(HasNoResults));
            OnPropertyChanged(nameof(IsResultSelected));

            if (SelectedResult != null)
                await LoadDetailsAsync(SelectedResult);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            if (!(_debounceToken?.IsCancellationRequested ?? true))
                IsLoading = false;
        }
    }
}
