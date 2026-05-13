using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace DevStation.Utils;

public static class ToastService
{
    public static void Show(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var target = Application.Current.Windows
                             .OfType<Window>()
                             .FirstOrDefault(w => w.IsActive && w.IsVisible)
                         ?? Application.Current.Windows
                             .OfType<Window>()
                             .FirstOrDefault(w => w.IsVisible);

            if (target == null) return;

            var border = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0xFF, 0xB8, 0x6C)),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(6),
                Padding         = new Thickness(16, 10, 16, 10),
                Child = new TextBlock
                {
                    Text       = message,
                    Foreground = Brushes.White,
                    FontSize   = 13,
                    FontFamily = new FontFamily("Segoe UI")
                }
            };

            var popup = new Popup
            {
                Child             = border,
                PlacementTarget   = target,
                Placement         = PlacementMode.RelativePoint,
                HorizontalOffset  = target.ActualWidth - 270,
                VerticalOffset    = target.ActualHeight - 80,
                IsOpen            = true,
                AllowsTransparency = true,
                StaysOpen         = true
            };

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            timer.Tick += (_, _) => { popup.IsOpen = false; timer.Stop(); };
            timer.Start();
        });
    }
}
