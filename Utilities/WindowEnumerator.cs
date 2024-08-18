using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TabOut.Utilities
{
    /// <summary>
    /// 窗口枚举器
    /// </summary>
    class WindowEnumerator
    {
        /// <summary>
        /// 枚举窗口
        /// </summary>
        /// <param name="processId">进程Id</param>
        public static IEnumerable<WindowInfo> EnumerateWindows(int processId)
        {
            Record record = new Record(new List<WindowInfo>(), processId);
            var gcHandle = GCHandle.Alloc(record);
            try
            {
                EnumWindows(onWindowEnum, GCHandle.ToIntPtr(gcHandle));
                return record.Windows;
            }
            finally
            {
                gcHandle.Free();
            }
        }

        static bool onWindowEnum(IntPtr hWnd, nint lparam)
        {
            // 仅查找顶层窗口
            if (GetParent(hWnd) != IntPtr.Zero)
            {
                // 获取进程ID
                GetWindowThreadProcessId(hWnd, out var lpdwProcessID);

                var handle = GCHandle.FromIntPtr(lparam);
                var record = handle.Target as Record;
                if (lpdwProcessID == record.ProcessId)
                {
                    // 获取窗口类名。
                    var lpString = new StringBuilder(512);
                    GetClassName(hWnd, lpString, lpString.Capacity);
                    var className = lpString.ToString();

                    // 添加到已找到的窗口列表。
                    record.Windows.Add(new WindowInfo(hWnd, className, (int)lpdwProcessID, GetParent(hWnd) == IntPtr.Zero));
                }
            }

            return true;
        }

        record Record(List<WindowInfo> Windows, int ProcessId);

        delegate bool WndEnumProc(IntPtr hWnd, nint lParam);

        [DllImport("user32")]
        static extern bool EnumWindows(WndEnumProc lpEnumFunc, nint lParam);

        [DllImport("user32")]
        static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32")]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}
