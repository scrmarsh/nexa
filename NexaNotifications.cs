using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace SessionDashboard;

internal sealed class NexaNotifications : IDisposable
{
    internal const string SavingMessage = "Autosaving…";
    internal const string FailureMessage = "Autosave failed — retrying…";
    private sealed record Entry(Border Card, TextBlock Glyph, string Message, DateTimeOffset? Until, int Priority);
    private readonly StackPanel stack = new() { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(18, 18, 18, 18), Width = 330 };
    private readonly StackPanel leftStack=new(){VerticalAlignment=VerticalAlignment.Bottom,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(18,18,18,18),Width=330};
    private readonly ScrollViewer leftViewport=new(){HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
    private static bool LeftSide(string key)=>key is "autosave" or "manual-save";
    private readonly Dictionary<string, Entry> entries = [];
    private readonly DispatcherTimer expiry = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly Grid owner;
    private Window? hostWindow;
    private bool disposed;
    private readonly Func<bool> motionAllowed;
    private readonly ScrollViewer viewport = new() { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0) };
    internal NexaNotifications(Grid owner, Func<bool>? motionAllowed = null)
    {
        this.motionAllowed=motionAllowed ?? (()=>SystemParameters.ClientAreaAnimation);
        this.owner = owner; viewport.Content = stack; Panel.SetZIndex(viewport, 90); owner.Children.Add(viewport);leftViewport.Content=leftStack;Panel.SetZIndex(leftViewport,90);owner.Children.Add(leftViewport);
        owner.SizeChanged += Resize; owner.Loaded += AttachWindow; AttachWindow(null, new RoutedEventArgs()); Resize(null, null);
        expiry.Tick += Tick; expiry.Start();
    }
    private void AttachWindow(object? sender, RoutedEventArgs e)
    {
        var next = Window.GetWindow(owner); if (ReferenceEquals(next, hostWindow)) return;
        DetachWindow(); hostWindow = next;
        if (hostWindow == null || disposed) return;
        hostWindow.LocationChanged += HostChanged; hostWindow.StateChanged += HostChanged; hostWindow.DpiChanged += HostDpiChanged;
    }
    private void DetachWindow()
    {
        if (hostWindow == null) return;
        hostWindow.LocationChanged -= HostChanged; hostWindow.StateChanged -= HostChanged; hostWindow.DpiChanged -= HostDpiChanged; hostWindow = null;
    }
    private void HostChanged(object? sender, EventArgs e) => Resize(null, null);
    private void HostDpiChanged(object sender, DpiChangedEventArgs e) => Resize(null, null);
    private void Resize(object? sender, SizeChangedEventArgs? e)
    {
        if (disposed) return;
        var safe = NexaWindowBounds.TryWorkArea(owner, out var work) ? work : new Rect(0, 0, Math.Max(0, owner.ActualWidth), Math.Max(0, owner.ActualHeight));
        var margin = new Thickness(Math.Max(18, safe.Left + 18), Math.Max(18, safe.Top + 18), Math.Max(18, owner.ActualWidth - safe.Right + 18), Math.Max(18, owner.ActualHeight - safe.Bottom + 18));
        stack.Margin = leftStack.Margin = margin;
        stack.Width = leftStack.Width = Math.Max(120, Math.Min(350, (safe.Width - 54) / 2));
        viewport.MaxHeight = leftViewport.MaxHeight = Math.Max(100, owner.ActualHeight - margin.Top - margin.Bottom - 80);
    }
    internal void Dismiss(string key)
    { if (entries.Remove(key, out var entry)) { entry.Glyph.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, null); entry.Glyph.BeginAnimation(UIElement.OpacityProperty,null); stack.Children.Remove(entry.Card);leftStack.Children.Remove(entry.Card); } }
    internal void Show(string key, string message, string icon = "ℹ", int priority = 0, double? seconds = 5, Action? action = null, string? actionLabel = null)
    {
        Dismiss(key);
        var body = new Grid();body.ColumnDefinitions.Add(new(){Width=GridLength.Auto});body.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});body.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var dismiss = new Button { Content = "×", Focusable = false, Padding = new Thickness(5, 1, 5, 1), VerticalAlignment = VerticalAlignment.Top, ToolTip = "Dismiss notification" };
        dismiss.Click += (_, _) => Dismiss(key); dismiss.Margin=new Thickness(12,0,0,0); Grid.SetColumn(dismiss,2); body.Children.Add(dismiss);
        var glyph = new TextBlock { Text = icon, Foreground = key == "tip" ? new SolidColorBrush(Color.FromRgb(250, 204, 21)) : priority >= 2 ? Brushes.Orange : Brushes.LightGreen, FontSize = key=="tip"?16:20, Margin = new Thickness(0, 0, key=="tip"?7:10, 0) };
        glyph.RenderTransformOrigin = new Point(.5, .5); glyph.RenderTransform = new RotateTransform();
        ConfigureGlyph(key,glyph,message);
        Grid.SetColumn(glyph,0); body.Children.Add(glyph);
        var content = new StackPanel();
        var text = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize=key=="tip"?11.5:12 };
        text.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        System.Windows.Automation.AutomationProperties.SetLiveSetting(text, System.Windows.Automation.AutomationLiveSetting.Polite);
        content.Children.Add(text);
        if (action != null && key!="tip")
        {
            var button = new Button { Content = actionLabel ?? "Review", Focusable = false, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
            button.Click += (_, _) => action(); content.Children.Add(button);
        }
        Grid.SetColumn(content,1);body.Children.Add(content);
        var card = new Border { Child = body, Padding = new Thickness(key=="tip"?10:13), MaxWidth=key=="tip"?270:double.PositiveInfinity, HorizontalAlignment=key=="tip"?HorizontalAlignment.Right:HorizontalAlignment.Stretch, Margin = new Thickness(0, 6, 0, 0), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10) };
        card.SetResourceReference(Border.BackgroundProperty, "PanelBrush"); card.SetResourceReference(Border.BorderBrushProperty, "LineBrush");
        if(key=="tip" && action!=null)
        {
            card.Cursor=System.Windows.Input.Cursors.Hand;card.Focusable=true;
            System.Windows.Automation.AutomationProperties.SetName(card,"Quick tip. "+message);
            System.Windows.Automation.AutomationProperties.SetHelpText(card,"Select this tip to open tip settings.");
            card.MouseLeftButtonUp+=(_,e)=>
            {
                for(var node=e.OriginalSource as DependencyObject;node!=null && node!=card;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node))
                    if(node is System.Windows.Controls.Primitives.ButtonBase)return;
                action();e.Handled=true;
            };
            card.KeyDown+=(_,e)=>{if(e.Key is System.Windows.Input.Key.Enter or System.Windows.Input.Key.Space){action();e.Handled=true;}};
        }
        entries[key] = new(card, glyph, message, seconds is { } duration ? DateTimeOffset.UtcNow.AddSeconds(duration) : null, priority);
        while (entries.Count > 5)
        {
            var drop = entries.Where(p => p.Key != key && p.Value.Priority < 2).OrderBy(p => p.Value.Priority).FirstOrDefault();
            if (drop.Key == null) break; Dismiss(drop.Key);
        }
        stack.Children.Clear();leftStack.Children.Clear();
        foreach(var pair in entries.OrderBy(p=>p.Value.Priority))(LeftSide(pair.Key)?leftStack:stack).Children.Add(pair.Value.Card);
    }
    private void ConfigureGlyph(string key,TextBlock glyph,string message)
    {
        glyph.RenderTransform.BeginAnimation(RotateTransform.AngleProperty,null);glyph.BeginAnimation(UIElement.OpacityProperty,null);glyph.Opacity=1;glyph.Effect=null;
        if(!motionAllowed())return;
        if(key=="autosave" && message==SavingMessage)glyph.RenderTransform.BeginAnimation(RotateTransform.AngleProperty,new DoubleAnimation(0,360,TimeSpan.FromSeconds(1)){RepeatBehavior=RepeatBehavior.Forever});
        if(key=="tip")
        {
            glyph.Effect=new DropShadowEffect{Color=Color.FromRgb(250,204,21),BlurRadius=8,ShadowDepth=0,Opacity=.4};
            glyph.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(.65,1,TimeSpan.FromSeconds(1.6)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever});
        }
    }
    internal void ReconcileMotion(){foreach(var pair in entries)ConfigureGlyph(pair.Key,pair.Value.Glyph,pair.Value.Message);}
    private void Tick(object? sender, EventArgs e)
    { foreach (var key in entries.Where(p => p.Value.Until <= DateTimeOffset.UtcNow).Select(p => p.Key).ToArray()) Dismiss(key); }
    public void Dispose()
    { disposed = true; expiry.Stop(); expiry.Tick -= Tick; owner.SizeChanged -= Resize; owner.Loaded -= AttachWindow; DetachWindow(); owner.Children.Remove(viewport);owner.Children.Remove(leftViewport); foreach (var key in entries.Keys.ToArray()) Dismiss(key); }
}
