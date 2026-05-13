using System.Collections.ObjectModel;
using DevStation.Data.Models;
using DevStation.Services.Interfaces;
using DevStation.ViewModels.Base;

namespace DevStation.ViewModels;

public class AccountViewModel : ViewModelBase
{
    private readonly IAccountService    _accountService;
    private readonly ICurrentUserService _currentUser;

    private string    _newUsername     = string.Empty;
    private string    _usernameError   = string.Empty;
    private bool      _usernameSuccess;

    private string    _currentPassword = string.Empty;
    private string    _newPassword     = string.Empty;
    private string    _confirmPassword = string.Empty;
    private string    _passwordError   = string.Empty;
    private bool      _passwordSuccess;

    private UserStats _stats           = new();
    private bool      _isLoading;

    private ObservableCollection<User> _allUsers      = [];
    private ObservableCollection<User> _filteredUsers = [];
    private string  _userSearchQuery  = string.Empty;
    private string? _adminActionError;
    private string? _adminActionSuccess;

    // ── Profile (read-only) ──────────────────────────────────────────
    public string Username    => _currentUser.CurrentUser?.Username    ?? string.Empty;
    public string Email       => _currentUser.CurrentUser?.Email       ?? string.Empty;
    public string Role        => _currentUser.IsAdmin ? "Admin" : "User";
    public string MemberSince => _currentUser.CurrentUser?.CreatedAt
                                     .ToLocalTime().ToString("MMMM dd, yyyy") ?? string.Empty;
    public bool   IsAdmin     => _currentUser.IsAdmin;

    public string Initials
    {
        get
        {
            var name = _currentUser.CurrentUser?.DisplayName ?? _currentUser.CurrentUser?.Username ?? "U";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                : name[..Math.Min(2, name.Length)].ToUpperInvariant();
        }
    }

    // ── Change username ──────────────────────────────────────────────
    public string NewUsername
    {
        get => _newUsername;
        set { SetProperty(ref _newUsername, value); OnPropertyChanged(nameof(CanSaveUsername)); }
    }
    public string UsernameError
    {
        get => _usernameError;
        set { SetProperty(ref _usernameError, value); OnPropertyChanged(nameof(HasUsernameError)); }
    }
    public bool UsernameSuccess
    {
        get => _usernameSuccess;
        set => SetProperty(ref _usernameSuccess, value);
    }
    public bool HasUsernameError => !string.IsNullOrEmpty(_usernameError);
    public bool CanSaveUsername  => !string.IsNullOrWhiteSpace(_newUsername) && _newUsername != Username;

