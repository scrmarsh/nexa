using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SessionDashboard;

public partial class MainWindow
{
    private UIElement BuildBible(WidgetInstance widget)
    {
        if (widget.BibleLocation == null)
        {
            widget.BibleLocation = new();
            if (BibleLibrary.TryReference(widget.Text, out var book, out var chapter, out var verse, out _))
            { widget.BibleLocation.Book = book; widget.BibleLocation.Chapter = chapter; widget.BibleLocation.Verse = Math.Max(1, verse); }
        }
        return new BibleReader(widget.BibleLocation, Math.Max(440, widget.Height - 85), () => Changed(), pin => AddScripturePin(pin), OpenWeb);
    }
    internal void AddScripturePin(ScripturePin pin, WidgetInstance? target = null)
    {
        target ??= state.Widgets.FirstOrDefault(w => w.Kind == "Scripture pins");
        if (target == null)
        {
            if (state.Widgets.Count >= 200) { Error("Add a Scripture Pins widget after removing an unused widget."); return; }
            target = WidgetLibrary.Create("Scripture pins"); WidgetLibrary.Add(state, target);
        }
        if (target.ScripturePins.Count >= 10000) { Error("This widget has reached its pin limit."); return; }
        var copy = Continuity.Clone(pin); copy.Id = Guid.NewGuid(); target.ScripturePins.Add(copy);
        Changed(); SaveNow();
        // Refresh just the destination card so the reader keeps its selection and scroll position.
        if (widgetFrames.TryGetValue(target.Id, out var frame) && !target.IsMicro && frame.Child is StackPanel layout)
        {
            var content = layout.Children.OfType<ScrollViewer>().FirstOrDefault();
            if (content != null) { content.Content = BuildWidgetContent(target); ArrangeWidgetFrames(); return; }
        }
        RenderWidgets();
    }
    private void BuildScripturePins(StackPanel panel, WidgetInstance widget)
    {
        panel.AllowDrop = true; panel.Tag = "ScriptureDropTarget";
        panel.DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent(BibleReader.PinFormat) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        panel.Drop += (_, e) => { if (e.Data.GetData(BibleReader.PinFormat) is ScripturePin pin) AddScripturePin(pin, widget); e.Handled = true; };
        var hint = new TextBlock { Text = "Select text in the Bible reader, then add or drag it here.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
        hint.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); panel.Children.Add(hint);
        foreach (var pin in widget.ScripturePins)
        {
            var card = new StackPanel();
            var heading = new DockPanel();
            var remove = new Button { Content = "×", Padding = new Thickness(6, 2, 6, 2), ToolTip = "Remove pin" };
            DockPanel.SetDock(remove, Dock.Right); heading.Children.Add(remove);
            remove.Click += (_, _) => { widget.ScripturePins.Remove(pin); Changed(); RenderWidgets(); };
            var open = new Button { Content = pin.Reference, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(6, 3, 6, 3), ToolTip = "Open in Bible reader" };
            open.Click += (_, _) => OpenScripture(pin); heading.Children.Add(open); card.Children.Add(heading);
            var permanent = new CheckBox { Content = "Permanent bookmark", IsChecked = pin.Permanent, Margin = new Thickness(0, 8, 0, 0) };
            permanent.Click += (_, _) => { pin.Permanent = permanent.IsChecked == true; Changed(); SaveNow(); }; card.Children.Add(permanent);
            var version = new TextBlock { Text = pin.Translation, FontSize = 11, Margin = new Thickness(0, 7, 0, 5), FontWeight = FontWeights.SemiBold };
            version.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush"); card.Children.Add(version);
            card.Children.Add(new TextBox { Text = pin.Text, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, BorderThickness = new Thickness(0), Background = Brushes.Transparent, Padding = new Thickness(0), VerticalScrollBarVisibility = ScrollBarVisibility.Disabled });
            card.Children.Add(new TextBlock { Text = "Note (optional)", FontSize = 11, Margin = new Thickness(0, 10, 0, 4) });
            var note = new TextBox { Text = pin.Note, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 42, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
            note.TextChanged += (_, _) => { pin.Note = note.Text; Changed(); }; card.Children.Add(note);
            var border = new Border { Child = card, Padding = new Thickness(12), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 10) };
            border.SetResourceReference(Border.BorderBrushProperty, "LineBrush"); panel.Children.Add(border);
        }
        // Existing free-form pins remain editable, without guessing a translation or fabricating verse text.
        foreach (var legacy in widget.Items)
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            var remove = new Button { Content = "×", Padding = new Thickness(6, 2, 6, 2) }; DockPanel.SetDock(remove, Dock.Right); row.Children.Add(remove);
            remove.Click += (_, _) => { widget.Items.Remove(legacy); Changed(); RenderWidgets(); };
            var text = new TextBox { Text = legacy.Text, TextWrapping = TextWrapping.Wrap, ToolTip = "Legacy reference / note" };
            text.TextChanged += (_, _) => { legacy.Text = text.Text; Changed(); }; row.Children.Add(text); panel.Children.Add(row);
        }
    }
    private void OpenScripture(ScripturePin pin)
    {
        var reader = state.Widgets.FirstOrDefault(w => w.Kind == "Bible");
        if (reader == null)
        {
            if (state.Widgets.Count >= 200) { Error("Remove an unused widget before adding a Bible reader."); return; }
            reader = WidgetLibrary.Create("Bible"); WidgetLibrary.Add(state, reader);
        }
        reader.BibleLocation = new() { Translation = pin.Translation, Book = pin.Book, Chapter = pin.Chapter, Verse = pin.Verse };
        reader.IsMicro = false; Changed(); RenderWidgets();
        Dispatcher.BeginInvoke(new Action(() => { if (widgetFrames.TryGetValue(reader.Id, out var frame)) frame.BringIntoView(); }));
    }
}
