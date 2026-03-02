namespace TestFlaUI;

public class AutomationHelper
{
    public static void RunAutomation(FormData formData, string configPath, string appPath)
    {
        var method = new Method(configPath, appPath);
        method.FillFormData(formData);
    }
}
