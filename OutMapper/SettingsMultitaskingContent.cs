using Microsoft.UI.Text;
using Windows.Storage;

namespace OutMapper;

public sealed class SettingsMultitaskingContent : Border
{
    private const string ComputeModeKey = "ComputeMode";
    private const string AllCoresMode = "AllCores";
    private const string LeaveOneFreeMode = "LeaveOneFree";
    private const string LeaveTwoFreeMode = "LeaveTwoFree";
    private const string OneCoreOnlyMode = "OneCoreOnly";

    public SettingsMultitaskingContent()
    {
        Padding = new Thickness(16);

        var title = new TextBlock
        {
            Text = "Multitasking",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var description = new TextBlock
        {
            Text = "Calculations can take a few minutes and use every processor core by default " +
                   "(a core is one of the individual units inside your computer's processor that does the work). " +
                   "Choose Background if you want to keep using other apps, like video or music, while OutMapper is working.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var processorCount = Environment.ProcessorCount;
        var coreCountLabel = new TextBlock
        {
            Text = processorCount == 1
                ? "Your computer has 1 core."
                : $"Your computer has {processorCount} cores.",
            Margin = new Thickness(0, 0, 0, 16)
        };

        var options = new (string Mode, string Label)[]
        {
            (AllCoresMode, "All cores — use every core for the fastest calculations"),
            (LeaveOneFreeMode, "Leave one core free — fast, with a little room for other apps"),
            (LeaveTwoFreeMode, "Leave two cores free — more room for other apps"),
            (OneCoreOnlyMode, "Use only one core — slowest, but leaves the rest of your computer free")
        };

        var currentMode = LoadComputeMode();
        var optionsPanel = new StackPanel { Orientation = Orientation.Vertical };

        foreach (var (mode, label) in options)
        {
            var option = new RadioButton
            {
                GroupName = "ComputeMode",
                Content = label,
                IsChecked = mode == currentMode,
                Margin = new Thickness(0, 4, 0, 0)
            };
            option.Checked += (_, _) => SaveComputeMode(mode);
            optionsPanel.Children.Add(option);
        }

        Child = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                title,
                description,
                coreCountLabel,
                optionsPanel
            }
        };
    }

    public static string LoadComputeMode()
    {
        return ApplicationData.Current.LocalSettings.Values.TryGetValue(ComputeModeKey, out var value)
            ? value as string ?? LeaveOneFreeMode
            : LeaveOneFreeMode;
    }

    private static void SaveComputeMode(string mode)
    {
        ApplicationData.Current.LocalSettings.Values[ComputeModeKey] = mode;
    }

    public static int GetMaxDegreeOfParallelism()
    {
        var mode = LoadComputeMode();
        if (mode == OneCoreOnlyMode)
        {
            return 1;
        }

        var processorCount = Environment.ProcessorCount;
        var reservedCores = mode switch
        {
            AllCoresMode => 0,
            LeaveTwoFreeMode => 2,
            _ => 1
        };

        return Math.Max(1, processorCount - reservedCores);
    }
}
