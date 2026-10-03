using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace SessionDashboard;

// One chapter is materialized at a time. The shared, immutable index serves all readers.
internal sealed class BibleReader : Grid
{
    internal const string PinFormat = "Nexa.ScripturePin";
    private readonly BibleReaderState location;
    private readonly Action changed;
    private readonly Action<ScripturePin> pin;
    private readonly Action<string> openWeb;
    internal readonly ComboBox Translation = new() { Width = 80 };
    internal readonly ComboBox Book = new() { Width = 156 };
    internal readonly ComboBox Chapter = new() { Width = 62 };
    internal readonly ComboBox Verse = new() { Width = 62 };
    internal readonly TextBox SearchBox = new() { ToolTip = "Search a reference or words (John 3:16, Romans 8, love)" };
    internal readonly RichTextBox Passage = new() { IsReadOnly = true, IsDocumentEnabled = true, IsUndoEnabled = false,
        VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        BorderThickness = new Thickness(0), Padding = new Thickness(4, 8, 4, 8), Background = Brushes.Transparent,
        SelectionBrush = new SolidColorBrush(Color.FromArgb(130, 115, 154, 220)), IsInactiveSelectionHighlightEnabled = true };
    private readonly StackPanel results = new();
    private readonly ScrollViewer resultsScroll;
    private readonly TextBlock status = new() { FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
    private readonly Button add = new() { Content = "Add to Scripture Pins", IsEnabled = false, Focusable = false, Padding = new Thickness(10, 5, 10, 5) };
    private readonly List<(BibleVerse Verse, Run Text)> verseRuns = [];
    private BibleEdition? edition;
    private bool updating;
    private int request;
    private Point? dragStart;
    internal BibleReader(BibleReaderState location, double height, Action changed, Action<ScripturePin> pin, Action<string> openWeb)
    {
        this.location = location; this.changed = changed; this.pin = pin; this.openWeb = openWeb;
        Height = Math.Max(420, height);
        RowDefinitions.Add(new() { Height = GridLength.Auto });
        RowDefinitions.Add(new() { Height = GridLength.Auto });
        RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        RowDefinitions.Add(new() { Height = GridLength.Auto });
        var selectors = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        void SelectControl(string label, ComboBox combo)
        {
            var stack = new StackPanel { Margin = new Thickness(0, 0, 6, 6) };
            var caption = new TextBlock { Text = label, FontSize = 11, Margin = new Thickness(0, 0, 0, 4) };
            caption.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
            System.Windows.Automation.AutomationProperties.SetName(combo, label);
            combo.ToolTip = label; stack.Children.Add(caption); stack.Children.Add(combo); selectors.Children.Add(stack);
        }
        SelectControl("Translation", Translation); SelectControl("Book", Book); SelectControl("Chapter", Chapter); SelectControl("Verse", Verse);
        Children.Add(selectors);
        var searchRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        var search = new Button { Content = "Search", Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(6, 0, 0, 0) };
        DockPanel.SetDock(search, Dock.Right); searchRow.Children.Add(search); searchRow.Children.Add(SearchBox);
        System.Windows.Automation.AutomationProperties.SetName(SearchBox, "Search Bible");
        SetRow(searchRow, 1); Children.Add(searchRow);
        SetRow(Passage, 2); Children.Add(Passage);
        Passage.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        resultsScroll = new ScrollViewer { Content = results, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Visibility = Visibility.Collapsed };
        SetRow(resultsScroll, 2); Children.Add(resultsScroll);
        var bottom = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        var actions = new WrapPanel();
        foreach (var (label, direction) in new[] { ("‹", -1), ("›", 1) })
        {
            var button = new Button { Content = label, ToolTip = direction < 0 ? "Previous chapter" : "Next chapter", Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 0, 5, 4) };
            button.Click += (_, _) => MoveChapter(direction); actions.Children.Add(button);
        }
        actions.Children.Add(add);
        var online = new Button { Content = "↗", ToolTip = "Open passage online", Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(8, 0, 0, 4), FontSize = 13 };
        System.Windows.Automation.AutomationProperties.SetName(online, "Open passage online");
        online.Click += (_, _) => openWeb("https://www.biblegateway.com/passage/?search=" + Uri.EscapeDataString($"{location.Book} {location.Chapter}:{location.Verse}") + "&version=" + location.Translation);
        actions.Children.Add(online); bottom.Children.Add(actions); bottom.Children.Add(status);
        status.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); SetRow(bottom, 3); Children.Add(bottom);
        Translation.ItemsSource = BibleLibrary.Translations; Book.ItemsSource = BibleLibrary.Books;
        updating = true; Translation.SelectedItem = location.Translation; Book.SelectedItem = location.Book; updating = false;
        Translation.SelectionChanged += async (_, _) => { if (!updating) { location.Translation = (string)Translation.SelectedItem; changed(); await LoadEdition(); } };
        Book.SelectionChanged += (_, _) => { if (!updating && edition != null) { location.Book = (string)Book.SelectedItem; location.Chapter = location.Verse = 1; ShowChapter(); changed(); } };
        Chapter.SelectionChanged += (_, _) => { if (!updating && Chapter.SelectedItem is int c) { location.Chapter = c; location.Verse = 1; ShowChapter(); changed(); } };
        Verse.SelectionChanged += (_, _) => { if (!updating && Verse.SelectedItem is int v) { location.Verse = v; ScrollToVerse(v); changed(); } };
        search.Click += (_, _) => Search(); SearchBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Search(); e.Handled = true; } };
        add.Click += (_, _) => AddSelection();
        Passage.SelectionChanged += (_, _) => add.IsEnabled = !Passage.Selection.IsEmpty && edition != null;
        var menu = new ContextMenu(); var item = new MenuItem { Header = "Add to Scripture Pins" };
        item.Click += (_, _) => AddSelection(); menu.Items.Add(item);
        var copy = new MenuItem { Header = "Copy", Command = ApplicationCommands.Copy, CommandTarget = Passage }; menu.Items.Add(copy);
        menu.Opened += (_, _) => item.IsEnabled = !Passage.Selection.IsEmpty; Passage.ContextMenu = menu;
        // Begin dragging only when pressing inside an existing selection; ordinary mouse selection remains native.
        Passage.PreviewMouseLeftButtonDown += (_, e) =>
        {
            var p = Passage.GetPositionFromPoint(e.GetPosition(Passage), true);
            if (!Passage.Selection.IsEmpty && p != null && p.CompareTo(Passage.Selection.Start) >= 0 && p.CompareTo(Passage.Selection.End) < 0)
            { dragStart = e.GetPosition(Passage); e.Handled = true; }
        };
        Passage.PreviewMouseMove += (_, e) =>
        {
            if (e.LeftButton != MouseButtonState.Pressed) { dragStart = null; return; }
            if (dragStart is not { } start || (e.GetPosition(Passage) - start).Length < 6) return;
            dragStart = null;
            if (SelectedPin() is { } selected) DragDrop.DoDragDrop(Passage, new DataObject(PinFormat, selected), DragDropEffects.Copy);
        };
        Loaded += async (_, _) => { if (edition == null) await LoadEdition(); };
    }
    private async Task LoadEdition()
    {
        var ticket = ++request; var translation = location.Translation;
        edition = null; add.IsEnabled = false; status.Text = $"Loading {translation}…";
        Passage.Document = new FlowDocument(); resultsScroll.Visibility = Visibility.Collapsed; Passage.Visibility = Visibility.Visible;
        try
        {
            var loaded = await Task.Run(() => BibleLibrary.Load(translation));
            await Dispatcher.InvokeAsync(() => { if (ticket == request) { edition = loaded; ShowChapter(); } });
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or InvalidOperationException)
        { await Dispatcher.InvokeAsync(() => { if (ticket == request) status.Text = $"{translation} could not be loaded. {ex.Message}"; }); }
    }
    internal void ShowChapter()
    {
        if (edition == null) return;
        updating = true;
        Book.SelectedItem = location.Book;
        var chapters = edition.Chapters(location.Book); Chapter.ItemsSource = chapters;
        if (!chapters.Contains(location.Chapter)) location.Chapter = chapters.FirstOrDefault(1);
        Chapter.SelectedItem = location.Chapter;
        var verses = edition.Chapter(location.Book, location.Chapter); Verse.ItemsSource = verses.Select(v => v.Verse).ToArray();
        if (!verses.Any(v => v.Verse == location.Verse)) location.Verse = verses.FirstOrDefault()?.Verse ?? 1;
        Verse.SelectedItem = location.Verse; updating = false;
        var doc = new FlowDocument { PagePadding = new Thickness(0), FontFamily = new FontFamily("Segoe UI"), FontSize = 16, LineHeight = 25 };
        verseRuns.Clear();
        foreach (var verse in verses)
        {
            var text = new Run(verse.Text);
            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
            var number = new Run(verse.Verse + "  ") { FontSize = 11, FontWeight = FontWeights.SemiBold };
            number.SetResourceReference(TextElement.ForegroundProperty, "AccentBrush");
            paragraph.Inlines.Add(number); paragraph.Inlines.Add(text); doc.Blocks.Add(paragraph); verseRuns.Add((verse, text));
        }
        Passage.Document = doc;
        resultsScroll.Visibility = Visibility.Collapsed; Passage.Visibility = Visibility.Visible;
        status.Text = verses.Length == 0 ? "This chapter is unavailable in the supplied file." : $"{location.Book} {location.Chapter} · {edition.Translation} · Select text to pin · Source: {edition.Source}";
        Dispatcher.BeginInvoke(new Action(() => ScrollToVerse(location.Verse)));
    }
    private void ScrollToVerse(int verse) => verseRuns.FirstOrDefault(v => v.Verse.Verse == verse).Text?.BringIntoView();
    private void MoveChapter(int direction)
    {
        if (edition == null) return;
        var chapters = edition.Chapters(location.Book); var index = Array.IndexOf(chapters, location.Chapter) + direction;
        if (index >= 0 && index < chapters.Length) location.Chapter = chapters[index];
        else
        {
            var book = Array.IndexOf(BibleLibrary.Books, location.Book) + direction;
            if (book < 0 || book >= BibleLibrary.Books.Length) return;
            location.Book = BibleLibrary.Books[book]; chapters = edition.Chapters(location.Book);
            location.Chapter = direction > 0 ? chapters.First() : chapters.Last();
        }
        location.Verse = 1; ShowChapter(); changed();
    }
    internal void Search()
    {
        if (edition == null) return;
        var hits = edition.Search(SearchBox.Text).ToArray(); results.Children.Clear();
        Passage.Visibility = Visibility.Collapsed; resultsScroll.Visibility = Visibility.Visible;
        status.Text = $"{hits.Length:N0} results · {edition.Translation}";
        var back = new Button { Content = "Back to passage", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8) };
        back.Click += (_, _) => ShowChapter(); results.Children.Add(back);
        var shown = 0;
        var more = new Button { Content = "Show more results", Margin = new Thickness(0, 8, 0, 0) };
        void Batch()
        {
            results.Children.Remove(more);
            foreach (var hit in hits.Skip(shown).Take(50))
            {
                var content = new StackPanel(); content.Children.Add(new TextBlock { Text = hit.Reference, FontWeight = FontWeights.SemiBold });
                content.Children.Add(new TextBlock { Text = hit.Text.Length > 210 ? hit.Text[..210] + "…" : hit.Text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
                var result = new Button { Content = content, HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(10) };
                result.Click += (_, _) => { location.Book = hit.Book; location.Chapter = hit.Chapter; location.Verse = hit.Verse; ShowChapter(); changed(); };
                results.Children.Add(result);
            }
            shown += 50; if (shown < hits.Length) results.Children.Add(more);
        }
        more.Click += (_, _) => Batch(); Batch();
    }
    internal ScripturePin? SelectedPin()
    {
        if (edition == null || Passage.Selection.IsEmpty) return null;
        var selected = new List<(BibleVerse Verse, string Text)>();
        foreach (var (verse, run) in verseRuns)
        {
            var start = Passage.Selection.Start.CompareTo(run.ContentStart) > 0 ? Passage.Selection.Start : run.ContentStart;
            var end = Passage.Selection.End.CompareTo(run.ContentEnd) < 0 ? Passage.Selection.End : run.ContentEnd;
            if (start.CompareTo(end) >= 0) continue;
            var text = new TextRange(start, end).Text;
            if (!string.IsNullOrWhiteSpace(text)) selected.Add((verse, text));
        }
        if (selected.Count == 0) return null;
        return new ScripturePin { Book = selected[0].Verse.Book, Chapter = selected[0].Verse.Chapter,
            Verse = selected[0].Verse.Verse, EndVerse = selected[^1].Verse.Verse, Translation = edition.Translation,
            Text = string.Join("\n", selected.Select(s => s.Text)).Trim() };
    }
    private void AddSelection()
    {
        if (SelectedPin() is not { } selected) return;
        pin(selected); status.Text = $"Pinned {selected.Reference} · {selected.Translation}";
    }
}
