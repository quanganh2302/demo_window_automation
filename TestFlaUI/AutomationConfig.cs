using System.Text.Json;

namespace TestFlaUI;

public class AutomationConfig
{
    public string WonoTextBoxId { get; set; } = string.Empty;
    public string CompletedQuantityTextBoxId { get; set; } = string.Empty;
    public string PackageStylePanelId { get; set; } = string.Empty;
    public string ToolMenuName { get; set; } = "Công cụ (T)";
    public string PrinterMenuItemName { get; set; } = "Máy in";
    public string PrinterLabelName { get; set; } = "LABEL_PRINTER (CS)";
    public string PrinterComboBoxId { get; set; } = "cmb_LABEL_PRINTER_CS";
    public string PrinterChangeButtonId { get; set; } = "btnChange";
    public string DataGridViewId { get; set; } = string.Empty;
    public string SendButtonId { get; set; } = string.Empty;

    public static AutomationConfig LoadFromJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Không tìm thấy file cấu hình: {jsonPath}");
        }

        string jsonContent = File.ReadAllText(jsonPath);
        var config = JsonSerializer.Deserialize<AutomationConfig>(jsonContent);

        if (config == null)
        {
            throw new InvalidOperationException("Không thể đọc file cấu hình JSON");
        }

        return config;
    }

    public void SaveToJson(string jsonPath)
    {
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true 
        };
        string jsonContent = JsonSerializer.Serialize(this, options);
        File.WriteAllText(jsonPath, jsonContent);
    }
}
