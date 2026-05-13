using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using System.IO;
using System.Text.RegularExpressions;

namespace DevStation.Services.Implementations;

public static class ImageOptimizerService
{
    public static async Task<byte[]> OptimizeAsync(string filePath, IProgress<int>? progress = null)
    {
        progress?.Report(5);
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".svg")
            return await OptimizeSvgAsync(filePath, progress);
        if (ext == ".jpg" || ext == ".jpeg")
            return await OptimizeJpegAsync(filePath, progress);
        if (ext == ".webp")
            return await OptimizeWebpAsync(filePath, progress);

        return await OptimizePngAsync(filePath, progress);
    }

    private static async Task<byte[]> OptimizePngAsync(string path, IProgress<int>? progress)
    {
        return await Task.Run(() =>
        {
            progress?.Report(25);
            using var image = Image.Load(path);
            progress?.Report(55);
            using var ms = new MemoryStream();
            // Только lossless-сжатие без метаданных — никакой квантизации
            image.Save(ms, new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                FilterMethod     = PngFilterMethod.Adaptive,
                SkipMetadata     = true
            });
            progress?.Report(90);
            return ms.ToArray();
        });
    }

    private static async Task<byte[]> OptimizeJpegAsync(string path, IProgress<int>? progress)
    {
        return await Task.Run(() =>
        {
            progress?.Report(30);
            using var image = Image.Load(path);
            progress?.Report(65);
            using var ms = new MemoryStream();
            image.Save(ms, new JpegEncoder
            {
                Quality      = 82,   // безопасное значение без видимых артефактов
                SkipMetadata = true  // EXIF смартфона весит 50-150 KB
            });
            progress?.Report(90);
            return ms.ToArray();
        });
    }

    private static async Task<byte[]> OptimizeWebpAsync(string path, IProgress<int>? progress)
    {
        return await Task.Run(() =>
        {
            progress?.Report(30);
            using var image = Image.Load(path);
            progress?.Report(65);
            using var ms = new MemoryStream();
            image.Save(ms, new WebpEncoder
            {
                Quality      = 85,
                FileFormat   = WebpFileFormatType.Lossy,
                SkipMetadata = true
            });
            progress?.Report(90);
            return ms.ToArray();
        });
    }

    private static Task<byte[]> OptimizeSvgAsync(string path, IProgress<int>? progress)
    {
        return Task.Run(() =>
        {
            progress?.Report(20);
            var svg = File.ReadAllText(path);

            svg = Regex.Replace(svg, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);
            svg = Regex.Replace(svg, @">\s+<", "><");
            svg = Regex.Replace(svg, @"\s{2,}", " ");
            svg = svg.Trim();

            progress?.Report(90);
            return System.Text.Encoding.UTF8.GetBytes(svg);
        });
    }
}
