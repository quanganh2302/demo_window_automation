using Button = System.Windows.Forms.Button;
using DataGridViewRow = System.Windows.Forms.DataGridViewRow;

namespace TestFlaUI;

public partial class Form1 : Form
{
    public FormData SavedData { get; private set; }

    const string appPath = @"D:\Quang-Anh\2_Personal\Automation_FlaUI\mock_tomato\Mock_Tomato\bin\Debug\net10.0-windows\Mock_Tomato.exe";

    public Form1()
    {
        InitializeComponent();
        SavedData = new FormData();

        btSend.Click += BtSend_Click;
        dt.CellValidating += Dt_CellValidating;
        dt.CellContentClick += Dt_CellContentClick;

        btnCheckWono.Click += (s, e) => CheckAndHighlight(CheckTarget.WOno, btnCheckWono);
        btnCheckCom.Click += (s, e) => CheckAndHighlight(CheckTarget.CompletedQuantity, btnCheckCom);
        btnCheckType.Click += (s, e) => CheckAndHighlight(CheckTarget.PackageStyle, btnCheckType);
        btnCheckPrint.Click += (s, e) => CheckAndHighlight(CheckTarget.Printer, btnCheckPrint);
        btnFindById.Click += (s, e) => FindByIdAndHighlight();
        tbFindById.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { FindByIdAndHighlight(); e.SuppressKeyPress = true; } };
        tbFindById.PlaceholderText = "AutomationId...";
        tbFindTest.PlaceholderText = "Label name...";
        tbFindTest.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnFindTest_Click(btnFindTest, EventArgs.Empty); e.SuppressKeyPress = true; } };
        dt.AllowUserToAddRows = true;

        cbStyle.Items.AddRange(new string[]
        {
            "Hộp bìa",
            "Kiện",
            "Bao Ni-lon",
            "Gói giấy",
            "Khác"
        });

        cbPrinter.Items.AddRange(new string[]
        {
            "Microsoft Print to PDF",
            "OneNote (Desktop)",
            "MF240 Series"
        });

    }

    private void Dt_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        if (dt.Columns[e.ColumnIndex].Name == "dataGridViewTextBoxColumn3")
        {
            if (MessageBox.Show("Bạn có muốn xóa dòng này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                dt.Rows.RemoveAt(e.RowIndex);
            }
        }
    }

    private void Dt_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (e.ColumnIndex == 2) return;

        if (dt.Rows[e.RowIndex].IsNewRow) return;

        string value = e.FormattedValue?.ToString() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(value) && !int.TryParse(value, out _))
        {
            MessageBox.Show("Vui lòng chỉ nhập số!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }
    }

    private void BtSend_Click(object? sender, EventArgs e)
    {
        if (!ValidateInputs())
        {
            MessageBox.Show("Vui lòng điền đầy đủ thông tin!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SavedData.WOno = tbWono.Text.Trim();
        SavedData.CompletedQuantity = tbCom.Text.Trim();
        SavedData.PackageStyle = cbStyle.Text.Trim();
        SavedData.Printer = cbPrinter.Text.Trim();

        SavedData.GridData.Clear();
        foreach (DataGridViewRow row in dt.Rows)
        {
            if (row.IsNewRow) continue;

            var col1 = row.Cells[0].Value?.ToString();
            var col2 = row.Cells[1].Value?.ToString();

            if (!string.IsNullOrWhiteSpace(col1) && !string.IsNullOrWhiteSpace(col2))
            {
                if (int.TryParse(col1, out int qty) && int.TryParse(col2, out int lot))
                {
                    SavedData.GridData.Add(new GridRowData
                    {
                        PackageQuantity = qty,
                        LotNumber = lot
                    });
                }
            }
        }

        MessageBox.Show($"Đã lưu dữ liệu:\n{SavedData}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

        // Tự động điền vào Mock_Tomato (tuỳ chọn)
        if (MessageBox.Show("Bạn có muốn tự động điền dữ liệu vào Mock_Tomato?",
            "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "automation_config.json");

                // Hiển thị cửa sổ console để xem log
                AllocConsole();

                AutomationHelper.RunAutomation(SavedData, configPath, appPath);

                // Minimize app đích sau khi điền thành công
                MinimizeTargetApp(appPath);

                MessageBox.Show("Đã điền dữ liệu vào Mock_Tomato thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Hiển thị lỗi chi tiết
                string errorMessage = $"Lỗi khi điền dữ liệu tự động:\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nChi tiết: {ex.InnerException.Message}";
                }
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Đưa app hiện tại lên trước app đích và hiển thị popup
                BringCurrentAppToFront();
                MessageBox.Show(this, errorMessage, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

    private void MinimizeTargetApp(string appPath)
    {
        try
        {
            var processName = Path.GetFileNameWithoutExtension(appPath);
            var processes = System.Diagnostics.Process.GetProcessesByName(processName);
            foreach (var proc in processes)
            {
                if (proc.MainWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(proc.MainWindowHandle, SW_MINIMIZE);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Không thể minimize app đích: {ex.Message}");
        }
    }

    private void BringCurrentAppToFront()
    {
        try
        {
            var handle = Handle;
            ShowWindow(handle, SW_RESTORE);
            SetForegroundWindow(handle);
            Activate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Không thể đưa app hiện tại lên trước: {ex.Message}");
        }
    }

    private void FindByIdAndHighlight()
    {
        var id = tbFindById.Text.Trim();
        if (string.IsNullOrEmpty(id))
        {
            MessageBox.Show(this, "Vui lòng nhập AutomationId.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tbFindById.Focus();
            return;
        }

        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "automation_config.json");

            using var highlighter = new ElementHighlighter(configPath, appPath);
            var result = highlighter.CheckAndHighlight(CheckTarget.FindById, id);

            ApplyButtonFeedback(btnFindById, result);
        }
        catch (Exception ex)
        {
            BringCurrentAppToFront();
            MessageBox.Show(this, $"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CheckAndHighlight(CheckTarget target, Button button)
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "automation_config.json");

            using var highlighter = new ElementHighlighter(configPath, appPath);
            var result = highlighter.CheckAndHighlight(target);

            ApplyButtonFeedback(button, result);
        }
        catch (Exception ex)
        {
            BringCurrentAppToFront();
            MessageBox.Show(this, $"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyButtonFeedback(Button button, HighlightResult result)
    {
        var originalColor = button.BackColor;
        var originalText = button.Text;

        if (result.Found)
        {
            button.BackColor = Color.LightGreen;
            button.Text = "✓ OK";

            if (result.Message.Contains("⚠️"))
            {
                button.BackColor = Color.LightYellow;
                button.Text = "⚠ Dup";
                BringCurrentAppToFront();
                MessageBox.Show(this, result.Message, "Trùng AutomationId", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            button.BackColor = Color.LightCoral;
            button.Text = "✗ Fail";
            BringCurrentAppToFront();
            MessageBox.Show(this, result.Message, "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        var resetTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        resetTimer.Tick += (s, e) =>
        {
            button.BackColor = originalColor;
            button.Text = originalText;
            resetTimer.Stop();
            resetTimer.Dispose();
        };
        resetTimer.Start();
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(tbWono.Text))
            return false;


        if (string.IsNullOrWhiteSpace(tbCom.Text))
            return false;

        if (string.IsNullOrWhiteSpace(cbStyle.Text))
            return false;

        if (string.IsNullOrWhiteSpace(cbPrinter.Text))
            return false;

        return true;
    }

    private async void btnFindTest_Click(object sender, EventArgs e)
    {
        var name = tbFindTest.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show(this, "Vui lòng nhập tên label.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tbFindTest.Focus();
            return;
        }

        btnFindTest.Enabled = false;
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "automation_config.json");

            var result = await Task.Run(() =>
            {
                using var highlighter = new ElementHighlighter(configPath, appPath);
                return highlighter.HighlightEditsByName(name);
            });

            ApplyButtonFeedback(btnFindTest, result);
        }
        catch (Exception ex)
        {
            BringCurrentAppToFront();
            MessageBox.Show(this, $"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnFindTest.Enabled = true;
        }
    }

}
