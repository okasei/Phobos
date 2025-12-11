using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Phobos.Utils.Arcusrix
{
    public enum TaskbarPosition { Left, Top, Right, Bottom }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public int lParam;
    }

    public class PUTaskbar
    {
        private const int ABM_GETTASKBARPOS = 0x00000005;
        private const int ABE_LEFT = 0;
        private const int ABE_TOP = 1;
        private const int ABE_RIGHT = 2;
        private const int ABE_BOTTOM = 3;

        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const uint MDT_EFFECTIVE_DPI = 0;

        [DllImport("shell32.dll")]
        static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);

        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, uint dpiType,
            out uint dpiX, out uint dpiY);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X, Y;
            public POINT(int x, int y) { X = x; Y = y; }
        }

        private static readonly Lazy<PUTaskbar> _lazyInstance = new Lazy<PUTaskbar>(() => new PUTaskbar());
        private PUTaskbar() { }
        public static PUTaskbar Instance => _lazyInstance.Value;

        public TaskbarPosition GetPosition(APPBARDATA data)
        {
            return data.uEdge switch
            {
                ABE_LEFT => TaskbarPosition.Left,
                ABE_TOP => TaskbarPosition.Top,
                ABE_RIGHT => TaskbarPosition.Right,
                _ => TaskbarPosition.Bottom
            };
        }

        public APPBARDATA GetTaskbarData()
        {
            var data = new APPBARDATA();
            data.cbSize = Marshal.SizeOf(data);

            if (SHAppBarMessage(ABM_GETTASKBARPOS, ref data) == IntPtr.Zero)
                throw new InvalidOperationException("Failed to get taskbar info.");

            return data;
        }
        private double GetDpiScaleForRect(RECT rc)
        {
            // Using left-top corner of taskbar
            var pt = new POINT(rc.Left, rc.Top);
            var monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

            GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _);

            return dpiX / 96.0;
        }

        public double GetTaskbarSizeDip()
        {
            var data = GetTaskbarData();
            double dpiScale = GetDpiScaleForRect(data.rc);
            var position = GetPosition(data);

            if (position == TaskbarPosition.Top || position == TaskbarPosition.Bottom)
            {
                int physicalHeight = data.rc.Bottom - data.rc.Top;
                return physicalHeight / dpiScale;
            }
            else
            {
                int physicalWidth = data.rc.Right - data.rc.Left;
                return physicalWidth / dpiScale;
            }
        }
    }
}
