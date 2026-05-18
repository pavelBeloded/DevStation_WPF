using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace DevStation.Views.Windows;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        loginPasswordBox.PasswordChanged += LoginPasswordBox_PasswordChanged;
        registerPasswordBox.PasswordChanged += RegisterPasswordBox_PasswordChanged;
        confirmPasswordBox.PasswordChanged += ConfirmPasswordBox_PasswordChanged;

        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is ViewModels.LoginViewModel old)
                old.PropertyChanged -= OnViewModelPropertyChanged;
            if (e.NewValue is ViewModels.LoginViewModel vm)
                vm.PropertyChanged += OnViewModelPropertyChanged;
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.LoginViewModel.IsLoginMode))
        {
            loginPasswordBox.Clear();
            registerPasswordBox.Clear();
            confirmPasswordBox.Clear();
        }
    }

    private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LoginViewModel vm)
            vm.Password = loginPasswordBox.Password;
    }

    private void RegisterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LoginViewModel vm)
            vm.RegisterPassword = registerPasswordBox.Password;
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LoginViewModel vm)
            vm.ConfirmPassword = confirmPasswordBox.Password;
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();
}
