using System.Windows;
using System.Windows.Threading;

namespace SessionDashboard;

internal static class DashboardTips
{
    internal static readonly string[] Messages =
    [
        "Tip: Click empty space to add a widget.",
        "Tip: Press Esc to close menus.",
        "Tip: Settings save automatically.",
        "Tip: Use Add Widget to open your widget library.",
        "Tip: Try Pixel Swarm when you need a break."
    ];
}

public partial class MainWindow
{
    private readonly DispatcherTimer tipRotation = new() { Interval = TimeSpan.FromSeconds(30) };
    private int tipIndex;
    private void InitializeDashboardTips()
    {
        tipRotation.Tick += (_, _) => AdvanceDashboardTip();
        tipRotation.Start(); Closed += (_, _) => tipRotation.Stop();
    }
    internal void AdvanceDashboardTip()
    {
        if (state.Compact || storyOpen || TutorialActive || !Options.ShowQuickTips) return;
        tipIndex = (tipIndex + 1) % DashboardTips.Messages.Length;
        DashboardTip.Text = DashboardTips.Messages[tipIndex];
        notifications?.Show("tip", DashboardTips.Messages[tipIndex], "💡", seconds:5,action:()=>OpenMainSettings("Tips"),actionLabel:"Tip settings");
    }
}
