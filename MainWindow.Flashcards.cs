using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace SessionDashboard;

public partial class MainWindow
{
    private void BuildFlashcards(StackPanel panel, WidgetInstance widget)
    {
        widget.FlashcardDecks ??= [];
        var deckRow = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        var create = new Button { Content = "New deck", Padding = new Thickness(9, 6, 9, 6), Margin = new Thickness(8, 0, 0, 0) }; DockPanel.SetDock(create, Dock.Right); deckRow.Children.Add(create);
        var import = new Button { Content = "Import", Padding = new Thickness(9, 6, 9, 6), Margin = new Thickness(8, 0, 0, 0) }; DockPanel.SetDock(import, Dock.Right); deckRow.Children.Add(import);
        var decks = new ComboBox { ItemsSource = widget.FlashcardDecks, DisplayMemberPath = "Name", MinWidth = 120, SelectedItem = widget.FlashcardDecks.FirstOrDefault(deck => deck.Id == widget.ActiveFlashcardDeckId) ?? widget.FlashcardDecks.FirstOrDefault() }; deckRow.Children.Add(decks); panel.Children.Add(deckRow);
        var cardFace = new TextBlock { TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, FontSize = 22, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(18) };
        var card = new Border { Child = cardFace, MinHeight = 150, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Cursor = Cursors.Hand }; card.SetResourceReference(Border.BackgroundProperty, "DashboardSurfaceBrush"); card.SetResourceReference(Border.BorderBrushProperty, "DashboardLineBrush"); panel.Children.Add(card);
        var progress = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, Foreground = DashboardBrush("MutedBrush"), Margin = new Thickness(0, 9, 0, 9) }; panel.Children.Add(progress);
        var navigation = new UniformGrid { Columns = 3 }; var previous = new Button { Content = "Previous", Margin = new Thickness(0, 0, 5, 0) }; var flip = new Button { Content = "Flip", Margin = new Thickness(5, 0, 5, 0), Style = (Style)FindResource("PrimaryButton") }; var next = new Button { Content = "Next", Margin = new Thickness(5, 0, 0, 0) }; navigation.Children.Add(previous); navigation.Children.Add(flip); navigation.Children.Add(next); panel.Children.Add(navigation);
        var study = new UniformGrid { Columns = 2, Margin = new Thickness(0, 9, 0, 0) }; var know = new Button { Content = "Know it", Margin = new Thickness(0, 0, 5, 0) }; var review = new Button { Content = "Review", Margin = new Thickness(5, 0, 0, 0) }; study.Children.Add(know); study.Children.Add(review); panel.Children.Add(study);
        var management = new StackPanel { Margin = new Thickness(0, 10, 0, 0) }; var cardActions = new UniformGrid { Columns = 3 }; var add = new Button { Content = "Add card", Margin = new Thickness(0, 0, 4, 0) }; var edit = new Button { Content = "Edit card", Margin = new Thickness(4, 0, 4, 0) }; var delete = new Button { Content = "Delete card", Margin = new Thickness(4, 0, 0, 0) }; cardActions.Children.Add(add); cardActions.Children.Add(edit); cardActions.Children.Add(delete); management.Children.Add(cardActions);
        var shuffle = new Button { Content = "Shuffle deck", Margin = new Thickness(0, 8, 0, 0) }; management.Children.Add(shuffle); panel.Children.Add(management);
        var showingBack = false;
        FlashcardDeck? Deck() => decks.SelectedItem as FlashcardDeck;
        Flashcard? Current() { var deck = Deck(); return deck == null || deck.Cards.Count == 0 ? null : deck.Cards[Math.Clamp(widget.FlashcardIndex, 0, deck.Cards.Count - 1)]; }
        void Refresh()
        {
            var deck = Deck(); widget.ActiveFlashcardDeckId = deck?.Id; var current = Current();
            cardFace.Text = current == null ? "Create or import a deck to begin." : showingBack ? current.Back : current.Front;
            progress.Text = current == null ? "0 / 0" : $"{widget.FlashcardIndex + 1} / {deck!.Cards.Count} · {current.StudyState}";
            foreach (var button in new[] { previous, flip, next, know, review, edit, delete, shuffle }) button.IsEnabled = current != null;
        }
        void Move(int delta) { var deck = Deck(); if (deck == null || deck.Cards.Count == 0) return; widget.FlashcardIndex = (widget.FlashcardIndex + delta + deck.Cards.Count) % deck.Cards.Count; showingBack = false; Changed(); Refresh(); }
        previous.Click += (_, _) => Move(-1); next.Click += (_, _) => Move(1); flip.Click += (_, _) => { showingBack = !showingBack; Refresh(); }; card.MouseLeftButtonUp += (_, _) => { showingBack = !showingBack; Refresh(); };
        know.Click += (_, _) => { if (Current() is { } value) { value.StudyState = "Know it"; Changed(); SaveNow(); Move(1); } }; review.Click += (_, _) => { if (Current() is { } value) { value.StudyState = "Review"; Changed(); SaveNow(); Move(1); } };
        decks.SelectionChanged += (_, _) => { widget.FlashcardIndex = 0; showingBack = false; Changed(); Refresh(); };
        create.Click += (_, _) => EditDeckName(null);
        add.Click += (_, _) => EditCard(null);
        edit.Click += (_, _) => { if (Current() is { } value) EditCard(value); };
        delete.Click += (_, _) => { if (Deck() is not { } deck || Current() is not { } value) return; deck.Cards.Remove(value); widget.FlashcardIndex = Math.Max(0, widget.FlashcardIndex - 1); Changed(); SaveNow(); Refresh(); };
        shuffle.Click += (_, _) => { if (Deck() is not { } deck) return; deck.Cards = deck.Cards.OrderBy(_ => Random.Shared.Next()).ToList(); widget.FlashcardIndex = 0; showingBack = false; Changed(); SaveNow(); Refresh(); };
        import.Click += (_, _) => ImportDeck();
        panel.SizeChanged += (_, e) => { study.Visibility = e.NewSize.Width >= 300 && e.NewSize.Height >= 280 ? Visibility.Visible : Visibility.Collapsed; management.Visibility = e.NewSize.Width >= 460 && e.NewSize.Height >= 390 ? Visibility.Visible : Visibility.Collapsed; };
        Refresh();

