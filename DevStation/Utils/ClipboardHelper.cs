using System.Windows;

namespace DevStation.Utils;

public static class ClipboardHelper
{
    /// <summary>Copies text to clipboard safely.</summary>
    public static bool TryCopy(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
