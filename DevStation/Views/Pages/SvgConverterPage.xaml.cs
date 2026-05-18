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

    private static bool IsSvgFile(string path) =>
        path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);

    private static bool HasSvgFile(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
        var files = e.Data.GetData(DataFormats.FileDrop) as string[];
        return files?.Any(IsSvgFile) == true;
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasSvgFile(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = HasSvgFile(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select SVG file",
            Filter = "SVG files (*.svg)|*.svg",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;
        if (!IsSvgFile(dialog.FileName)) return;

        try
        {
            var content = System.IO.File.ReadAllText(dialog.FileName, System.Text.Encoding.UTF8);
            if (VM is not null)
                VM.SvgCode = content;
        }
        catch
        {
        }
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var file = files.FirstOrDefault(f =>
            f.EndsWith(".svg", StringComparison.OrdinalIgnoreCase));

        if (file is null) return;

        try
        {
            var content = System.IO.File.ReadAllText(file, System.Text.Encoding.UTF8);
            if (VM is not null)
                VM.SvgCode = content;
        }
        catch
        {
        }
    }
}
