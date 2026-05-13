using DevStation.Services.Implementations;
using DevStation.Utils;
using DevStation.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace DevStation.ViewModels;

public enum OptimizationStatus { Queued, Optimizing, Completed }

public class ImageFileItem : ViewModelBase
{
    private OptimizationStatus _status = OptimizationStatus.Queued;
    private int _progress;
    private long _optimizedSize;

    public string FileName    { get; init; } = string.Empty;
    public string FilePath    { get; init; } = string.Empty;
    public string MimeType    { get; init; } = string.Empty;
    public long   OriginalSize { get; init; }
    public BitmapImage? Thumbnail { get; init; }

    public long OptimizedSize
    {
        get => _optimizedSize;
        set
        {
            if (SetProperty(ref _optimizedSize, value))
            {
                OnPropertyChanged(nameof(OptimizedSizeText));
                OnPropertyChanged(nameof(SavingsText));
            }
        }
    }

    public OptimizationStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(IsOptimizing));
                OnPropertyChanged(nameof(IsQueued));
            }
        }
    }

    public int Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public byte[]? OptimizedBytes { get; set; }

    public bool IsCompleted  => Status == OptimizationStatus.Completed;
    public bool IsOptimizing => Status == OptimizationStatus.Optimizing;
    public bool IsQueued     => Status == OptimizationStatus.Queued;
    public bool HasThumbnail => Thumbnail != null;

    public string OriginalSizeText  => FormatSize(OriginalSize);
    public string OptimizedSizeText => FormatSize(OptimizedSize);

    public string SavingsText
    {
        get
        {
            if (OriginalSize == 0 || OptimizedSize == 0) return string.Empty;
            var pct = (int)Math.Round((1.0 - (double)OptimizedSize / OriginalSize) * 100);
            return $"(-{pct}%)";
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0)          return "—";
        if (bytes < 1024)        return $"{bytes} B";
        if (bytes < 1_048_576)   return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / 1_048_576.0:F1} MB";
    }
}

public class ImageOptimizerViewModel : ViewModelBase
{
    private static readonly string[] _supportedExt =
        [".png", ".jpg", ".jpeg", ".webp", ".svg"];

    public ObservableCollection<ImageFileItem> Files { get; } = [];

    public RelayCommand<ImageFileItem> RemoveFileCommand   { get; }
    public RelayCommand<ImageFileItem> DownloadFileCommand { get; }
    public RelayCommand DeleteAllCommand   { get; }
    public RelayCommand DownloadAllCommand { get; }

    public ImageOptimizerViewModel()
    {
        RemoveFileCommand = new RelayCommand<ImageFileItem>(
            item => { if (item is not null) Files.Remove(item); });

        DownloadFileCommand = new RelayCommand<ImageFileItem>(item =>
        {
            if (item is null || !item.IsCompleted || item.OptimizedBytes is null) return;
            var ext = System.IO.Path.GetExtension(item.FileName);
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName   = item.FileName,
                DefaultExt = ext,
                Filter     = $"Image (*{ext})|*{ext}|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                System.IO.File.WriteAllBytes(dlg.FileName, item.OptimizedBytes);
                ToastService.Show("File saved!");
            }
        });

        DeleteAllCommand = new RelayCommand(() =>
        {
            Files.Clear();
            ToastService.Show("Queue cleared.");
        });

        // OpenFolderDialog доступен в .NET 8 WPF без Windows.Forms
        DownloadAllCommand = new RelayCommand(() =>
        {
            var completed = Files.Where(f => f.IsCompleted && f.OptimizedBytes is not null).ToList();
            if (completed.Count == 0) return;

            var dlg = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select folder to save optimized files"
            };
            if (dlg.ShowDialog() != true) return;

            var saved = 0;
            foreach (var item in completed)
            {
                var dest = System.IO.Path.Combine(dlg.FolderName, item.FileName);
                try { System.IO.File.WriteAllBytes(dest, item.OptimizedBytes!); saved++; }
                catch { /* skip locked or missing files */ }
            }
            if (saved > 0)
                ToastService.Show($"{saved} {(saved == 1 ? "file" : "files")} saved!");
        }, () => Files.Any(f => f.IsCompleted));
    }

    // Вызывается из code-behind при Drop и Browse
    public async Task AddFilesAsync(IEnumerable<string> paths)
    {
        var added = new List<ImageFileItem>();

        foreach (var path in paths)
        {
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (!_supportedExt.Contains(ext)) continue;
            if (Files.Any(f => f.FilePath == path)) continue;

            var info = new System.IO.FileInfo(path);
            if (!info.Exists || info.Length > 25 * 1024 * 1024) continue;

            var item = new ImageFileItem
            {
                FileName    = info.Name,
                FilePath    = path,
                MimeType    = ExtToMime(ext),
                OriginalSize = info.Length,
                Thumbnail   = LoadThumbnail(path)
            };

            Files.Add(item);
            added.Add(item);
        }

        // Параллельная обработка: не более (CPU-1) одновременных задач
        var maxDegree = Math.Max(2, Environment.ProcessorCount - 1);
        using var semaphore = new SemaphoreSlim(maxDegree);

        var tasks = added.Select(async item =>
        {
            await semaphore.WaitAsync();
            try   { await OptimizeAsync(item); }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);

        if (added.Count > 0)
            ToastService.Show($"{added.Count} {(added.Count == 1 ? "file" : "files")} optimized!");
    }

    private static async Task OptimizeAsync(ImageFileItem item)
    {
        item.Status   = OptimizationStatus.Optimizing;
        item.Progress = 0;

        var uiProgress = new Progress<int>(p => item.Progress = p);

        try
        {
            var bytes = await ImageOptimizerService.OptimizeAsync(item.FilePath, uiProgress);
            item.OptimizedBytes = bytes;
            item.OptimizedSize  = bytes.Length;
        }
        catch
        {
            // Если оптимизация не удалась — сохраняем исходник без изменений
            item.OptimizedBytes = await System.IO.File.ReadAllBytesAsync(item.FilePath);
            item.OptimizedSize  = item.OriginalSize;
        }

        item.Progress = 100;
        item.Status   = OptimizationStatus.Completed;
    }

    private static BitmapImage? LoadThumbnail(string path)
    {
        try
        {
            if (System.IO.Path.GetExtension(path).ToLowerInvariant() == ".svg")
                return null;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource       = new Uri(path, UriKind.Absolute);
            bmp.DecodePixelWidth = 80;
            bmp.CacheOption     = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    private static string ExtToMime(string ext) => ext switch
    {
        ".png"             => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp"            => "image/webp",
        ".svg"             => "image/svg+xml",
        _                  => "image/*"
    };
}