        void EditDeckName(string? initial)
        {
            var dialog = Dialog("Nexa · Flashcard deck", 420, 220); var body = new StackPanel { Margin = new Thickness(22) }; body.Children.Add(new TextBlock { Text = "Deck name", FontSize = 20, FontWeight = FontWeights.SemiBold }); var name = new TextBox { Text = initial ?? "New deck", Margin = new Thickness(0, 12, 0, 16) }; body.Children.Add(name); var save = new Button { Content = "Create deck", Style = (Style)FindResource("PrimaryButton") }; body.Children.Add(save); save.Click += (_, _) => { if (string.IsNullOrWhiteSpace(name.Text)) return; var deck = new FlashcardDeck { Name = name.Text.Trim() }; widget.FlashcardDecks.Add(deck); widget.ActiveFlashcardDeckId = deck.Id; decks.ItemsSource = null; decks.ItemsSource = widget.FlashcardDecks; decks.SelectedItem = deck; Changed(); SaveNow(); dialog.CloseWithFade(); }; dialog.Content = body; dialog.ShowDismissible();
        }
        void EditCard(Flashcard? existing)
        {
            if (Deck() is not { } deck) { EditDeckName(null); return; }
            var dialog = Dialog("Nexa · Flashcard", 520, 430); var body = new StackPanel { Margin = new Thickness(22) }; body.Children.Add(new TextBlock { Text = existing == null ? "Add card" : "Edit card", FontSize = 22, FontWeight = FontWeights.SemiBold });
            var front = new TextBox { Text = existing?.Front ?? "", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 100, Margin = new Thickness(0, 12, 0, 8) }; var back = new TextBox { Text = existing?.Back ?? "", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 100, Margin = new Thickness(0, 0, 0, 14) }; body.Children.Add(front); body.Children.Add(back); var save = new Button { Content = "Save card", Style = (Style)FindResource("PrimaryButton") }; body.Children.Add(save);
            save.Click += (_, _) => { if (string.IsNullOrWhiteSpace(front.Text) || string.IsNullOrWhiteSpace(back.Text)) return; if (existing == null) deck.Cards.Add(new Flashcard { Front = front.Text.Trim(), Back = back.Text.Trim() }); else { existing.Front = front.Text.Trim(); existing.Back = back.Text.Trim(); } Changed(); SaveNow(); Refresh(); dialog.CloseWithFade(); }; dialog.Content = body; dialog.ShowDismissible();
        }
        void ImportDeck()
        {
            var picker = new OpenFileDialog { Filter = "Flashcard decks|*.csv;*.json|CSV|*.csv|JSON|*.json" }; if (picker.ShowDialog(this) != true) return;
            try
            {
                FlashcardDeck deck;
                if (Path.GetExtension(picker.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var json = File.ReadAllText(picker.FileName);
                    using var document = JsonDocument.Parse(json);
                    deck = document.RootElement.ValueKind == JsonValueKind.Array
                        ? new FlashcardDeck { Name = Path.GetFileNameWithoutExtension(picker.FileName), Cards = JsonSerializer.Deserialize<List<Flashcard>>(json) ?? [] }
                        : JsonSerializer.Deserialize<FlashcardDeck>(json) ?? throw new InvalidDataException("The deck file is empty.");
                }
                else
                {
                    deck = new FlashcardDeck { Name = Path.GetFileNameWithoutExtension(picker.FileName) };
                    foreach (var line in File.ReadLines(picker.FileName).Where(line => !string.IsNullOrWhiteSpace(line))) { var values = Csv(line); if (values.Count >= 2 && !(deck.Cards.Count == 0 && values[0].Equals("front", StringComparison.OrdinalIgnoreCase))) deck.Cards.Add(new Flashcard { Front = values[0], Back = values[1] }); }
                }
                if (deck.Cards.Count == 0) throw new InvalidDataException("No cards were found."); deck.Id = Guid.NewGuid(); widget.FlashcardDecks.Add(deck); decks.ItemsSource = null; decks.ItemsSource = widget.FlashcardDecks; decks.SelectedItem = deck; Changed(); SaveNow(); Refresh();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException) { Error("Flashcard import failed: " + ex.Message); }
        }
        static List<string> Csv(string line)
        {
            var values = new List<string>(); var current = new StringBuilder(); var quoted = false;
            for (var i = 0; i < line.Length; i++) { var ch = line[i]; if (ch == '"' && quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; } else if (ch == '"') quoted = !quoted; else if (ch == ',' && !quoted) { values.Add(current.ToString().Trim()); current.Clear(); } else current.Append(ch); }
            values.Add(current.ToString().Trim()); return values;
        }
    }
}
