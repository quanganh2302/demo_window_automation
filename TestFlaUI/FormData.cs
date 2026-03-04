namespace TestFlaUI;

public class GridRowData
{
    public int PackageQuantity { get; set; }
    public int LotNumber { get; set; }

    public override string ToString()
    {
        return $"Số lượng: {PackageQuantity}, Số lô: {LotNumber}";
    }
}

public class FormData
{
    public string WOno { get; set; } = string.Empty;
    public string CustomInput { get; set; } = string.Empty;
    public string CompletedQuantity { get; set; } = string.Empty;
    public string PackageStyle { get; set; } = string.Empty;
    public string Printer { get; set; } = string.Empty;
    public List<GridRowData> GridData { get; set; } = new List<GridRowData>();

    public override string ToString()
    {
        var gridInfo = GridData.Count > 0 
            ? $"\nDữ liệu lưới ({GridData.Count} dòng):\n" + string.Join("\n", GridData)
            : "\nKhông có dữ liệu lưới";

        return $"WOno: {WOno}, Custom Input: {CustomInput}, Completed: {CompletedQuantity}, Style: {PackageStyle}, Printer: {Printer}{gridInfo}";
    }
}
