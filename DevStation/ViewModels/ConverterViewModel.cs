using DevStation.Utils;
using DevStation.ViewModels.Base;
using System.Text;
using System.Windows;
using System.Xml.Linq;

namespace DevStation.ViewModels;

public class ConverterViewModel : ViewModelBase
{
    private string _svgCode = string.Empty;
    private string _reactCode = string.Empty;
    private bool _isEmpty = true;
    private string _statusText = "READY";

    public string SvgCode
    {
        get => _svgCode;
        set
        {
            if (SetProperty(ref _svgCode, value))
            {
                IsEmpty = string.IsNullOrWhiteSpace(value);
                if (!IsEmpty)
                    RunConversion();
                else
                {
                    ReactCode = string.Empty;
                    StatusText = "READY";
                }
            }
        }
    }

    public string ReactCode
    {
        get => _reactCode;
        private set => SetProperty(ref _reactCode, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public RelayCommand CopyCommand { get; }
    public RelayCommand DownloadCommand { get; }
    public RelayCommand ClearCommand { get; }

    public ConverterViewModel()
    {
        CopyCommand = new RelayCommand(ExecuteCopy, () => !string.IsNullOrEmpty(ReactCode));
        DownloadCommand = new RelayCommand(ExecuteDownload, () => !string.IsNullOrEmpty(ReactCode));
        ClearCommand = new RelayCommand(ExecuteClear, () => !IsEmpty);
    }

    private void RunConversion()
    {
        StatusText = "PROCESSING";
        try
        {
            ReactCode = SvgToJsx(SvgCode);
            StatusText = "READY";
        }
        catch
        {
            ReactCode = "// Invalid SVG — could not parse input";
            StatusText = "ERROR";
        }
    }

    private static string SvgToJsx(string svgCode)
    {
        var doc = XDocument.Parse(svgCode.Trim());
        var root = doc.Root!;

        var sb = new StringBuilder();
        sb.AppendLine("// Generated via DevStation SVG Converter");
        sb.AppendLine("import React from \"react\";");
        sb.AppendLine();
        sb.AppendLine("const ConvertedComponent = ({ size = 24, color = \"currentColor\" }) => (");
        AppendElement(sb, root, 1, isRoot: true);
        sb.AppendLine(");");
        sb.AppendLine();
        sb.Append("export default ConvertedComponent;");
        return sb.ToString();
    }

    private static void AppendElement(StringBuilder sb, XElement el, int depth, bool isRoot = false)
    {
        var pad = new string(' ', depth * 2);
        sb.Append($"{pad}<{el.Name.LocalName}");

        foreach (var attr in el.Attributes())
        {
            if (attr.IsNamespaceDeclaration) continue;
            var name = ToJsxName(attr.Name.LocalName);
            var value = ToJsxValue(name, attr.Value, isRoot);
            sb.Append($"\n{pad}  {name}={value}");
        }

        var children = el.Elements().ToList();
        if (children.Count == 0)
        {
            sb.AppendLine($"\n{pad}/>");
        }
        else
        {
            sb.AppendLine($"\n{pad}>");
            foreach (var child in children)
                AppendElement(sb, child, depth + 1);
            sb.AppendLine($"{pad}</{el.Name.LocalName}>");
        }
    }

    private static string ToJsxName(string name) => name switch
    {
        "class"             => "className",
        "for"               => "htmlFor",
        "fill-rule"         => "fillRule",
        "clip-rule"         => "clipRule",
        "clip-path"         => "clipPath",
        "stroke-width"      => "strokeWidth",
        "stroke-linecap"    => "strokeLinecap",
        "stroke-linejoin"   => "strokeLinejoin",
        "stroke-dasharray"  => "strokeDasharray",
        "stroke-dashoffset" => "strokeDashoffset",
        "stroke-miterlimit" => "strokeMiterlimit",
        "font-size"         => "fontSize",
        "font-family"       => "fontFamily",
        "font-weight"       => "fontWeight",
        "text-anchor"       => "textAnchor",
        "dominant-baseline" => "dominantBaseline",
        "stop-color"        => "stopColor",
        "stop-opacity"      => "stopOpacity",
        "marker-end"        => "markerEnd",
        "marker-start"      => "markerStart",
        _                   => name
    };

    private static string ToJsxValue(string jsxName, string value, bool isRoot)
    {
        if (isRoot && jsxName == "width")  return "{size}";
        if (isRoot && jsxName == "height") return "{size}";

        var isColorValue = value is "currentColor" or "black" or "#000" or "#000000";
        if (jsxName is "fill" or "stroke" && isColorValue) return "{color}";

        if (jsxName == "style") return ToJsxStyle(value);

        return $"\"{value}\"";
    }

    private static string ToJsxStyle(string css)
    {
        var props = new Dictionary<string, string>();

        foreach (var part in css.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = part.IndexOf(':');
            if (idx < 0) continue;
            var prop = KebabToCamelCase(part[..idx].Trim()); 
            var val  = part[(idx + 1)..].Trim();
            props[prop] = val;
        }

        var entries = props.Select(kv => $"{kv.Key}: '{kv.Value}'");
        return $"{{{{{string.Join(", ", entries)}}}}}";
    }

    private static string KebabToCamelCase(string kebab)
    {
        var parts = kebab.Split('-');
        if (parts.Length == 1) return kebab;
        return parts[0] + string.Concat(
            parts.Skip(1).Select(p => p.Length > 0 ? char.ToUpper(p[0]) + p[1..] : p));
    }

    private void ExecuteCopy()
    {
        Clipboard.SetText(ReactCode);
        ToastService.Show("Copied to clipboard!");
    }

    private void ExecuteDownload()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "ConvertedComponent.jsx",
            DefaultExt = ".jsx",
            Filter = "JSX files (*.jsx)|*.jsx|JavaScript files (*.js)|*.js|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            System.IO.File.WriteAllText(dialog.FileName, ReactCode, System.Text.Encoding.UTF8);
            ToastService.Show("File saved!");
        }
    }

    private void ExecuteClear()
    {
        SvgCode = string.Empty;
        ReactCode = string.Empty;
        StatusText = "READY";
        IsEmpty = true;
        ToastService.Show("Cleared.");
    }
}
