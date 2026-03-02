using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TestFlaUI;

public class Method
{
    private UIA3Automation _automation;
    private ConditionFactory _cf;
    private AutomationConfig _config;
    private string _appPath;

    // Import Windows API để restore window
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

    public Method(string configPath, string appPath)
    {
        _automation = new UIA3Automation();
        _cf = new ConditionFactory(new UIA3PropertyLibrary());
        _config = AutomationConfig.LoadFromJson(configPath);
        _appPath = appPath;
    }

    public void FillFormData(FormData data)
    {
        FlaUI.Core.Application? application = null;

        try
        {
            // Tìm app đang chạy theo tên process
            var processName = Path.GetFileNameWithoutExtension(_appPath); // "Mock_Tomato"
            var runningProcesses = System.Diagnostics.Process.GetProcessesByName(processName);

            if (runningProcesses.Length == 0)
            {
                throw new Exception($"Không tìm thấy {processName} đang chạy!\nVui lòng mở ứng dụng trước khi nhập dữ liệu.");
            }

            Console.WriteLine($"✅ Tìm thấy {processName} đang chạy (PID: {runningProcesses[0].Id})");

            // Attach vào app đang chạy
            application = FlaUI.Core.Application.Attach(runningProcesses[0].Id);
            Thread.Sleep(500); // Đợi attach

            var mainWindow = application.GetMainWindow(_automation);
            Console.WriteLine("✅ Đã kết nối với cửa sổ chính");

            // Restore window nếu bị minimize (cần thiết để automation hoạt động)
            // Nhưng KHÔNG bring to front - chỉ restore
            RestoreWindowIfMinimized(mainWindow);

            Console.WriteLine("💡 Đang nhập dữ liệu (không bring to front)...");

            // Bước 1: Điền các TextBox
            try
            {
                Console.WriteLine("\n📝 BƯỚC 1: Điền WOno...");
                FillTextBox(mainWindow, _config.WonoTextBoxId, data.WOno);
                Console.WriteLine("✅ Đã điền WOno thành công");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tại BƯỚC 1 (WOno): {ex.Message}", ex);
            }

            try
            {
                Console.WriteLine("\n📝 BƯỚC 2: Điền Custom Input...");
                FillTextBox(mainWindow, _config.CustomInputTextBoxId, data.CustomInput);
                Console.WriteLine("✅ Đã điền Custom Input thành công");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tại BƯỚC 2 (Custom Input): {ex.Message}", ex);
            }

            try
            {
                Console.WriteLine("\n📝 BƯỚC 3: Điền Số hoàn thành...");
                FillTextBox(mainWindow, _config.CompletedQuantityTextBoxId, data.CompletedQuantity);
                Console.WriteLine("✅ Đã điền Số hoàn thành thành công");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tại BƯỚC 3 (Số hoàn thành): {ex.Message}", ex);
            }

            // Bước 2: Chọn RadioButton cho Kiểu đóng gói
            try
            {
                Console.WriteLine("\n📝 BƯỚC 4: Chọn kiểu đóng gói...");
                SelectRadioButton(mainWindow, _config.PackageStylePanelId, data.PackageStyle);
                Console.WriteLine("✅ Đã chọn kiểu đóng gói thành công");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tại BƯỚC 4 (Kiểu đóng gói): {ex.Message}", ex);
            }

            // Bước 3: Điền dữ liệu vào DataGridView
            try
            {
                Console.WriteLine("\n📝 BƯỚC 5: Điền dữ liệu vào DataGridView...");
                FillDataGridView(mainWindow, _config.DataGridViewId, data.GridData);
                Console.WriteLine("✅ Đã điền DataGridView thành công");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tại BƯỚC 5 (DataGridView): {ex.Message}", ex);
            }

            // Bước 4: Nhấn nút Send (nếu có cấu hình)
            if (!string.IsNullOrEmpty(_config.SendButtonId))
            {
                try
                {
                    Console.WriteLine("\n📝 BƯỚC 6: Nhấn nút Send...");
                    ClickButton(mainWindow, _config.SendButtonId);
                    Console.WriteLine("✅ Đã nhấn nút Send thành công");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Lỗi tại BƯỚC 6 (Button Send): {ex.Message}", ex);
                }
            }

            Thread.Sleep(500);
            Console.WriteLine("\n🎉 ĐÃ HOÀN THÀNH TẤT CẢ CÁC BƯỚC!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ LỖI: {ex.Message}");
            throw;
        }
        finally
        {
            // Không dispose application vì ta chỉ attach, không launch
            if (application != null)
            {
                application.Dispose();
            }
        }
    }

