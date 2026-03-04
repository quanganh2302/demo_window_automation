using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using RpaServer.Worker.Utils;
using System;
using System.Collections.Generic;
using System.Threading;

namespace TestFlaUI;

public class Method
{
    private UIA3Automation _automation;
    private ConditionFactory _cf;
    private AutomationConfig _config;
    private string _appPath;

    public Method(string configPath, string appPath)
    {
        _automation = new UIA3Automation();
        _cf = new ConditionFactory(new UIA3PropertyLibrary());
        _config = AutomationConfig.LoadFromJson(configPath);
        _appPath = appPath;
    }

    public void FillFormData(FormData data)
    {
        try
        {
            Console.WriteLine("🔍 Đang tìm và chờ TOMATO window sẵn sàng...");
            var mainWindow = TomatoWindowHelper.WaitForWindowStable(_automation, _appPath, timeoutMs: 10000)
                                               .GetAwaiter().GetResult();
            if (mainWindow == null)
                throw new Exception("Không tìm thấy hoặc TOMATO chưa sẵn sàng!\nVui lòng mở ứng dụng và đảm bảo đang ở màn hình nhập liệu.");

            Console.WriteLine($"✅ Đã kết nối với TOMATO window: '{mainWindow.Title}'");
            TomatoWindowHelper.BringToFront(mainWindow);
            Thread.Sleep(300);
            Console.WriteLine("💡 Đang nhập dữ liệu...");

            // BƯỚC 1: WOno + Enter
            try
            {
                Console.WriteLine("\n📝 BƯỚC 1: Điền WOno...");
                FillTextBox(mainWindow, _config.WonoTextBoxId, data.WOno);
                Keyboard.Press(VirtualKeyShort.ENTER);
                Thread.Sleep(500);
                Console.WriteLine("✅ Đã điền WOno và nhấn Enter");
            }
            catch (Exception ex) { throw new Exception($"Lỗi tại BƯỚC 1 (WOno): {ex.Message}", ex); }

            // BƯỚC 2: Mở menu Công cụ → Máy in → Chọn printer
            try
            {
                Console.WriteLine("\n📝 BƯỚC 2: Mở Công cụ → Máy in → Chọn printer...");
                OpenMenuAndSelectPrinter(mainWindow, data.Printer);
                Console.WriteLine("✅ Đã chọn máy in thành công");
            }
            catch (Exception ex) { throw new Exception($"Lỗi tại BƯỚC 2 (Máy in): {ex.Message}", ex); }

            // BƯỚC 3: Số hoàn thành
            try
            {
                Console.WriteLine("\n📝 BƯỚC 3: Điền Số hoàn thành...");
                FillTextBox(mainWindow, _config.CompletedQuantityTextBoxId, data.CompletedQuantity);
                Console.WriteLine("✅ Đã điền Số hoàn thành thành công");
            }
            catch (Exception ex) { throw new Exception($"Lỗi tại BƯỚC 3 (Số hoàn thành): {ex.Message}", ex); }

            // BƯỚC 4: Kiểu đóng gói
            try
            {
                Console.WriteLine("\n📝 BƯỚC 4: Chọn kiểu đóng gói...");
                SelectRadioButton(mainWindow, _config.PackageStylePanelId, data.PackageStyle);
                Console.WriteLine("✅ Đã chọn kiểu đóng gói thành công");
            }
            catch (Exception ex) { throw new Exception($"Lỗi tại BƯỚC 4 (Kiểu đóng gói): {ex.Message}", ex); }

            // BƯỚC 5: DataGridView
            try
            {
                Console.WriteLine("\n📝 BƯỚC 5: Điền dữ liệu vào DataGridView...");
                if (!TomatoWindowHelper.IsWindowResponsive(_automation, mainWindow))
                    throw new Exception("TOMATO window không còn responsive trước khi điền DataGridView.");
                FillDataGridView(mainWindow, _config.DataGridViewId, data.GridData);
                Console.WriteLine("✅ Đã điền DataGridView thành công");
            }
            catch (Exception ex) { throw new Exception($"Lỗi tại BƯỚC 5 (DataGridView): {ex.Message}", ex); }

            // BƯỚC 6: Nhấn Send
            if (!string.IsNullOrEmpty(_config.SendButtonId))
            {
                try
                {
                    Console.WriteLine("\n📝 BƯỚC 6: Nhấn nút Send...");
                    ClickButton(mainWindow, _config.SendButtonId);
                    Console.WriteLine("✅ Đã nhấn nút Send thành công");
                }
                catch (Exception ex) { throw new Exception($"Lỗi tại BƯỚC 6 (Button Send): {ex.Message}", ex); }
            }

            Thread.Sleep(500);
            Console.WriteLine("\n🎉 ĐÃ HOÀN THÀNH TẤT CẢ CÁC BƯỚC!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ LỖI: {ex.Message}");
            throw;
        }
    }

