using DevStation.Configuration;
using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using DevStation.Utils;
using DevStation.ViewModels.Base;
using System.Text;

namespace DevStation.ViewModels;

public class SnippetDetailViewModel : ViewModelBase
{
    private readonly ISnippetService       _snippetService;
    private readonly ICurrentUserService   _currentUser;
    private readonly Action                _navigateBack;
    private readonly Action<ViewModelBase> _navigateTo;
    private readonly AppSettings           _settings;

    private Snippet _snippet;
    private bool    _isInstalled;
    private bool    _isFavorite;

    public Snippet Snippet
    {
        get => _snippet;
        private set
        {
            SetProperty(ref _snippet, value);
            RefreshComputedProps();
        }
    }

    public bool IsInstalled
    {
        get => _isInstalled;
        set { SetProperty(ref _isInstalled, value); RefreshComputedProps(); }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        private set => SetProperty(ref _isFavorite, value);
    }

    public bool CanFavorite => IsInstalled && !IsOwner;

    public bool IsOwner        => _snippet?.CreatorId == _currentUser.CurrentUser?.Id;
    public bool HasDescription => !string.IsNullOrWhiteSpace(_snippet?.Description);
    public bool HasTags        => !string.IsNullOrWhiteSpace(_snippet?.Tags);
    public bool HasLanguage    => !string.IsNullOrWhiteSpace(_snippet?.Language);
    public bool CanInstall     => !IsOwner && !IsInstalled && _snippet?.Status == SnippetStatus.Published;
    public bool CanUse         => IsInstalled || (IsOwner && _snippet?.Status == SnippetStatus.Published);
    public bool CanEditAndSubmit => IsOwner && _snippet?.Status == SnippetStatus.Draft;
    public bool AwaitingReview => IsOwner && _snippet?.Status == SnippetStatus.PendingReview;
    public bool WasRejected    => IsOwner && _snippet?.Status == SnippetStatus.Rejected;
    public bool WasPublished   => IsOwner && _snippet?.Status == SnippetStatus.Published;
    public bool CanUninstall   => IsInstalled && !IsOwner;
    public bool CanAdminEdit   => _currentUser.IsAdmin && !IsOwner && _snippet?.Status == SnippetStatus.PendingReview;
    public bool CanAdminDelete => _currentUser.IsAdmin && !CanEditAndSubmit;