    private void RestoreWindowIfMinimized(Window window)
    {
        try
        {
            var handle = window.Properties.NativeWindowHandle.ValueOrDefault;

            if (handle != IntPtr.Zero)
            {
                // Chỉ restore nếu bị minimize
                if (IsIconic(handle))
                {
                    Console.WriteLine("🔄 Window đang bị minimize, đang restore...");
                    ShowWindow(handle, SW_RESTORE);
                    Thread.Sleep(500);
                    Console.WriteLine("✅ Đã restore window (không bring to front)");
                }
                else
                {
                    Console.WriteLine("✅ Window đã sẵn sàng");
                }

                // KHÔNG gọi SetForegroundWindow - để window ở background
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Không thể kiểm tra window state: {ex.Message}");
        }
    }

    private void RestoreAndFocusWindow(Window window)
    {
        try
        {
            var handle = window.Properties.NativeWindowHandle.ValueOrDefault;

            if (handle != IntPtr.Zero)
            {
                // Kiểm tra xem window có bị minimize không
                if (IsIconic(handle))
                {
                    Console.WriteLine("🔄 Window đang bị minimize, đang restore...");
                    ShowWindow(handle, SW_RESTORE);
                    Thread.Sleep(300);
                }
                else
                {
                    ShowWindow(handle, SW_SHOW);
                    Thread.Sleep(200);
                }

                // Đưa window lên foreground
                SetForegroundWindow(handle);
                Thread.Sleep(300);
                Console.WriteLine("✅ Đã đưa window lên phía trước");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Không thể restore window: {ex.Message}");
        }
    }

    private void FillTextBox(AutomationElement window, string automationId, string value)
    {
        if (string.IsNullOrEmpty(automationId))
        {
            throw new Exception("AutomationId không được để trống!");
        }

        var textBox = window.FindFirstDescendant(_cf.ByAutomationId(automationId))?.AsTextBox();
        if (textBox != null)
        {
            textBox.Text = value;
            Thread.Sleep(300);
        }
        else
        {
            throw new Exception($"Không tìm thấy TextBox với AutomationId: '{automationId}'. Vui lòng dùng Inspect để kiểm tra AutomationID đúng.");
        }
    }

    private void SelectRadioButton(AutomationElement window, string panelAutomationId, string radioButtonText)
    {
        if (string.IsNullOrEmpty(panelAutomationId))
        {
            throw new Exception("Panel AutomationId không được để trống!");
        }

        var panel = window.FindFirstDescendant(_cf.ByAutomationId(panelAutomationId));
        if (panel == null)
        {
            throw new Exception($"Không tìm thấy Panel với AutomationId: '{panelAutomationId}'. Vui lòng dùng Inspect để kiểm tra.");
        }

        // Tìm RadioButton theo text
        var radioButton = panel.FindFirstDescendant(_cf.ByName(radioButtonText))?.AsRadioButton();
        if (radioButton == null)
        {
            throw new Exception($"Không tìm thấy RadioButton với tên: '{radioButtonText}' trong Panel '{panelAutomationId}'");
        }

        if (!radioButton.IsChecked)
        {
            radioButton.Click();
            Thread.Sleep(300);
        }
    }

    private void FillDataGridView(AutomationElement window, string dataGridViewId, List<GridRowData> gridData)
    {
        if (string.IsNullOrEmpty(dataGridViewId))
        {
            throw new Exception("DataGridView AutomationId không được để trống!");
        }

        if (gridData.Count == 0)
        {
            Console.WriteLine("⚠️ Không có dữ liệu để điền vào DataGridView");
            return;
        }

        var dataGrid = window.FindFirstDescendant(_cf.ByAutomationId(dataGridViewId))?.AsDataGridView();
        if (dataGrid == null)
        {
            throw new Exception($"Không tìm thấy DataGridView với AutomationId: '{dataGridViewId}'. Vui lòng dùng Inspect để kiểm tra.");
        }

        Console.WriteLine($"Tìm thấy DataGridView, bắt đầu điền {gridData.Count} dòng...");

        for (int i = 0; i < gridData.Count; i++)
        {
            var rowData = gridData[i];
            Console.WriteLine($"  Đang điền dòng {i + 1}: Số lượng={rowData.PackageQuantity}, Số lô={rowData.LotNumber}");

            var rows = dataGrid.Rows;

            // Đảm bảo có đủ row
            if (i >= rows.Length)
            {
                Console.WriteLine($"  ⚠️ Không đủ dòng trong DataGridView (có {rows.Length} dòng, cần {gridData.Count} dòng)");
                break;
            }

            var row = rows[i];
            var cells = row.Cells;

            // Nhập trực tiếp vào cell (không dùng keyboard)
            if (cells.Length > 0)
            {
                try
                {
                    // Thử dùng ValuePattern để set value trực tiếp
                    var cell1 = cells[0];
                    if (cell1.Patterns.Value.IsSupported)
                    {
                        cell1.Patterns.Value.Pattern.SetValue(rowData.PackageQuantity.ToString());
                    }
                    else
                    {
                        // Fallback: dùng Text property
                        var textBox = cell1.AsTextBox();
                        if (textBox != null)
                        {
                            textBox.Text = rowData.PackageQuantity.ToString();
                        }
                    }
                    Thread.Sleep(100);
                    Console.WriteLine($"    ✅ Đã nhập số lượng: {rowData.PackageQuantity}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ⚠️ Lỗi khi nhập cột 1: {ex.Message}");
                }
            }

            // Nhập cột 2: Số lô
            if (cells.Length > 1)
            {
                try
                {
                    var cell2 = cells[1];
                    if (cell2.Patterns.Value.IsSupported)
                    {
                        cell2.Patterns.Value.Pattern.SetValue(rowData.LotNumber.ToString());
                    }
                    else
                    {
                        var textBox = cell2.AsTextBox();
                        if (textBox != null)
                        {
                            textBox.Text = rowData.LotNumber.ToString();
                        }
                    }
                    Thread.Sleep(100);
                    Console.WriteLine($"    ✅ Đã nhập số lô: {rowData.LotNumber}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ⚠️ Lỗi khi nhập cột 2: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"✅ Đã điền {gridData.Count} dòng vào DataGridView");
    }

    private void ClickButton(AutomationElement window, string automationId)
    {
        if (string.IsNullOrEmpty(automationId)) return;

        var button = window.FindFirstDescendant(_cf.ByAutomationId(automationId))?.AsButton();
        if (button != null)
        {
            button.Click();
            Thread.Sleep(500);
            Console.WriteLine("Đã nhấn nút Send");
        }
        else
        {
            Console.WriteLine($"Không tìm thấy Button với AutomationId: {automationId}");
        }
    }

    public void Testmethod()
    {
        var application = FlaUI.Core.Application.Launch(@"E:\3_Learn\5_C#\Mock_Tomato\Mock_Tomato\bin\Debug\net10.0-windows\Mock_Tomato.exe");

        var mainWindow = application.GetMainWindow(new UIA3Automation());
        ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Tìm và nhấn nút Send
        var sendButton = mainWindow.FindFirstDescendant(cf.ByName("Send")).AsButton();
        sendButton.Click();


    }
}
