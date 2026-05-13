using DevStation.Services.Interfaces;
using DevStation.Utils;
using DevStation.ViewModels.Base;
using DevStation.Views.Windows;
using System.Windows;

namespace DevStation.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly Func<MainWindow> _mainWindowFactory;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _newUsername = string.Empty;
    private string _email = string.Empty;
    private string _registerPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _isLoginMode = true;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    /// <summary>Set from code-behind (login PasswordBox).</summary>
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string NewUsername
    {
        get => _newUsername;
        set => SetProperty(ref _newUsername, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    /// <summary>Set from code-behind (register PasswordBox).</summary>
    public string RegisterPassword
    {
        get => _registerPassword;
        set => SetProperty(ref _registerPassword, value);
    }

    /// <summary>Set from code-behind (confirm PasswordBox).</summary>
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsLoginMode
    {
        get => _isLoginMode;
        set => SetProperty(ref _isLoginMode, value);
    }

    public RelayCommand LoginCommand { get; }
    public RelayCommand RegisterCommand { get; }
    public RelayCommand SwitchToLoginCommand { get; }
    public RelayCommand SwitchToRegisterCommand { get; }
    public RelayCommand ForgotPasswordCommand { get; }

    public LoginViewModel(IAuthService authService, Func<MainWindow> mainWindowFactory)
    {
        _authService = authService;
        _mainWindowFactory = mainWindowFactory;

        LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync(), CanLogin);
        RegisterCommand = new RelayCommand(async () => await ExecuteRegisterAsync(), CanRegister);

        SwitchToLoginCommand = new RelayCommand(() =>
        {
            IsLoginMode = true;
            ErrorMessage = string.Empty;
        });

        SwitchToRegisterCommand = new RelayCommand(() =>
        {
            IsLoginMode = false;
            ErrorMessage = string.Empty;
        });

        ForgotPasswordCommand = new RelayCommand(
            () => ErrorMessage = "Password recovery is not available. Contact an administrator.");
    }

    private bool CanLogin() =>
        !IsLoading &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);

    private bool CanRegister() =>
        !IsLoading &&
        !string.IsNullOrWhiteSpace(NewUsername) &&
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(RegisterPassword) &&
        !string.IsNullOrWhiteSpace(ConfirmPassword);

    private async Task ExecuteLoginAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var user = await _authService.LoginAsync(Username, Password);
            if (user == null)
            {
                ErrorMessage = "Invalid credentials.";
                return;
            }
            OpenMainWindow();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteRegisterAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            if (!ValidationHelper.IsValidUsername(NewUsername))
            {
                ErrorMessage = "Username must be 3–50 alphanumeric characters.";
                return;
            }
            if (!ValidationHelper.IsValidEmail(Email))
            {
                ErrorMessage = "Invalid email address.";
                return;
            }
            if (!ValidationHelper.IsValidPassword(RegisterPassword))
            {
                ErrorMessage = "Password must be at least 6 characters.";
                return;
            }
            if (RegisterPassword != ConfirmPassword)
            {
                ErrorMessage = "Passwords don't match.";
                return;
            }

            await _authService.RegisterAsync(NewUsername, Email, RegisterPassword);
            OpenMainWindow();
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Registration failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenMainWindow()
    {
        var mainWindow = _mainWindowFactory();
        mainWindow.Show();
        Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();
    }
}
