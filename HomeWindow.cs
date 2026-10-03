using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace SessionDashboard;

internal sealed class HomeWindow : NexaDialog
{
    private readonly StateStore store;
    internal readonly AppState Saved;
    internal readonly StackPanel Recent = new();
    internal readonly Button ContinueButton = new() { Content = "Continue", Padding = new Thickness(24, 20, 24, 20), Margin = new Thickness(0, 0, 12, 0) };
    private readonly TextBlock feedback = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 20, 0, 0) };
    internal HomeWindow(StateStore store)
    {
        this.store = store; Saved = store.Load();
        if (Saved.Workspace == null && File.Exists(store.FilePath)) Saved.Workspace = Continuity.Migrate(Saved, store.UserId);
        Title = "Nexa"; Width = 1080; Height = 780; MinWidth = 740; MinHeight = 550; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var panel = new StackPanel { Margin = new Thickness(60, 36, 60, 40), MaxWidth = 960 };
        panel.Children.Add(new NexaLogo { Height = 105, Width = 334, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(-12, 0, 0, 28) });
        panel.Children.Add(new TextBlock { Text = "Welcome back", FontSize = 36, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "A space for whatever comes next.", FontSize = 16, Foreground = (Brush)FindResource("MutedBrush"), Margin = new Thickness(0, 10, 0, 8) });
        panel.Children.Add(new TextBlock { Text = "Web app version coming soon — the desktop build remains the current release.", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("AccentBrush"), Margin = new Thickness(0, 0, 0, 22) });
        var actions = new System.Windows.Controls.Primitives.UniformGrid { Columns = 3 };
        var create = new Button { Content = "New dashboard", Padding = new Thickness(24, 20, 24, 20), Margin = new Thickness(0, 0, 12, 0), Style = (Style)FindResource("PrimaryButton") };
        create.Click += (_, _) => NewDashboard(); actions.Children.Add(create);
        ContinueButton.IsEnabled = Saved.Workspace?.Dashboards.Count > 0;
        ContinueButton.Click += (_, _) => { if (Saved.Workspace != null) OpenDashboard(Saved.Workspace.Dashboards.OrderByDescending(d => d.Current.LastAccessedAt).First().DashboardId); }; actions.Children.Add(ContinueButton);
        var import = new Button { Content = "Import dashboard", Padding = new Thickness(24, 20, 24, 20) }; import.Click += (_, _) => ImportDashboard(); actions.Children.Add(import); panel.Children.Add(actions);
        panel.Children.Add(new TextBlock { Text = "Recent dashboards", FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 38, 0, 16) });
        foreach (var d in (Saved.Workspace?.Dashboards ?? []).Where(d => d.PresetOrigin != "Developer mode").OrderByDescending(d => d.Current.LastAccessedAt))
        {
            var content = new StackPanel(); content.Children.Add(new TextBlock { Text = d.Name, FontSize = 17, FontWeight = FontWeights.SemiBold });
            content.Children.Add(new TextBlock { Text = $"{d.PresetOrigin} · Last opened {d.Current.LastAccessedAt.LocalDateTime:g}", Foreground = (Brush)FindResource("MutedBrush"), Margin = new Thickness(0, 7, 0, 0) });
            var button = new Button { Content = content, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 10) };
            button.Click += (_, _) => OpenDashboard(d.DashboardId); Recent.Children.Add(button);
        }
        if (Recent.Children.Count == 0) Recent.Children.Add(new TextBlock { Text = "Your first workspace starts here. Choose a preset or a blank canvas.", TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("MutedBrush") });
        panel.Children.Add(Recent); feedback.Text = store.Warning ?? "Saved privately on this Windows account."; panel.Children.Add(feedback);
        var about = new Button { Content = "About Nexa", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 20, 0, 0) };
        about.Click += (_, _) => MessageBox.Show(this, "Nexa 1.3\nYour workspace, your way.\n\nLocal autosave · Session history · Flexible dashboards\nYour data stays on your Windows account.", "About Nexa"); panel.Children.Add(about);
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
    internal void OpenDashboard(Guid id)
    {
        try
        {
            var workspace = Continuity.Clone(Saved.Workspace!); workspace.ActiveDashboardId = id;
            Continuity.Access(Continuity.Current(workspace, store.UserId));
            var next = Continuity.Activate(workspace, store.UserId, Saved.HedyRegion, Saved.HedyProtectedKey); store.Save(next);
            ShowDashboard();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException) { feedback.Text = "Could not open dashboard: " + ex.Message; }
    }
    private void ShowDashboard()
    {
        var window = new MainWindow(store); Application.Current.MainWindow = window; window.Show(); Close();
    }
    private void NewDashboard()
    {
        var dialog = new DashboardCreation(this, store.UserId);
        if (dialog.ShowDialog() != true || dialog.Result == null) return;
        try
        {
            var workspace = Saved.Workspace == null ? new WorkspaceDocument { UserId = store.UserId } : Continuity.Clone(Saved.Workspace);
            workspace.Dashboards.Add(dialog.Result); workspace.ActiveDashboardId = dialog.Result.DashboardId;
            store.Save(Continuity.Activate(workspace, store.UserId, Saved.HedyRegion, Saved.HedyProtectedKey)); ShowDashboard();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { feedback.Text = "Could not create dashboard: " + ex.Message; }
    }
    private void ImportDashboard()
    {
        var picker = new OpenFileDialog { Filter = "Nexa dashboard (*.nexa)|*.nexa" }; if (picker.ShowDialog(this) != true) return;
        try
        {
            if (new FileInfo(picker.FileName).Length > 10_000_000) throw new InvalidDataException("Dashboard file exceeds the 10 MB limit.");
            var file = Continuity.ReadExport(File.ReadAllText(picker.FileName));
            if (file.IncludesSessionData && MessageBox.Show(this, "This file includes private session data. Import that content into your Windows account?", "Import dashboard", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
            var workspace = Saved.Workspace == null ? new WorkspaceDocument { UserId = store.UserId } : Continuity.Clone(Saved.Workspace);
            if (workspace.Dashboards.Count == 0)
            {
                var record = Continuity.NewDashboard(file.DashboardName, file.PresetOrigin, file.Data, store.UserId);
                foreach (var widget in record.Current.Data.Widgets) widget.Id = Guid.NewGuid();
                foreach (var preset in record.Current.Data.Presets) preset.Id = Guid.NewGuid();
                workspace.Dashboards.Add(record); workspace.ActiveDashboardId = record.DashboardId;
            }
            else Continuity.Import(workspace, file, store.UserId, file.IncludesSessionData);
            store.Save(Continuity.Activate(workspace, store.UserId, Saved.HedyRegion, Saved.HedyProtectedKey)); ShowDashboard();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidOperationException) { feedback.Text = "Import failed: " + ex.Message; }
    }
}
