using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SessionDashboard;

// Owned panels share dismissal and backdrop behavior; Home and the dashboard keep native window controls.
internal class NexaPanel : NexaDialog
{
    internal NexaPanel() : base(true) { }
}

internal static class NexaPanels
{
    private static readonly List<NexaDialog> open = [];
    private static readonly Dictionary<UIElement, (int Count, double Opacity)> backdrops = [];
    private static readonly Dictionary<NexaDialog, UIElement> backdropSources = [];
    private static Window Root(Window window) { while (window.Owner != null) window = window.Owner; return window; }
    internal static NexaDialog? Topmost(Window window) => open.LastOrDefault(p => p.IsVisible && ReferenceEquals(Root(p), Root(window)));
    internal static void Register(NexaDialog panel)
    {
        if (panel.Owner == null || open.Contains(panel)) return;
        open.Add(panel);
        if (!panel.IsPanel || panel.Owner.Content is not UIElement content) return;
        var backdrop = backdrops.GetValueOrDefault(content, (0, content.Opacity));
        backdropSources[panel] = content;
        backdrops[content] = (backdrop.Item1 + 1, backdrop.Item2);
        content.SetCurrentValue(UIElement.OpacityProperty, backdrop.Item2 * .78);
    }
    internal static void Unregister(NexaDialog panel)
    {
        open.Remove(panel);
        if (!backdropSources.Remove(panel, out var content) || !backdrops.TryGetValue(content, out var backdrop)) return;
        if (backdrop.Count > 1) backdrops[content] = (backdrop.Count - 1, backdrop.Opacity);
        else { content.SetCurrentValue(UIElement.OpacityProperty, backdrop.Opacity); backdrops.Remove(content); }
    }
    internal static void FitOwner(NexaDialog panel)
    {
        if (panel.Owner is not { } owner) return;
        var width = Math.Max(320, owner.ActualWidth - 32); var height = Math.Max(260, owner.ActualHeight - 32);
        panel.MinWidth = Math.Min(panel.MinWidth, width); panel.MinHeight = Math.Min(panel.MinHeight, height);
        panel.Width = Math.Min(panel.Width, width); panel.Height = Math.Min(panel.Height, height);
    }
    internal static Border CloseFooter(NexaDialog panel)
    {
        var close = new Button { Content = "Close", MinWidth = 88, HorizontalAlignment = HorizontalAlignment.Left, ToolTip = "Close panel (Esc)", Margin = new Thickness(18, 12, 18, 12) };
        System.Windows.Automation.AutomationProperties.SetName(close, "Close panel");
        close.Click += (_, _) => panel.CloseWithFade();
        var footer = new Border { Tag = "PanelCloseBar", Child = close, BorderThickness = new Thickness(0, 1, 0, 0) };
        footer.SetResourceReference(Border.BackgroundProperty, "PanelBrush"); footer.SetResourceReference(Border.BorderBrushProperty, "LineBrush");
        return footer;
    }
    internal static bool DismissTopmost(Window window)
    {
        if (Topmost(window) is not { } panel) return false;
        panel.CloseWithFade(); return true;
    }
    internal static bool HandleEscape(Window window, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.Escape || Topmost(window) is not { } panel) return false;
        // A dropdown/calendar above the panel gets the first Escape.
        if (panel.Body != null && ClosePopup(panel.Body, new HashSet<DependencyObject>())) { e.Handled = true; return true; }
        panel.CloseWithFade(); e.Handled = true; return true;
    }
    private static bool ClosePopup(DependencyObject node, HashSet<DependencyObject> visited)
    {
        if (!visited.Add(node)) return false;
        if (node is Popup { IsOpen: true } popup) { popup.IsOpen = false; return true; }
        if (node is ComboBox { IsDropDownOpen: true } combo) { combo.IsDropDownOpen = false; return true; }
        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()) if (ClosePopup(child, visited)) return true;
        if (node is Visual)
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) if (ClosePopup(VisualTreeHelper.GetChild(node, i), visited)) return true;
        return false;
    }
    internal static void EnableOutsideDismissal(NexaDialog panel)
    {
        if (panel.Owner is not { } owner) return;
        MouseButtonEventHandler outside = (_, e) =>
        {
            if (e.Handled || !panel.IsVisible || Topmost(owner) is not { } top) return;
            // Consume the initiating click before dashboard handlers can reopen a picker or start a drag.
            e.Handled = true; top.CloseWithFade();
        };
        owner.AddHandler(UIElement.PreviewMouseDownEvent, outside, true);
        panel.Closed += (_, _) => owner.RemoveHandler(UIElement.PreviewMouseDownEvent, outside);
    }
}
