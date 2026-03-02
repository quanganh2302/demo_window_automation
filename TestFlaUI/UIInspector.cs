using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;
using System;
using System.Linq;

namespace TestFlaUI;

public class UIInspector
{
    public static void InspectMockTomato(string appPath)
    {
        var processName = Path.GetFileNameWithoutExtension(appPath);
        var runningProcesses = System.Diagnostics.Process.GetProcessesByName(processName);

        if (runningProcesses.Length == 0)
        {
            Console.WriteLine($"❌ Không tìm thấy {processName} đang chạy!");
            return;
        }

        var automation = new UIA3Automation();
        var application = FlaUI.Core.Application.Attach(runningProcesses[0].Id);
        var mainWindow = application.GetMainWindow(automation);

        Console.WriteLine("========================================");
        Console.WriteLine("📋 DANH SÁCH TẤT CẢ CONTROLS CÓ AUTOMATIONID");
        Console.WriteLine("========================================\n");

        PrintAllControlsWithAutomationId(mainWindow, 0);

        application.Dispose();
    }

    private static void PrintAllControlsWithAutomationId(AutomationElement element, int depth)
    {
        try
        {
            var automationId = element.Properties.AutomationId.ValueOrDefault;
            var name = element.Properties.Name.ValueOrDefault;
            var controlType = element.Properties.ControlType.ValueOrDefault;

            if (!string.IsNullOrEmpty(automationId))
            {
                string indent = new string(' ', depth * 2);
                Console.WriteLine($"{indent}[{controlType}]");
                Console.WriteLine($"{indent}  AutomationId: \"{automationId}\"");
                Console.WriteLine($"{indent}  Name: \"{name}\"");
                Console.WriteLine();
            }

            // Đệ quy tìm tất cả children
            var children = element.FindAllChildren();
            foreach (var child in children)
            {
                PrintAllControlsWithAutomationId(child, depth + 1);
            }
        }
        catch (Exception)
        {
            // Ignore controls that can't be accessed
        }
    }

    public static void InspectSpecificControl(string appPath, string automationId)
    {
        var processName = Path.GetFileNameWithoutExtension(appPath);
        var runningProcesses = System.Diagnostics.Process.GetProcessesByName(processName);

        if (runningProcesses.Length == 0)
        {
            Console.WriteLine($"❌ Không tìm thấy {processName} đang chạy!");
            return;
        }

        var automation = new UIA3Automation();
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var application = FlaUI.Core.Application.Attach(runningProcesses[0].Id);
        var mainWindow = application.GetMainWindow(automation);

        Console.WriteLine($"🔍 Tìm kiếm control với AutomationId: {automationId}");
        
        var control = mainWindow.FindFirstDescendant(cf.ByAutomationId(automationId));
        
        if (control != null)
        {
            Console.WriteLine($"✅ Tìm thấy!");
            Console.WriteLine($"  ControlType: {control.Properties.ControlType.ValueOrDefault}");
            Console.WriteLine($"  Name: {control.Properties.Name.ValueOrDefault}");
            Console.WriteLine($"  ClassName: {control.Properties.ClassName.ValueOrDefault}");
            Console.WriteLine($"  IsEnabled: {control.Properties.IsEnabled.ValueOrDefault}");
            Console.WriteLine($"  IsVisible: {control.Properties.IsOffscreen.ValueOrDefault}");
        }
        else
        {
            Console.WriteLine($"❌ KHÔNG tìm thấy control với AutomationId: {automationId}");
        }

        application.Dispose();
    }
}
