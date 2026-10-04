using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckShared012()
    {
        var store = new StateStore(Path.Combine(output, "shared012.json"));
        var pixel = WidgetLibrary.Create("Pixel timer", "pixels"); pixel.BannerSpan = 12;
        var clock = WidgetLibrary.Create("Clock"); clock.Span = 8; clock.PlacementX = .6; clock.PlacementY = 0;
        var notes = WidgetLibrary.Create("Notes", "notes"); notes.Span = 10; notes.PlacementX = 0; notes.PlacementY = 480;
        var tasks = WidgetLibrary.Create("Tasks", "tasks"); tasks.Span = 10; tasks.PlacementX = .5; tasks.PlacementY = 480;
        store.Save(new AppState { WidgetLayoutInitialized = true, Widgets = [pixel, clock, notes, tasks] });
        Theme.Apply(Application.Current.Resources, store.LoadThemeMode(), store.LoadAppTheme());
        var window = new MainWindow(store) { Width = 1450, Height = 950 }; window.Show(); Pump(TimeSpan.FromMilliseconds(220));
        Border Frame(WidgetInstance widget) => window.WidgetCanvas.Children.OfType<Border>().Single(f => Equals(f.Tag, widget.Id));
        Rect Bounds(WidgetInstance widget) { var f = Frame(widget); return new Rect(Canvas.GetLeft(f), Canvas.GetTop(f), f.Width, f.Height); }
        var timer = window.PixelBannerContainer;
        Assert(Math.Abs(Bounds(clock).Left - timer.Width - 8) < 1 && timer.Height == 360 &&
            timer.Children.OfType<ContentControl>().Single().Content is Border card && card.BorderThickness == new Thickness(1) &&
            card.CornerRadius == Frame(clock).CornerRadius && Equals(card.BorderBrush, Frame(clock).BorderBrush),
            "Pixel Timer uses the normal card edge and meets its neighbor across one widget gap");
        var clockBefore = Bounds(clock); var pixelBefore = timer.Width;
        window.PixelWidthHandle.RaiseEvent(new DragStartedEventArgs(0, 0));
        window.PixelWidthHandle.RaiseEvent(new DragDeltaEventArgs(75, 0));
        Assert(timer.Width > pixelBefore && Bounds(clock).Width < clockBefore.Width && Bounds(clock).Top == clockBefore.Top,
            "Pixel and Clock trade width during the drag without moving rows");
        window.PixelWidthHandle.RaiseEvent(new DragCompletedEventArgs(0, 0, false)); Pump(TimeSpan.FromMilliseconds(90));
        Assert(Math.Abs(Bounds(clock).Left - timer.Width - 8) < 1 &&
            store.Load().Widgets.Single(w => w.Id == clock.Id).Span == window.state.Widgets.Single(w => w.Id == clock.Id).Span,
            "Shared Pixel/Clock width persists without a gap");
        var left = Bounds(notes); var right = Bounds(tasks);
        Assert(Math.Abs(right.Left - left.Right - 8) < 1 && left.Top == right.Top,
            "Adjacent normal widgets begin as a continuous row");
        var handle = window.WidgetCanvas.Children.OfType<Thumb>().Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Resize Right " + notes.Title);
        var unit = (window.WidgetCanvas.ActualWidth + 8) / 20;
        handle.RaiseEvent(new DragStartedEventArgs(0, 0)); handle.RaiseEvent(new DragDeltaEventArgs(unit, 0));
        Assert(Bounds(notes).Width > left.Width && Bounds(tasks).Width < right.Width && Bounds(tasks).Top == right.Top,
            "Dragging a shared widget edge resizes both cards live");
        handle.RaiseEvent(new DragCompletedEventArgs(0, 0, false)); Pump(TimeSpan.FromMilliseconds(90));
        Assert(Math.Abs(Bounds(tasks).Left - Bounds(notes).Right - 8) < 1 &&
            store.Load().Widgets.Single(w => w.Id == notes.Id).Span == window.state.Widgets.Single(w => w.Id == notes.Id).Span,
            "Normal shared-edge resize saves a gap-free row");
        var committedLeft = Bounds(notes); var committedRight = Bounds(tasks);
        handle.RaiseEvent(new DragStartedEventArgs(0, 0)); handle.RaiseEvent(new DragDeltaEventArgs(-unit, 0));
        handle.RaiseEvent(new DragCompletedEventArgs(0, 0, true)); Pump(TimeSpan.FromMilliseconds(50));
        Assert(Math.Abs(Bounds(notes).Width - committedLeft.Width) < 1 &&
            Math.Abs(Bounds(tasks).Width - committedRight.Width) < 1 && Bounds(tasks).Top == committedRight.Top,
            "Canceling a shared resize restores both cards without moving them");
        var settings = new SettingsDialog(window, store, window.state, window, "Appearance"); settings.Show(); Pump(TimeSpan.FromMilliseconds(80));
        var palette = Descendants(settings).OfType<ComboBox>().Single(c => c.Items.Contains(AppTheme.Ember));
        palette.SelectedItem = AppTheme.Ember; Pump(TimeSpan.FromMilliseconds(50));
        Assert(((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color ==
            (Color)ColorConverter.ConvertFromString(Theme.AppPalette(AppTheme.Ember).Accent),
            "Changing the global theme applies the chosen palette immediately in a Space");
        settings.FlushPendingChanges();
        Assert(store.LoadAppTheme() == AppTheme.Ember, "Selected global theme is saved");
        settings.Close(); window.Close();
        var reopened = new MainWindow(store) { Width = 1450, Height = 950 }; reopened.Show(); Pump(TimeSpan.FromMilliseconds(160));
        Assert(reopened.state.Widgets.Single(w => w.Id == pixel.Id).BannerSpan == store.Load().Widgets.Single(w => w.Id == pixel.Id).BannerSpan &&
            reopened.state.Widgets.Single(w => w.Id == notes.Id).Span == store.Load().Widgets.Single(w => w.Id == notes.Id).Span,
            "Saved shared layout reloads with existing Space data");
        reopened.Close();
    }
}
