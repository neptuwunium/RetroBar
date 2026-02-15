using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static ManagedShell.Interop.NativeMethods;

namespace RetroBar.Utilities
{
    // TODO: This should move to ManagedShell
    public class LowLevelMouseHook : IDisposable
    {
        [DllImport("user32.dll")]
        public static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProcDelegate callback, nint hInstance, uint threadId);

        [DllImport("user32.dll")]
        public static extern nint CallNextHookEx(nint idHook, int nCode, uint wParam, nint lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern nint GetModuleHandle(string lpModuleName);

        public delegate nint LowLevelMouseProcDelegate(int code, uint wParam, nint lParam);

        const int WH_MOUSE_LL = 14;

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public nint dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public class LowLevelMouseEventArgs : HandledEventArgs
        {
            public WM Message;
            public MSLLHOOKSTRUCT HookStruct;
        }

        public event EventHandler<LowLevelMouseEventArgs> LowLevelMouseEvent;

        private nint _hook = nint.Zero;
        private LowLevelMouseProcDelegate _hookDelegate;

        public LowLevelMouseHook() {
            _hookDelegate = MouseHookProc;
        }

        public bool Initialize()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hook = SetWindowsHookEx(WH_MOUSE_LL, _hookDelegate, GetModuleHandle(curModule.ModuleName), 0);

                if (_hook == nint.Zero)
                {
                    return false;
                }

                return true;
            }
        }

        private nint MouseHookProc(int code, uint wParam, nint lParam)
        {
            LowLevelMouseEventArgs args = new LowLevelMouseEventArgs
            {
                Message = (WM)wParam,
                HookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT))
            };

            LowLevelMouseEvent?.Invoke(this, args);

            if (args.Handled)
            {
                return (nint)1;
            }

            return CallNextHookEx(_hook, code, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hook == nint.Zero)
            {
                return;
            }

            UnhookWindowsHookEx(_hook);
            _hook = nint.Zero;
        }
    }
}
