using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckBible()
    {
        var translations = new List<string>();
        foreach (var name in BibleLibrary.Translations)
        {
            var bible = BibleLibrary.Load(name);
            Assert(bible.Translation == name && bible.Source == name + ".pdf" && bible.SourceSha256.Length == 64, name + " uses its own fingerprinted source PDF");
            Assert(bible.Verses.Select(v => v.Book).Distinct().SequenceEqual(BibleLibrary.Books) &&
                BibleLibrary.Books.Sum(b => bible.Chapters(b).Length) == 1189, name + " indexes all 66 books and 1,189 chapters");
            Assert(bible.Verses.All(v => !string.IsNullOrWhiteSpace(v.Text)) &&
                bible.Verses.Select(v => (v.Book, v.Chapter, v.Verse)).Distinct().Count() == bible.Verses.Count, name + " has nonempty, uniquely addressed verses");
            var john = bible.Search("John 3:16").Single(); translations.Add(john.Text);
            Assert(john.Book == "John" && john.Chapter == 3 && john.Verse == 16 && john.Text.Length > 100, name + " resolves John 3:16");
            Assert(bible.Search("Romans 8").Count() >= 38 && bible.Search("Psalm 23").Count() == 6 && bible.Search("John 3:16-18").Count() == 3, name + " resolves chapters, Psalm aliases and verse ranges");
            Assert(new[] { "love", "faith", "grace" }.All(q => bible.Search(q).Any()), name + " searches words throughout the supplied Bible");
            Assert(bible.Search("John 999:999").Count() == 0, name + " never substitutes text for a missing passage");
            Assert(bible.Chapter("Revelation",22).Last().Text.Length < 120 && bible.Verses.All(v => v.Text.Length < 1500), name + " excludes appendices and reference tables from verse text");
            if (name == "NKJV") Assert(bible.Search("Psalm 23:1").Single().Text == "THE LORD is my shepherd; I shall not want.", "NKJV Psalm text excludes section headings and superscriptions");
        }
        Assert(translations.Distinct().Count() == 3, "Each translation returns its own John 3:16 text");
        var store = new StateStore(Path.Combine(output, "bible-ui.json"));
        var data = DashboardTemplates.Build(DashboardTemplates.All.Single(t => t.Name == "Church"));
        var readerWidget = data.Widgets.Single(w => w.Kind == "Bible");
        readerWidget.BibleLocation = new() { Translation = "NLT", Book = "John", Chapter = 3, Verse = 16 };
        var pins = data.Widgets.Single(w => w.Kind == "Scripture pins");
        pins.Items.Add(new() { Text = "Psalm 23 — existing personal reflection" });
        var dashboard = Continuity.NewDashboard("Bible checks", "Church", data, store.UserId);
        store.Save(Continuity.Activate(new() { UserId = store.UserId, Dashboards = [dashboard], ActiveDashboardId = dashboard.DashboardId }, store.UserId));
        var window = new MainWindow(store) { Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual };
        window.Show(); Pump(TimeSpan.FromMilliseconds(600));
        var reader = Descendants(window).OfType<BibleReader>().Single();
        Assert(new TextRange(reader.Passage.Document.ContentStart, reader.Passage.Document.ContentEnd).Text.Contains(translations[0]), "Built-in reader displays real NLT text");
        var paragraph = reader.Passage.Document.Blocks.OfType<Paragraph>().ElementAt(15);
        var run = paragraph.Inlines.OfType<Run>().Last();
        // Wheel events originate from document content, not just visual controls.
        foreach (var source in new ContentElement[] { run, paragraph, reader.Passage.Document })
        {
            reader.Passage.ScrollToHome(); Pump(TimeSpan.FromMilliseconds(40));
            var outerOffset = window.WidgetScroller.VerticalOffset;
            var wheel = new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, -120)
            { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseWheelEvent };
            source.RaiseEvent(wheel); Pump(TimeSpan.FromMilliseconds(40));
            Assert(wheel.Handled && reader.Passage.VerticalOffset > 0 && window.WidgetScroller.VerticalOffset == outerOffset,
                "Mouse wheel over " + source.GetType().Name + " scrolls the Bible without crashing or moving the dashboard");
        }
        reader.Passage.Selection.Select(run.ContentStart, run.ContentStart.GetPositionAtOffset(35)!);
        var pin = reader.SelectedPin()!;
        Assert(pin.Reference == "John 3:16" && pin.Translation == "NLT" && pin.Text == run.Text[..35], "A partial mouse selection preserves reference, translation and exact text");
        var nextRun = reader.Passage.Document.Blocks.OfType<Paragraph>().ElementAt(17).Inlines.OfType<Run>().Last();
        reader.Passage.Selection.Select(run.ContentStart, nextRun.ContentEnd);
        Assert(reader.SelectedPin()!.EndVerse == 18 && !reader.SelectedPin()!.Text.Contains("17  "), "Multi-verse selection stores a range without UI verse-number labels");
        reader.Passage.Selection.Select(run.ContentStart, run.ContentEnd);
        var actualPins = window.state.Widgets.Single(w => w.Kind == "Scripture pins");
        Click(window, "Add to Scripture Pins"); Pump(TimeSpan.FromMilliseconds(100));
        Assert(actualPins.ScripturePins.Count == 1 && actualPins.ScripturePins[0].Text == translations[0] && actualPins.Items.Count == 1, "Selection-to-pin preserves legacy notes and adds source text");
        Assert(ReferenceEquals(reader, Descendants(window).OfType<BibleReader>().Single()), "Pinning keeps the reader's selection and scroll position");
        actualPins.ScripturePins[0].Note = "A saved reflection"; window.SaveNow();
        var reopened = store.Load(); var savedPin = reopened.Widgets.Single(w => w.Kind == "Scripture pins").ScripturePins.Single();
        Assert(savedPin.Note == "A saved reflection" && savedPin.Translation == "NLT" && savedPin.Text == translations[0], "Pin fields round-trip through dashboard persistence");
        reader.Translation.SelectedItem = "CSB"; Pump(TimeSpan.FromMilliseconds(400));
        Assert(new TextRange(reader.Passage.Document.ContentStart, reader.Passage.Document.ContentEnd).Text.Contains(translations[1]) && savedPin.Translation == "NLT", "Changing reader translation preserves a saved pin's original translation");
        foreach (var translation in BibleLibrary.Translations)
        {
            reader.Translation.SelectedItem = translation;
            var loadingWheel = new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, -120)
            { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseWheelEvent };
            reader.Passage.Document.RaiseEvent(loadingWheel);
            Pump(TimeSpan.FromMilliseconds(400));
            var verseText = reader.Passage.Document.Blocks.OfType<Paragraph>().ElementAt(15).Inlines.OfType<Run>().Last();
            verseText.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, -120)
            { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseWheelEvent });
            Assert(verseText.Text == translations[Array.IndexOf(BibleLibrary.Translations, translation)],
                "Switching to " + translation + " and scrolling during and after loading keeps the correct Bible text");
        }
        for (var switchIndex = 0; switchIndex < 18; switchIndex++)
            reader.Translation.SelectedItem = BibleLibrary.Translations[switchIndex % 3];
        reader.Translation.SelectedItem = "CSB"; Pump(TimeSpan.FromMilliseconds(600));
        Assert(new TextRange(reader.Passage.Document.ContentStart, reader.Passage.Document.ContentEnd).Text.Contains(translations[1]),
            "Rapid translation changes keep the final selection and discard earlier load completions");
        reader.SearchBox.Text = "Romans 8"; reader.Search();
        Assert(Descendants(reader).OfType<TextBlock>().Any(t => t.Text == "Romans 8:1"), "Search results display references and verse previews");
        var result = Descendants(reader).OfType<Button>().First(b => b.Content is StackPanel p && p.Children.OfType<TextBlock>().FirstOrDefault()?.Text == "Romans 8:1");
        result.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert((string)reader.Book.SelectedItem == "Romans" && (int)reader.Chapter.SelectedItem == 8, "Clicking a search result opens its chapter inside Nexa");
        // Exercise the actual drag destination event with the same format used by the reader.
        var target = Descendants(window).OfType<StackPanel>().Single(p => Equals(p.Tag, "ScriptureDropTarget"));
        var args = (DragEventArgs)Activator.CreateInstance(typeof(DragEventArgs), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, [new DataObject(BibleReader.PinFormat, pin), DragDropKeyStates.LeftMouseButton, DragDropEffects.Copy, target, new Point(10, 10)], null)!;
        args.RoutedEvent = DragDrop.DropEvent;
        target.RaiseEvent(args);
        Assert(actualPins.ScripturePins.Count == 2 && actualPins.ScripturePins.Select(p => p.Id).Distinct().Count() == 2, "Dropping selected Scripture adds a new pin with its translation");
        window.Width = 960; window.Height = 660; Pump(TimeSpan.FromMilliseconds(150));
        RenderPreview((FrameworkElement)window.Content, "bible-small");
        window.Width = 1220; window.Height = 880; Pump(TimeSpan.FromMilliseconds(150));
        RenderPreview((FrameworkElement)window.Content, "bible-reader");
        window.WidgetScroller.ScrollToVerticalOffset(actualPins.Row * 24); Pump(TimeSpan.FromMilliseconds(100));
        RenderPreview((FrameworkElement)window.Content, "scripture-pins");
        var copy = window.CopyWorkspaceState(); var record = Continuity.Current(copy.Workspace!, store.UserId);
        Assert(!Continuity.ReadExport(Continuity.Export(record, false)).Data.Widgets.Any(w => w.ScripturePins.Count > 0), "Configuration-only export excludes private pins");
        Assert(Continuity.ReadExport(Continuity.Export(record, true)).Data.Widgets.Single(w => w.Kind == "Scripture pins").ScripturePins.Count == 2, "Private-data export preserves structured pins");
        var historicalPin = record.Current.Data.Widgets.Single(w => w.Kind == "Scripture pins").ScripturePins[0];
        Continuity.Reset(record);
        Assert(record.Current.Data.Widgets.All(w => w.ScripturePins.Count == 0) && record.History.Last().Data.Widgets.Any(w => w.ScripturePins.Any(p => p.Id == historicalPin.Id)), "Explicit reset archives pins before clearing current-session content");
        Assert(MainWindow.PreviewSession(record.History.Last()).Contains("John 3:16 · NLT") && MainWindow.PreviewSession(record.History.Last()).Contains("A saved reflection"), "Read-only session history displays pin text, translation and notes");
        Continuity.Merge(record, record.History.Last().SessionId, store.UserId, false);
        Continuity.Merge(record, record.History.Last().SessionId, store.UserId, false);
        Assert(record.Current.Data.Widgets.Single(w => w.Kind == "Scripture pins").ScripturePins.Count == 2, "History merge restores pins without duplicate imports");
        window.Close();
    }
}
