using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Kith.Sources
{
    public class WindowsSubclass
    {
        private const int WM_GETMINMAXINFO = 0x0024;
        private readonly Windows.Win32.UI.Shell.SUBCLASSPROC _subclassProc;
        private readonly Windows.Win32.Foundation.HWND _hwnd;
        private readonly uint _id;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowMinSizeSubclass"/> class.
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        /// <param name="id">The subclass ID, could be any number.</param>
        /// <param name="minSize">The minimum size of the window.</param>
        /// <exception cref="InvalidOperationException">Thrown when setting the window subclass fails.</exception>
        public WindowsSubclass(IntPtr hwnd, uint id, Size minSize)
        {
            _hwnd = (Windows.Win32.Foundation.HWND)hwnd;
            _id = id;
            MinSize = minSize;
            _subclassProc = new Windows.Win32.UI.Shell.SUBCLASSPROC(WindowSubclassProc);

            var result = Windows.Win32.PInvoke.SetWindowSubclass(_hwnd, _subclassProc, _id, 0);
            if (result.Value == 0)
            {
                throw new InvalidOperationException("Failed to set window subclass");
            }
        }

        public Size MinSize { get; }

        private Windows.Win32.Foundation.LRESULT WindowSubclassProc(Windows.Win32.Foundation.HWND hWnd, uint uMsg,
            Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam,
            nuint uIdSubclass, nuint dwRefData)
        {
            switch (uMsg)
            {
                case WM_GETMINMAXINFO:
                    // Windows sends this message to query size constraints. 
                    // ptMinTrackSize defines the smallest draggable window size.
                    // We adjust it based on DPI scaling.
                    var dpi = Windows.Win32.PInvoke.GetDpiForWindow(hWnd);
                    float scalingFactor = (float)dpi / 96;
                    var minMaxInfo = Marshal.PtrToStructure<Windows.Win32.UI.WindowsAndMessaging.MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.X = (int)(MinSize.Width * scalingFactor);
                    minMaxInfo.ptMinTrackSize.Y = (int)(MinSize.Height * scalingFactor);
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    return new Windows.Win32.Foundation.LRESULT(0);
            }
            return Windows.Win32.PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }
    }
}
