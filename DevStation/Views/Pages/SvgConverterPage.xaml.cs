using DevStation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevStation.Views.Pages;

public partial class SvgConverterPage : UserControl
{
    public SvgConverterPage()
    {
        InitializeComponent();
    }

    private ConverterViewModel? VM => DataContext as ConverterViewModel;

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select SVG or XML file",
            Filter = "SVG/XML files (*.svg;*.xml)|*.svg;*.xml|All files (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var content = System.IO.File.ReadAllText(dialog.FileName, System.Text.Encoding.UTF8);
            if (VM is not null)
                VM.SvgCode = content;
        }
        catch
        {
            // файл недоступен — игнорируем
        }
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var file = files.FirstOrDefault(f =>
            f.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

        if (file is null) return;

        try
        {
            var content = System.IO.File.ReadAllText(file, System.Text.Encoding.UTF8);
            if (VM is not null)
                VM.SvgCode = content;
        }
        catch
        {
            // Файл недоступен — просто игнорируем
        }
    }
}
