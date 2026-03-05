using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Exceptions;
using FlaUI.UIA3;
using RpaServer.Worker.Utils;

namespace TestFlaUI;

public record HighlightResult(bool Found, string Message);

public enum CheckTarget
{
    WOno,
    CompletedQuantity,
    PackageStyle,
    Printer,
    FindById
}

public class ElementHighlighter : IDisposable
{
    private readonly UIA3Automation _automation;
    private readonly ConditionFactory _cf;
    private readonly AutomationConfig _config;
    private readonly string _appPath;

    private static readonly Color HighlightColor = Color.Red;
    private static readonly TimeSpan HighlightDuration = TimeSpan.FromSeconds(2);

    public ElementHighlighter(string configPath, string appPath)
    {
        _appPath = appPath;
        _cf = new ConditionFactory(new UIA3PropertyLibrary());
        _automation = new UIA3Automation();
        _config = AutomationConfig.LoadFromJson(configPath);
    }

    public HighlightResult CheckAndHighlight(CheckTarget target)
        => CheckAndHighlight(target, null);

    public HighlightResult CheckAndHighlight(CheckTarget target, string? automationId)
    {
        var window = GetTargetWindow();
        if (window == null)
            return new HighlightResult(false, $"Tomato is not open, please check");

        return target switch
        {
            CheckTarget.Printer => HighlightPrinterMenu(window),
            CheckTarget.FindById => HighlightByAutomationId(window, automationId, automationId),
            _ => HighlightByAutomationId(window, GetAutomationId(target), target.ToString())
        };
    }

    private Window? GetTargetWindow()
    {
        var processName = TomatoWindowHelper.GetProcessName(_appPath);
        var window = TomatoWindowHelper.Attach(_automation, processName);
        if (window != null)
            TomatoWindowHelper.BringToFront(window);
        return window;
    }

    private HighlightResult HighlightByAutomationId(Window window, string? automationId, string? label)
    {
        if (string.IsNullOrEmpty(automationId))
            return new HighlightResult(false, $"AutomationId cho '{label}' chưa được cấu hình.");

        var allMatches = window.FindAllDescendants(_cf.ByAutomationId(automationId));

        if (allMatches.Length == 0)
            return new HighlightResult(false, $"Không tìm thấy element '{automationId}'");

        allMatches[0].DrawHighlight(false, HighlightColor, HighlightDuration);

        if (allMatches.Length > 1)
        {
            for (int i = 1; i < allMatches.Length; i++)
                allMatches[i].DrawHighlight(false, HighlightColor, HighlightDuration);

            var details = string.Join("\n", allMatches.Select((el, i) =>
                $"  [{i}] Type={el.ControlType}, Name='{el.Name}'"));
            return new HighlightResult(true,
                $"⚠️ Tìm thấy {allMatches.Length} element trùng AutomationId '{automationId}':\n{details}\n\nĐã highlight element đầu tiên.");
        }

        return new HighlightResult(true, $"Highlighted '{label}'");
    }
    private HighlightResult HighlightPrinterMenu(Window window)
    {
        var toolMenu = window.FindFirstDescendant(_cf.ByName(_config.ToolMenuName));
        if (toolMenu == null)
            return new HighlightResult(false, $"{_config.ToolMenuName} not found");
        toolMenu.DrawHighlight(true, HighlightColor, HighlightDuration);
        return new HighlightResult(true, $"Highlighted menu {_config.ToolMenuName}");
    }

    private string GetAutomationId(CheckTarget target) => target switch
    {
        CheckTarget.WOno => _config.WonoTextBoxId,
        CheckTarget.CompletedQuantity => _config.CompletedQuantityTextBoxId,
        CheckTarget.PackageStyle => _config.PackageStylePanelId,
        _ => string.Empty
    };

    public HighlightResult HighlightEditsByName(string name)
    {
        var window = GetTargetWindow();
        if (window == null)
            return new HighlightResult(false, "Tomato is not open, please check");

        if (string.IsNullOrEmpty(name))
            return new HighlightResult(false, "Tên label không được để trống.");

        // Bước 1: Tìm label theo Name — restrict to Text control type
        var label = window.FindFirstDescendant(
            _cf.ByName(name).And(_cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text)));
        if (label == null)
            return new HighlightResult(false, $"Không tìm thấy element với Name '{name}'");

        // Bước 2: Tìm Edit cùng parent (sibling)
        var parent = label.Parent;
        if (parent == null)
        {
            label.DrawHighlight(false, HighlightColor, HighlightDuration);
            return new HighlightResult(true, $"Tìm thấy label '{name}' nhưng không có parent để tìm Edit.");
        }

        var edits = parent.FindAllChildren(
            _cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));

        if (edits.Length == 0)
        {
            label.DrawHighlight(false, HighlightColor, HighlightDuration);
            return new HighlightResult(true, $"Tìm thấy label '{name}' nhưng không có Edit nào cùng parent.");
        }

        // Bước 3: Chọn Edit gần nhất bên phải, cùng hàng với label
        var labelRect = label.BoundingRectangle;
        var labelCenterY = labelRect.Top + labelRect.Height / 2.0;

        var nearestEdit = edits
            .Where(e =>
            {
                var r = e.BoundingRectangle;
                if (r.IsEmpty) return false;
                // Edit phải nằm bên phải label (cho phép chồng nhẹ 5px)
                if (r.Left < labelRect.Right - 5) return false;
                // Edit phải cùng hàng (tâm Y chênh lệch không quá nửa chiều cao label)
                var editCenterY = r.Top + r.Height / 2.0;
                return Math.Abs(editCenterY - labelCenterY) <= labelRect.Height;
            })
            .OrderBy(e => e.BoundingRectangle.Left - labelRect.Right)
            .FirstOrDefault();

        if (nearestEdit == null)
        {
            label.DrawHighlight(false, HighlightColor, HighlightDuration);
            return new HighlightResult(true, $"Tìm thấy label '{name}' nhưng không xác định được Edit gần nhất.");
        }

        // Highlight cả label và Edit
        label.DrawHighlight(false, Color.Blue, HighlightDuration);
        nearestEdit.DrawHighlight(false, HighlightColor, HighlightDuration);

        return new HighlightResult(true,
            $"Label '{name}' → Edit AutomationId='{nearestEdit.AutomationId}', Name='{nearestEdit.Name}'");
    }

    // Helper to safely check before highlighting
    private static bool TryDrawHighlight(AutomationElement element, bool blocking, Color color, TimeSpan duration)
    {
        try
        {
            element.DrawHighlight(blocking, color, duration);
            return true;
        }
        catch (PropertyNotSupportedException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _automation.Dispose();
    }
}