    // ── Change password ──────────────────────────────────────────────
    public string CurrentPassword
    {
        get => _currentPassword;
        set { SetProperty(ref _currentPassword, value); OnPropertyChanged(nameof(CanSavePassword)); }
    }
    public string NewPassword
    {
        get => _newPassword;
        set { SetProperty(ref _newPassword, value); OnPropertyChanged(nameof(CanSavePassword)); }
    }
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set { SetProperty(ref _confirmPassword, value); OnPropertyChanged(nameof(CanSavePassword)); }
    }
    public string PasswordError
    {
        get => _passwordError;
        set { SetProperty(ref _passwordError, value); OnPropertyChanged(nameof(HasPasswordError)); }
    }
    public bool PasswordSuccess
    {
        get => _passwordSuccess;
        set => SetProperty(ref _passwordSuccess, value);
    }
    public bool HasPasswordError => !string.IsNullOrEmpty(_passwordError);
    public bool CanSavePassword  =>
        !string.IsNullOrWhiteSpace(_currentPassword) &&
        !string.IsNullOrWhiteSpace(_newPassword) &&
        _newPassword == _confirmPassword;

    // ── Stats ────────────────────────────────────────────────────────
    public UserStats Stats
    {
        get => _stats;
        private set => SetProperty(ref _stats, value);
    }
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    // ── User management (admin only) ─────────────────────────────────
    public ObservableCollection<User> AllUsers
    {
        get => _allUsers;
        private set => SetProperty(ref _allUsers, value);
    }
    public ObservableCollection<User> FilteredUsers
    {
        get => _filteredUsers;
        private set => SetProperty(ref _filteredUsers, value);
    }
    public string UserSearchQuery
    {
        get => _userSearchQuery;
        set { SetProperty(ref _userSearchQuery, value); FilterUsers(); }
    }
    public string? AdminActionError
    {
        get => _adminActionError;
        set => SetProperty(ref _adminActionError, value);
    }
    public string? AdminActionSuccess
    {
        get => _adminActionSuccess;
        set => SetProperty(ref _adminActionSuccess, value);
    }

    // ── Commands ─────────────────────────────────────────────────────
    public RelayCommand         UpdateUsernameCommand { get; }
    public RelayCommand         UpdatePasswordCommand { get; }
    public RelayCommand<object> PromoteCommand        { get; }
    public RelayCommand<object> DemoteCommand         { get; }

    public AccountViewModel(IAccountService accountService, ICurrentUserService currentUser)
    {
        _accountService = accountService;
        _currentUser    = currentUser;
        _newUsername    = currentUser.CurrentUser?.Username ?? string.Empty;

        UpdateUsernameCommand = new RelayCommand(
            async () => await SaveUsernameAsync(),
            () => CanSaveUsername);

        UpdatePasswordCommand = new RelayCommand(
            async () => await SavePasswordAsync(),
            () => CanSavePassword);

        PromoteCommand = new RelayCommand<object>(async param =>
        {
            if (param is int id) await PromoteAsync(id);
        });

        DemoteCommand = new RelayCommand<object>(async param =>
        {
            if (param is int id) await DemoteAsync(id);
        });

        _ = LoadAllAsync();
    }

    public async Task LoadAllAsync()
    {
        await LoadStatsAsync();
        if (IsAdmin) await LoadUsersAsync();
    }

    public async Task LoadStatsAsync()
    {
        if (_currentUser.CurrentUser == null) return;
        IsLoading = true;
        try { Stats = await _accountService.GetUserStatsAsync(_currentUser.CurrentUser.Id); }
        finally { IsLoading = false; }
    }

    private async Task SaveUsernameAsync()
    {
        UsernameError   = string.Empty;
        UsernameSuccess = false;

        if (_newUsername.Length < 3)
        {
            UsernameError = "Username must be at least 3 characters";
            return;
        }

        var ok = await _accountService.UpdateUsernameAsync(
            _currentUser.CurrentUser!.Id, _newUsername);

        if (ok)
        {
            _currentUser.CurrentUser.Username = _newUsername;
            UsernameSuccess = true;
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(Initials));
        }
        else
        {
            UsernameError = "Username is already taken";
        }
    }

    public async Task LoadUsersAsync()
    {
        AdminActionError   = null;
        AdminActionSuccess = null;
        try
        {
            var users = await _accountService.GetAllUsersAsync();
            AllUsers = new ObservableCollection<User>(users);
            FilterUsers();
        }
        catch (Exception ex)
        {
            AdminActionError = $"Failed to load users: {ex.Message}";
        }
    }

    private void FilterUsers()
    {
        if (string.IsNullOrWhiteSpace(_userSearchQuery))
        {
            FilteredUsers = new ObservableCollection<User>(AllUsers);
            return;
        }
        FilteredUsers = new ObservableCollection<User>(
            AllUsers.Where(u =>
                u.Username.Contains(_userSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(_userSearchQuery, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task PromoteAsync(int targetUserId)
    {
        AdminActionError   = null;
        AdminActionSuccess = null;

        var ok = await _accountService.PromoteToAdminAsync(
            targetUserId, _currentUser.CurrentUser!.Id);

        if (ok)
        {
            AdminActionSuccess = "User promoted to Admin successfully";
            await LoadUsersAsync();
        }
        else
        {
            AdminActionError = "Failed to promote user";
        }
    }

    private async Task DemoteAsync(int targetUserId)
    {
        AdminActionError   = null;
        AdminActionSuccess = null;

        var ok = await _accountService.DemoteToUserAsync(
            targetUserId, _currentUser.CurrentUser!.Id);

        if (ok)
        {
            AdminActionSuccess = "User demoted to User successfully";
            await LoadUsersAsync();
        }
        else
        {
            AdminActionError = "Failed to demote user";
        }
    }

    private async Task SavePasswordAsync()
    {
        PasswordError   = string.Empty;
        PasswordSuccess = false;

        if (_newPassword.Length < 6)
        {
            PasswordError = "Password must be at least 6 characters";
            return;
        }

        if (_newPassword != _confirmPassword)
        {
            PasswordError = "Passwords do not match";
            return;
        }

        var ok = await _accountService.UpdatePasswordAsync(
            _currentUser.CurrentUser!.Id, _currentPassword, _newPassword);

        if (ok)
        {
            PasswordSuccess  = true;
            CurrentPassword  = string.Empty;
            NewPassword      = string.Empty;
            ConfirmPassword  = string.Empty;
        }
        else
        {
            PasswordError = "Current password is incorrect";
        }
    }
}
