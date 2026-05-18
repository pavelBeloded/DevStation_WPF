using DevStation.Configuration;
using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using DevStation.Utils;
using DevStation.ViewModels.Base;
using System.Text;

namespace DevStation.ViewModels;

public class CreateSnippetViewModel : ViewModelBase
{
    private readonly ISnippetService       _snippetService;
    private readonly ICurrentUserService   _currentUser;
    private readonly Action                _navigateBack;
    private readonly Action<ViewModelBase> _navigateTo;
    private readonly Snippet?              _editingSnippet;

    private string _title        = string.Empty;
    private string _description  = string.Empty;
    private string _language     = "JavaScript";
    private string _tags         = string.Empty;
    private string _code         = string.Empty;
    private bool   _isSaving;
    private string _errorMessage = string.Empty;

    public bool   IsEditMode { get; }
    public string PageTitle  => IsEditMode ? "Edit Snippet" : "Create Snippet";

    public string Title
    {
        get => _title;
        set { SetProperty(ref _title, value); OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(FileName)); }
    }
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
    public string Language
    {
        get => _language;
        set { SetProperty(ref _language, value); OnPropertyChanged(nameof(FileExtension)); }
    }
    public string Tags
    {
        get => _tags;
        set
        {
            SetProperty(ref _tags, value);
            OnPropertyChanged(nameof(HasTags));
            OnPropertyChanged(nameof(TagItems));
        }
    }
    public string Code
    {
        get => _code;
        set
        {
            SetProperty(ref _code, value);
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(LineCount));
            OnPropertyChanged(nameof(CodeSizeText));
        }
    }
    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
    }
    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

    public bool CanSave => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Code) && !IsSaving;
    public bool HasTags => !string.IsNullOrWhiteSpace(Tags);

    public IEnumerable<string> TagItems =>
        Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public string FileName      => string.IsNullOrWhiteSpace(_title) ? "untitled" : _title;
    public string FileExtension => LanguageToExtension(_language);
    public int    LineCount     => _code.Split('\n').Length;
    public string CodeSizeText
    {
        get
        {
            var bytes = Encoding.UTF8.GetByteCount(_code);
            return bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024.0:F1} KB";
        }
    }

    private static string LanguageToExtension(string? lang) => lang?.ToLowerInvariant() switch
    {
        "typescript"         => ".ts",
        "javascript"         => ".js",
        "python"             => ".py",
        "csharp" or "c#"     => ".cs",
        "java"               => ".java",
        "go"                 => ".go",
        "rust"               => ".rs",
        "css"                => ".css",
        "html"               => ".html",
        "sql"                => ".sql",
        _                    => ".txt"
    };

    public List<string> Languages { get; }

    public RelayCommand NavigateBackCommand    { get; }
    public RelayCommand SaveDraftCommand       { get; }
    public RelayCommand SaveAndSubmitCommand   { get; }

    public CreateSnippetViewModel(
        Snippet? editingSnippet,
        ISnippetService snippetService, ICurrentUserService currentUser,
        Action navigateBack, Action<ViewModelBase> navigateTo,
        AppSettings settings)
    {
        _editingSnippet = editingSnippet;
        _snippetService = snippetService;
        _currentUser    = currentUser;
        _navigateBack   = navigateBack;
        _navigateTo     = navigateTo;
        IsEditMode      = editingSnippet != null;
        Languages       = settings.Languages;

        if (editingSnippet != null)
        {
            _title       = editingSnippet.Title;
            _description = editingSnippet.Description ?? string.Empty;
            _language    = editingSnippet.Language ?? "JavaScript";
            _tags        = editingSnippet.Tags ?? string.Empty;
            _code        = editingSnippet.Code;
        }

        NavigateBackCommand  = new RelayCommand(_navigateBack);
        SaveDraftCommand     = new RelayCommand(async () => await SaveAsync(false), () => CanSave);
        SaveAndSubmitCommand = new RelayCommand(async () => await SaveAsync(true),  () => CanSave);
    }

    private async Task SaveAsync(bool submitForReview)
    {
        if (_currentUser.CurrentUser == null) return;
        ErrorMessage = string.Empty;
        IsSaving = true;
        try
        {
            if (IsEditMode)
            {
                await _snippetService.UpdateSnippetAsync(
                    _editingSnippet!.Id, Title, Code, Language, Description, Tags);
                ToastService.Show("Changes saved!");
            }
            else
            {
                var snippet = await _snippetService.CreateSnippetAsync(
                    _currentUser.CurrentUser.Id, Title, Code, Language, Description, Tags);
                if (submitForReview)
                {
                    await _snippetService.SubmitForReviewAsync(snippet.Id);
                    ToastService.Show("Snippet submitted for review!");
                }
                else
                {
                    ToastService.Show("Draft saved!");
                }
            }
            _navigateBack();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.InnerException?.Message ?? ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
