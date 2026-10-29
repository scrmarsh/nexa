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
        UIElement content = close;
        if (panel is SettingsDialog settings)
        {
            content=settings.BuildSettingsFooter(close);
        }
        else if (panel is NexaConfirmation confirmation)
        {
            var row = new DockPanel { LastChildFill = false };
            var cancel = new Button { Content = "Cancel", MinWidth = 88, Margin = new Thickness(18, 12, 0, 12), IsDefault = true, IsCancel = true };
            cancel.Click += (_, _) => confirmation.Cancel(); DockPanel.SetDock(cancel, Dock.Left); row.Children.Add(cancel);
            var destroy = new Button { Content = confirmation.ConfirmLabel, MinWidth = 125, Margin = new Thickness(0, 12, 18, 12), Style = (Style)Application.Current.FindResource("DestructiveButton") };
            destroy.Click += (_, _) => confirmation.Confirm(); DockPanel.SetDock(destroy, Dock.Right); row.Children.Add(destroy);
            content = row;
        }
        else if (panel is DashboardCreation creation)
        {
            var row = new DockPanel { LastChildFill = false };
            var back = new Button { Content = "Back", MinWidth = 88, Margin = new Thickness(18, 12, 0, 12) };
            back.Click += (_, _) => creation.Back(); DockPanel.SetDock(back, Dock.Left); row.Children.Add(back);
            close.Margin = new Thickness(0, 12, 18, 12); DockPanel.SetDock(close, Dock.Right); row.Children.Add(close);
            content = row;
        }
        var footer = new Border { Tag = "PanelCloseBar", Child = content, BorderThickness = new Thickness(0, 1, 0, 0) };
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
        if(Keyboard.FocusedElement is TextBox {Tag:Action cancelName}){cancelName();e.Handled=true;return true;}
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
            if (!panel.IsVisible || Topmost(owner) is not { } top) return;
            // Consume the initiating click before dashboard handlers can reopen a picker or start a drag.
            e.Handled = true; top.CloseWithFade();
        };
        EventHandler activated = (_, _) =>
        {
            // Windows can use the first outside click only to activate the owner;
            // in that case no routed mouse event reaches its content.
            // Closing a child activates its owner before Closed removes this handler.
            // That activation must not cascade into dismissing the parent menu.
            if (panel.IsVisible && !open.Any(p => p.DismissalInProgress && ReferenceEquals(Root(p), Root(owner))) && Topmost(owner) is { } top) top.CloseWithFade();
        };
        owner.AddHandler(UIElement.PreviewMouseDownEvent, outside, true);
        owner.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, outside, true);
        owner.Activated += activated;
        panel.Closed += (_, _) =>
        {
            owner.RemoveHandler(UIElement.PreviewMouseDownEvent, outside);
            owner.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, outside);
            owner.Activated -= activated;
        };
    }
}