    // =========================================================================
    // SafeUiaAction
    // =========================================================================

    private static void SafeUiaAction(Action action, string stepName)
    {
        try { action(); }
        catch (Exception ex) when (HasComEventError(ex))
        {
            Console.WriteLine($"  ⚠️ {stepName}: COM event warning (0x80040201) — bỏ qua");
        }
    }

    private static bool HasComEventError(Exception? ex)
    {
        const int EVENT_E_ALL_SUBSCRIBERS_FAILED = unchecked((int)0x80040201);
        while (ex != null)
        {
            if (ex.HResult == EVENT_E_ALL_SUBSCRIBERS_FAILED) return true;
            ex = ex.InnerException;
        }
        return false;
    }

    // =========================================================================
    // Printer: Menu → Dialog → Select → Change
    // =========================================================================

    private void OpenMenuAndSelectPrinter(Window mainWindow, string printerName)
    {
        const int maxAttempts = 2;
        Exception? lastEx = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                OpenMenuAndSelectPrinterCore(mainWindow, printerName);
                return;
            }
            catch (Exception ex) when (HasComEventError(ex))
            {
                Console.WriteLine("  ⚠️ COM event warning sau nhấn Change — coi như thành công");
                return;
            }
            catch (Exception ex)
            {
                lastEx = ex;
                if (attempt < maxAttempts)
                {
                    Console.WriteLine($"  ⚠️ Attempt {attempt} thất bại ({ex.Message}), thử lại...");
                    SafeUiaAction(() => Keyboard.Press(VirtualKeyShort.ESCAPE), "Escape");
                    Thread.Sleep(500);
                    SafeUiaAction(() => Keyboard.Press(VirtualKeyShort.ESCAPE), "Escape");
                    Thread.Sleep(500);
                }
            }
        }

        throw lastEx ?? new Exception("OpenMenuAndSelectPrinter thất bại.");
    }

    private void OpenMenuAndSelectPrinterCore(Window mainWindow, string printerName)
    {
        // 1. Click menu "Công cụ"
        var toolMenu = mainWindow.FindFirstDescendant(_cf.ByName(_config.ToolMenuName));
        if (toolMenu == null) throw new Exception($"Không tìm thấy menu '{_config.ToolMenuName}'.");
        SafeUiaAction(() => toolMenu.Click(), "Click menu Công cụ");
        Thread.Sleep(500);

        // 2. Click sub-menu "Máy in"
        var printerMenuItem = mainWindow.FindFirstDescendant(_cf.ByName(_config.PrinterMenuItemName));
        if (printerMenuItem == null) throw new Exception($"Không tìm thấy menu item '{_config.PrinterMenuItemName}'.");
        SafeUiaAction(() => printerMenuItem.Click(), "Click Máy in");
        Thread.Sleep(800);

        // 3. Chờ dialog
        AutomationElement? printerDialog = WaitForModalDialog(mainWindow, timeoutMs: 5000)
                                        ?? WaitForDialogFromDesktop(timeoutMs: 5000);
        if (printerDialog == null) throw new Exception("Printer Information dialog không xuất hiện.");
        Console.WriteLine($"  ✅ Đã mở dialog: '{printerDialog.Name}'");
        Thread.Sleep(300);

        // 4. Tìm ComboBox
        var targetCb = printerDialog.FindFirstDescendant(_cf.ByAutomationId(_config.PrinterComboBoxId))
                    ?? mainWindow.FindFirstDescendant(_cf.ByAutomationId(_config.PrinterComboBoxId))
                    ?? _automation.GetDesktop().FindFirstDescendant(_cf.ByAutomationId(_config.PrinterComboBoxId));

        if (targetCb == null)
        {
            Console.WriteLine("  📋 Tất cả elements trong dialog:");
            foreach (var el in printerDialog.FindAllDescendants())
                Console.WriteLine($"    Type={el.ControlType}, Name='{el.Name}', AutoId='{el.AutomationId}'");
            throw new Exception($"Không tìm thấy ComboBox '{_config.PrinterComboBoxId}'.");
        }
        Console.WriteLine($"  ✅ Tìm thấy ComboBox: AutoId='{targetCb.AutomationId}', Name='{targetCb.Name}'");

        // 5. Chọn printer
        SelectComboBoxValue(targetCb, printerName);
        Console.WriteLine($"  ✅ Đã chọn printer: {printerName}");
        Thread.Sleep(200);

        // 6. Click Change — dialog vẫn còn vì không dùng ESCAPE/ENTER
        var changeBtn = printerDialog.FindFirstDescendant(_cf.ByAutomationId(_config.PrinterChangeButtonId))?.AsButton()
                     ?? printerDialog.FindFirstDescendant(_cf.ByName("Change"))?.AsButton();
        if (changeBtn == null) throw new Exception("Không tìm thấy nút Change.");

        string btnName = "Change";
        try { btnName = changeBtn.Name; } catch { }
        SafeUiaAction(() => changeBtn.Click(), "Click Change");
        Thread.Sleep(500);
        Console.WriteLine($"  ✅ Đã nhấn '{btnName}'");
    }

    /// <summary>
    /// Mở dropdown 1 lần để đọc danh sách item và tìm index của target.
    /// Đóng dropdown bằng ALT+UP (không đóng dialog).
    /// Navigate bằng HOME + DOWN đúng số lần — không đọc UIA trong vòng lặp.
    /// </summary>
    private void SelectComboBoxValue(AutomationElement comboBoxElement, string value)
    {
        Console.WriteLine("  🔄 Mở dropdown để tìm index, sau đó navigate chính xác...");

        // BƯỚC 1: Mở dropdown
        SafeUiaAction(() => comboBoxElement.Click(), "Mở dropdown");
        Thread.Sleep(300); // Chờ dropdown render xong

        // BƯỚC 2: Đọc ComboLBox — bọc try-catch COM để không bubble up
        int targetIndex = -1;
        try
        {
            var desktop = _automation.GetDesktop();
            AutomationElement? dropdownList = null;

            // Dùng FindFirstDescendant với ByClassName — không enumerate toàn bộ Desktop
            for (int t = 0; t < 3 && dropdownList == null; t++)
            {
                try
                {
                    dropdownList = desktop.FindFirstDescendant(_cf.ByClassName("ComboLBox"));
                }
                catch (Exception ex) when (HasComEventError(ex)) { /* bỏ qua, thử lại */ }

                if (dropdownList == null) Thread.Sleep(100);
            }

            if (dropdownList != null)
            {
                var items = dropdownList.FindAllChildren();
                Console.WriteLine($"  📋 Dropdown có {items.Length} items:");
                for (int i = 0; i < items.Length; i++)
                {
                    Console.WriteLine($"    [{i}] '{items[i].Name}'");
                    if (items[i].Name.Equals(value, StringComparison.OrdinalIgnoreCase))
                        targetIndex = i;
                }
            }
            else
            {
                Console.WriteLine("  ⚠️ Không tìm thấy ComboLBox");
            }
        }
        catch (Exception ex) when (HasComEventError(ex))
        {
            Console.WriteLine($"  ⚠️ COM warning khi đọc dropdown — bỏ qua");
        }

        // BƯỚC 3: Đóng dropdown bằng ALT+UP
        Keyboard.TypeSimultaneously(VirtualKeyShort.ALT, VirtualKeyShort.UP);
        Thread.Sleep(200);

        if (targetIndex < 0)
        {
            Console.WriteLine($"  ⚠️ Không tìm thấy '{value}' trong danh sách");
            return;
        }

        // BƯỚC 4: Focus lại ComboBox, HOME + DOWN đúng số lần
        SafeUiaAction(() => comboBoxElement.Click(), "Focus ComboBox");
        Thread.Sleep(100);
        Keyboard.Press(VirtualKeyShort.HOME);
        Thread.Sleep(80);

        for (int i = 0; i < targetIndex; i++)
        {
            Keyboard.Press(VirtualKeyShort.DOWN);
            Thread.Sleep(40);
        }

        Console.WriteLine($"  ✅ Đã chọn: '{value}' (index {targetIndex})");
    }

    // =========================================================================
    // Dialog helpers
    // =========================================================================

    private Window? WaitForModalDialog(Window parentWindow, int timeoutMs = 5000)
    {
        int elapsed = 0;
        while (elapsed < timeoutMs)
        {
            var modals = parentWindow.ModalWindows;
            if (modals.Length > 0) return modals[0];
            Thread.Sleep(250);
            elapsed += 250;
        }
        return null;
    }

    private AutomationElement? WaitForDialogFromDesktop(int timeoutMs = 5000)
    {
        int elapsed = 0;
        while (elapsed < timeoutMs)
        {
            var desktop = _automation.GetDesktop();
            foreach (var win in desktop.FindAllChildren(_cf.ByControlType(ControlType.Window)))
            {
                var name = win.Name ?? string.Empty;
                if (name.Contains("Printer", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Máy in", StringComparison.OrdinalIgnoreCase))
                    return win;
            }
            Thread.Sleep(250);
            elapsed += 250;
        }
        return null;
    }

    // =========================================================================
    // Các private helper
    // =========================================================================

    private void FillTextBox(AutomationElement window, string automationId, string value)
    {
        if (string.IsNullOrEmpty(automationId))
            throw new Exception("AutomationId không được để trống!");
        var textBox = window.FindFirstDescendant(_cf.ByAutomationId(automationId))?.AsTextBox();
        if (textBox == null)
            throw new Exception($"Không tìm thấy TextBox với AutomationId: '{automationId}'.");
        textBox.Text = value;
        Thread.Sleep(300);
    }

    private void SelectRadioButton(AutomationElement window, string panelAutomationId, string radioButtonText)
    {
        if (string.IsNullOrEmpty(panelAutomationId))
            throw new Exception("Panel AutomationId không được để trống!");
        var panel = window.FindFirstDescendant(_cf.ByAutomationId(panelAutomationId));
        if (panel == null)
            throw new Exception($"Không tìm thấy Panel với AutomationId: '{panelAutomationId}'.");
        var radioButton = panel.FindFirstDescendant(_cf.ByName(radioButtonText))?.AsRadioButton();
        if (radioButton == null)
            throw new Exception($"Không tìm thấy RadioButton '{radioButtonText}' trong Panel '{panelAutomationId}'.");
        if (!radioButton.IsChecked)
        {
            radioButton.Click();
            Thread.Sleep(300);
        }
    }

    private void FillDataGridView(AutomationElement window, string dataGridViewId, List<GridRowData> gridData)
    {
        if (string.IsNullOrEmpty(dataGridViewId))
            throw new Exception("DataGridView AutomationId không được để trống!");
        if (gridData.Count == 0)
        {
            Console.WriteLine("⚠️ Không có dữ liệu để điền vào DataGridView");
            return;
        }
        var dataGrid = window.FindFirstDescendant(_cf.ByAutomationId(dataGridViewId))?.AsDataGridView();
        if (dataGrid == null)
            throw new Exception($"Không tìm thấy DataGridView với AutomationId: '{dataGridViewId}'.");

        Console.WriteLine($"Tìm thấy DataGridView, bắt đầu điền {gridData.Count} dòng...");
        for (int i = 0; i < gridData.Count; i++)
        {
            var rowData = gridData[i];
            Console.WriteLine($"  Đang điền dòng {i + 1}: Số lượng={rowData.PackageQuantity}, Số lô={rowData.LotNumber}");
            var rows = dataGrid.Rows;
            if (i >= rows.Length)
            {
                Console.WriteLine($"  ⚠️ Không đủ dòng (có {rows.Length}, cần {gridData.Count})");
                break;
            }
            var cells = rows[i].Cells;
            if (cells.Length > 0)
            {
                try { SetCellValue(cells[0], rowData.PackageQuantity.ToString()); Thread.Sleep(100); Console.WriteLine($"    ✅ Đã nhập số lượng: {rowData.PackageQuantity}"); }
                catch (Exception ex) { Console.WriteLine($"    ⚠️ Lỗi cột 1: {ex.Message}"); }
            }
            if (cells.Length > 1)
            {
                try { SetCellValue(cells[1], rowData.LotNumber.ToString()); Thread.Sleep(100); Console.WriteLine($"    ✅ Đã nhập số lô: {rowData.LotNumber}"); }
                catch (Exception ex) { Console.WriteLine($"    ⚠️ Lỗi cột 2: {ex.Message}"); }
            }
        }
        Console.WriteLine($"✅ Đã điền {gridData.Count} dòng vào DataGridView");
    }

    private static void SetCellValue(AutomationElement cell, string value)
    {
        if (cell.Patterns.Value.IsSupported)
            cell.Patterns.Value.Pattern.SetValue(value);
        else
            cell.AsTextBox().Text = value;
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
}