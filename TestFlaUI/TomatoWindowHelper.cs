using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RpaServer.Worker.Utils
{
    public static class TomatoWindowHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;
        private const int WindowStableCheckMs = 200;

        // ─────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────

        /// <summary>"D:\...\Mock_Tomato.exe" → "Mock_Tomato"</summary>
        public static string GetProcessName(string appPath)
            => Path.GetFileNameWithoutExtension(appPath);

        // ─────────────────────────────────────────
        // FIND WINDOW — chỉ 1 overload, nhận processName
        // ─────────────────────────────────────────

        /// <summary>
        /// Tìm MainWindow theo processName.
        /// Ưu tiên khớp PID → fallback khớp tên window.
        /// </summary>
        public static Window? FindMainWindow(UIA3Automation automation, string processName, long timeoutMs = 10000)
        {
            var sw = Stopwatch.StartNew();
            var attemptCount = 0;

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                attemptCount++;
                try
                {
                    var desktop = automation.GetDesktop();
                    var windows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));

                    Console.WriteLine($"🔍 Attempt {attemptCount}: {windows.Length} windows trên desktop");

                    // Ưu tiên: khớp theo PID
                    var processes = Process.GetProcessesByName(processName);
                    if (processes.Length == 0)
                    {
                        Console.WriteLine($"⚠️ Process '{processName}' chưa chạy!");
                        break; // Không retry nếu process không tồn tại
                    }

                    AutomationElement? matched = null;
                    foreach (var proc in processes)
                    {
                        matched = windows.FirstOrDefault(w =>
                        {
                            try { return w.Properties.ProcessId.ValueOrDefault == proc.Id; }
                            catch { return false; }
                        });

                        if (matched != null)
                        {
                            Console.WriteLine($"✅ Khớp PID {proc.Id}: '{matched.Name}'");
                            break;
                        }
                    }

                    // Fallback: khớp tên window
                    if (matched == null)
                    {
                        matched = windows.FirstOrDefault(w =>
                            !string.IsNullOrEmpty(w.Name) &&
                            w.Name.Contains(processName, StringComparison.OrdinalIgnoreCase));

                        if (matched != null)
                            Console.WriteLine($"✅ Khớp tên window: '{matched.Name}'");
                    }

                    if (matched != null)
                        return matched.AsWindow();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Attempt {attemptCount}: {ex.Message}");
                }

                Task.Delay(300).Wait();
            }

            Console.WriteLine($"❌ Không tìm thấy window cho '{processName}'");
            return null;
        }

        // ─────────────────────────────────────────
        // ATTACH — chỉ 1 overload, nhận processName
        // ─────────────────────────────────────────

        /// <summary>Tìm window + kiểm tra stale.</summary>
        public static Window? Attach(UIA3Automation automation, string processName)
        {
            var window = FindMainWindow(automation, processName);
            if (window == null) return null;

            try
            {
                _ = window.Title; // Stale check
                return window;
            }
            catch
            {
                Console.WriteLine("⚠️ Window bị stale");
                return null;
            }
        }

        // ─────────────────────────────────────────
        // BRING TO FRONT
        // ─────────────────────────────────────────

        public static void BringToFront(Window window)
        {
            try
            {
                var handle = window.FrameworkAutomationElement.Properties.NativeWindowHandle.Value;
                if (handle == 0) return;

                ShowWindow((IntPtr)handle, SW_RESTORE);
                SetForegroundWindow((IntPtr)handle);
                Console.WriteLine("✅ Window đã được đưa lên foreground");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ BringToFront: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────
        // RESPONSIVE CHECK
        // ─────────────────────────────────────────

        public static bool IsWindowResponsive(UIA3Automation automation, Window window)
        {
            try
            {
                var cf = new ConditionFactory(new UIA3PropertyLibrary());
                var field = window.FindFirstDescendant(cf.ByControlType(ControlType.Edit));
                if (field == null) return false;

                _ = field.Name;
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────
        // WAIT FOR STABLE — entry point chính
        // ─────────────────────────────────────────

        /// <summary>
        /// Chờ window ổn định: tồn tại + không stale + responsive.
        /// Truyền appPath, hàm tự lấy processName.
        /// </summary>
        public static async Task<Window?> WaitForWindowStable(
            UIA3Automation automation,
            string appPath,
            int timeoutMs = 10000)
        {
            var processName = GetProcessName(appPath); // "Mock_Tomato"
            var sw = Stopwatch.StartNew();

            Console.WriteLine($"⏳ Chờ '{processName}' ổn định...");

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                var w = Attach(automation, processName);
                if (w != null)
                {
                    BringToFront(w);
                    if (IsWindowResponsive(automation, w))
                    {
                        Console.WriteLine($"✅ '{processName}' sẵn sàng sau {sw.ElapsedMilliseconds}ms");
                        return w;
                    }
                    Console.WriteLine("⚠️ Window tìm thấy nhưng chưa responsive...");
                }

                await Task.Delay(WindowStableCheckMs);
            }

            Console.WriteLine($"❌ Timeout: '{processName}' không ổn định sau {timeoutMs}ms");
            return null;
        }

        // ─────────────────────────────────────────
        // UTILS
        // ─────────────────────────────────────────

        /// <summary>Kiểm tra nhanh (không chờ).</summary>
        public static bool IsAppReady(UIA3Automation automation, string appPath)
        {
            var w = Attach(automation, GetProcessName(appPath));
            return w != null && IsWindowResponsive(automation, w);
        }

        /// <summary>Wrapper async cho pipeline, hỗ trợ CancellationToken.</summary>
        public static async Task<Window?> EnsureAppReadyAsync(
            UIA3Automation automation,
            string appPath,
            CancellationToken ct = default,
            int timeoutMs = 10000)
        {
            var processName = GetProcessName(appPath);
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs && !ct.IsCancellationRequested)
            {
                var w = Attach(automation, processName);
                if (w != null && IsWindowResponsive(automation, w))
                {
                    BringToFront(w);
                    return w;
                }
                await Task.Delay(WindowStableCheckMs, ct);
            }

            return null;
        }
    }
}