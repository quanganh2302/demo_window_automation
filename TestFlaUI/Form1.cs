namespace TestFlaUI;

public partial class Form1 : Form
{
    public FormData SavedData { get; private set; }

    public Form1()
    {
        InitializeComponent();
        SavedData = new FormData();

        btSend.Click += BtSend_Click;
        dt.CellValidating += Dt_CellValidating;
        dt.CellContentClick += Dt_CellContentClick;

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

        // Thêm button để inspect UI
        var btnInspect = new Button
        {
            Text = "🔍 Inspect Mock_Tomato",
            Location = new Point(240, 426),
            Size = new Size(138, 52),
            BackColor = Color.LightBlue
        };
        btnInspect.Click += BtnInspect_Click;
        Controls.Add(btnInspect);
    }

    private void BtnInspect_Click(object? sender, EventArgs e)
    {
        try
        {
            AllocConsole();
            Console.Clear();

            string appPath = @"D:\Quang-Anh\2_Personal\Automation_FlaUI\mock_tomato\Mock_Tomato\bin\Debug\net10.0-windows\Mock_Tomato.exe";

            Console.WriteLine("Bắt đầu inspect Mock_Tomato...\n");
            UIInspector.InspectMockTomato(appPath);

            MessageBox.Show("Đã in danh sách AutomationID ra Console!\nKiểm tra cửa sổ console để xem.", 
                "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
                string appPath = @"D:\Quang-Anh\2_Personal\Automation_FlaUI\mock_tomato\Mock_Tomato\bin\Debug\net10.0-windows\Mock_Tomato.exe";

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
}
