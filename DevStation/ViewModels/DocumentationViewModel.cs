using DevStation.ViewModels.Base;
using System.Windows.Input;

namespace DevStation.ViewModels;

public class DocumentationViewModel : ViewModelBase
{
    private string _activeSection = "getting-started";
    public string ActiveSection
    {
        get => _activeSection;
        set => SetProperty(ref _activeSection, value);
    }

    public ICommand ScrollToCommand { get; }
    public Action<string>? ScrollToSection { get; set; }

    public DocumentationViewModel()
    {
        ScrollToCommand = new RelayCommand<string>(section =>
        {
            ActiveSection  = section ?? "getting-started";
            ScrollToSection?.Invoke(ActiveSection);
        });
    }
}
