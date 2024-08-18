using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabOut.Utilities
{
    /// <summary>
    /// 获取 Win32 窗口的一些基本信息。
    /// </summary>
    readonly struct WindowInfo
    {
        public WindowInfo(IntPtr hWnd, string className, int processId, bool isTopLevel)
        {
            Hwnd = hWnd;
            ClassName = className;
            ProcessId = processId;
            IsTopLevel = isTopLevel;
        }

        /// <summary>
        /// 获取窗口句柄。
        /// </summary>
        public IntPtr Hwnd { get; }

        /// <summary>
        /// 获取窗口类名。
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        /// 进程Id
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// 是否顶级窗口
        /// </summary>
        public bool IsTopLevel { get; }
    }
}
