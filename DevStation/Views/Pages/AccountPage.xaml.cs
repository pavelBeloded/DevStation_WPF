using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DevStation.ViewModels;

namespace DevStation.Views.Pages;

public partial class AccountPage : UserControl
{
    private AccountViewModel? _vm;

    public AccountPage()
    {
        InitializeComponent();
        Loaded   += AccountPage_Loaded;
        Unloaded += AccountPage_Unloaded;
    }

    private void AccountPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountViewModel vm)
        {
            _vm = vm;
            _vm.PropertyChanged += Vm_PropertyChanged;
        }
    }

    private void AccountPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.PropertyChanged -= Vm_PropertyChanged;
            _vm = null;
        }
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AccountViewModel.PasswordSuccess) && _vm?.PasswordSuccess == true)
        {
            currentPasswordBox.Password = string.Empty;
            newPasswordBox.Password     = string.Empty;
            confirmPasswordBox.Password = string.Empty;
        }
    }

    private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountViewModel vm)
            vm.CurrentPassword = ((PasswordBox)sender).Password;
    }

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountViewModel vm)
            vm.NewPassword = ((PasswordBox)sender).Password;
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountViewModel vm)
            vm.ConfirmPassword = ((PasswordBox)sender).Password;
    }
}
