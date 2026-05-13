using DevStation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevStation.Views.Pages;

public partial class ImageOptimizerPage : UserControl
{
    public ImageOptimizerPage()
    {
        InitializeComponent();
    }

    private ImageOptimizerViewModel? VM => DataContext as ImageOptimizerViewModel;

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

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) || VM is null) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        await VM.AddFilesAsync(files);
    }

    private async void DropZone_Click(object sender, MouseButtonEventArgs e)
    {
        if (VM is null) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title       = "Select images to optimize",
            Filter      = "Images (*.png;*.jpg;*.jpeg;*.webp;*.svg)|*.png;*.jpg;*.jpeg;*.webp;*.svg|All files (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true) return;
        await VM.AddFilesAsync(dialog.FileNames);
    }
}
