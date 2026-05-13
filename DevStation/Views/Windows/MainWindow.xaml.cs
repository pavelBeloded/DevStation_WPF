using DevStation.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DevStation.Views.Windows;

public partial class MainWindow : Window
{
    private const int WM_GETMINMAXINFO = 0x0024;
    private const int WM_SYSCOMMAND   = 0x0112;
    private const int WM_SYSKEYDOWN   = 0x0104;
    private const int SC_KEYMENU      = 0xF100;
    private const int VK_SPACE        = 0x20;

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int  cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var source = (HwndSource)PresentationSource.FromVisual(this);
        source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Ограничиваем максимальный размер рабочей областью ТЕКУЩЕГО монитора (физические пиксели)
        if (msg == WM_GETMINMAXINFO)
        {
            var mmi     = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                GetMonitorInfo(monitor, ref info);
                // ptMaxPosition — относительно левого верхнего угла монитора (не всей раскладки)
                int w = info.rcWork.Right  - info.rcWork.Left;
                int h = info.rcWork.Bottom - info.rcWork.Top;
                mmi.ptMaxPosition.x  = info.rcWork.Left - info.rcMonitor.Left;
                mmi.ptMaxPosition.y  = info.rcWork.Top  - info.rcMonitor.Top;
                mmi.ptMaxSize.x      = w;
                mmi.ptMaxSize.y      = h;
                // Minimum resize size — cannot exceed the work area size
                mmi.ptMinTrackSize.x = Math.Min(800, w);
                mmi.ptMinTrackSize.y = Math.Min(500, h);
            }
            Marshal.StructureToPtr(mmi, lParam, true);
            handled = true;
            return IntPtr.Zero;
        }

        // Подавляем системное меню, вызываемое Alt+Space
        if (msg == WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == SC_KEYMENU)
        {
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WM_SYSKEYDOWN && wParam.ToInt32() == VK_SPACE)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (MaxRestoreIcon == null) return;
        MaxRestoreIcon.Text = WindowState == WindowState.Maximized
            ? ""   // Restore (восстановить)
            : "";  // Maximize (развернуть)
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        switch (e.Key)
        {
            case Key.Enter:
                if (vm.HasQuestionQuery)
                    vm.OpenSelectedMdnResult();
                else
                    vm.UseFirstResultCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                vm.SearchQuery = string.Empty;
                e.Handled = true;
                break;
            case Key.Down:
                if (vm.HasQuestionQuery)
                    vm.SelectNextMdnResult();
                else
                    vm.SelectNextResult();
                e.Handled = true;
                break;
            case Key.Up:
                if (vm.HasQuestionQuery)
                    vm.SelectPreviousMdnResult();
                else
                    vm.SelectPreviousResult();
                e.Handled = true;
                break;
        }
    }
}