    public string AuthorName   => _snippet?.Creator?.DisplayName ?? _snippet?.Creator?.Username ?? "—";
    public int    LineCount    => _snippet?.Code?.Split('\n').Length ?? 0;
    public int    InstallCount => _snippet?.UserSnippets?.Count ?? 0;
    public string CreatedAtText => _snippet?.CreatedAt.ToString("yyyy-MM-dd") ?? "—";
    public string StatusText   => _snippet?.Status.ToString() ?? "—";
    public string FileName     => _snippet?.Title ?? "snippet";
    public string FileExtension => LanguageToExtension(_snippet?.Language);
    public string CodeSizeText
    {
        get
        {
            var bytes = Encoding.UTF8.GetByteCount(_snippet?.Code ?? "");
            return bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024.0:F1} KB";
        }
    }

    private static string LanguageToExtension(string? lang) => lang?.ToLowerInvariant() switch
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

    public IEnumerable<string> TagItems =>
        _snippet?.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? [];

    public string? RejectionComment =>
        _snippet?.Reviews.FirstOrDefault(r => r.Status == ReviewStatus.Rejected)?.ReviewComment;

    public RelayCommand NavigateBackCommand    { get; }
    public RelayCommand InstallCommand         { get; }
    public RelayCommand UseSnippetCommand      { get; }
    public RelayCommand UninstallCommand       { get; }
    public RelayCommand ToggleFavoriteCommand  { get; }
    public RelayCommand EditCommand            { get; }
    public RelayCommand SubmitForReviewCommand { get; }
    public RelayCommand DeleteCommand          { get; }
    public RelayCommand AdminDeleteCommand     { get; }

    public SnippetDetailViewModel(
        Snippet snippet, bool isInstalled, bool isFavorite,
        ISnippetService snippetService, ICurrentUserService currentUser,
        Action navigateBack, Action<ViewModelBase> navigateTo,
        AppSettings settings)
    {
        _snippet        = snippet;
        _isInstalled    = isInstalled;
        _isFavorite     = isFavorite;
        _snippetService = snippetService;
        _currentUser    = currentUser;
        _navigateBack   = navigateBack;
        _navigateTo     = navigateTo;
        _settings       = settings;

        NavigateBackCommand = new RelayCommand(_navigateBack);

        InstallCommand = new RelayCommand(async () =>
        {
            if (_currentUser.CurrentUser == null) return;
            await _snippetService.InstallSnippetAsync(_currentUser.CurrentUser.Id, _snippet.Id);
            IsInstalled = true;
            ToastService.Show("Snippet installed!");
        });

        UseSnippetCommand = new RelayCommand(() =>
        {
            System.Windows.Clipboard.SetText(_snippet.Code);
            ToastService.Show("Copied to clipboard!");
        });

        EditCommand = new RelayCommand(() =>
        {
            var editVm = new CreateSnippetViewModel(
                _snippet, _snippetService, _currentUser,
                () => _navigateTo(this), _navigateTo, _settings);
            _navigateTo(editVm);
        });

        SubmitForReviewCommand = new RelayCommand(async () =>
        {
            await _snippetService.SubmitForReviewAsync(_snippet.Id);
            _snippet.Status = SnippetStatus.PendingReview;
            RefreshComputedProps();
            ToastService.Show("Submitted for review!");
        });

        UninstallCommand = new RelayCommand(async () =>
        {
            if (_currentUser.CurrentUser == null) return;
            await _snippetService.UninstallSnippetAsync(_currentUser.CurrentUser.Id, _snippet.Id);
            ToastService.Show("Removed from your library.");
            _navigateBack();
        });

        ToggleFavoriteCommand = new RelayCommand(async () =>
        {
            if (_currentUser.CurrentUser == null) return;
            IsFavorite = await _snippetService.ToggleFavoriteAsync(_currentUser.CurrentUser.Id, _snippet.Id);
            ToastService.Show(IsFavorite ? "Added to favorites!" : "Removed from favorites.");
        });

        DeleteCommand = new RelayCommand(async () =>
        {
            await _snippetService.DeleteSnippetAsync(_snippet.Id);
            ToastService.Show("Snippet deleted.");
            _navigateBack();
        });

        AdminDeleteCommand = new RelayCommand(async () =>
        {
            await _snippetService.DeleteSnippetAsync(_snippet.Id);
            ToastService.Show("Snippet permanently deleted.");
            _navigateBack();
        });
    }

    private void RefreshComputedProps()
    {
        OnPropertyChanged(nameof(IsOwner));
        OnPropertyChanged(nameof(HasDescription));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(HasLanguage));
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(CanUse));
        OnPropertyChanged(nameof(CanEditAndSubmit));
        OnPropertyChanged(nameof(AwaitingReview));
        OnPropertyChanged(nameof(WasRejected));
        OnPropertyChanged(nameof(WasPublished));
        OnPropertyChanged(nameof(CanUninstall));
        OnPropertyChanged(nameof(CanAdminEdit));
        OnPropertyChanged(nameof(CanAdminDelete));
        OnPropertyChanged(nameof(CanFavorite));
        OnPropertyChanged(nameof(TagItems));
        OnPropertyChanged(nameof(RejectionComment));
        OnPropertyChanged(nameof(AuthorName));
        OnPropertyChanged(nameof(LineCount));
        OnPropertyChanged(nameof(CodeSizeText));
        OnPropertyChanged(nameof(InstallCount));
        OnPropertyChanged(nameof(CreatedAtText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(FileExtension));
    }
}
