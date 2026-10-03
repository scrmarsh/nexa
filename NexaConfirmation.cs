using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

internal sealed class NexaConfirmation : NexaPanel
{
    internal NexaConfirmation(Window owner, string title, string message, string confirmLabel)
    {
        Owner = owner; Title = title; Width = 520; Height = 255; MinWidth = 380; MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ConfirmLabel = confirmLabel;
        var body = new StackPanel { Margin = new Thickness(28, 28, 28, 18) };
        body.Children.Add(new TextBlock { Text = title, FontSize = 24, FontWeight = FontWeights.SemiBold });
        var copy = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 16, 0, 0) };
        copy.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); body.Children.Add(copy);
        Content = body;
    }

    internal string ConfirmLabel { get; }
    internal void Cancel() => CloseWithFade(false);
    internal void Confirm() => CloseWithFade(true);

    internal static bool ClearAllHistory(Window owner) =>
        new NexaConfirmation(owner, "Clear all history?", "This will permanently delete all session history. This action cannot be undone.", "Clear History").ShowDialog() == true;
}
