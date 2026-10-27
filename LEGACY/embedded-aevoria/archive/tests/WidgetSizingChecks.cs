using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckWidgetSizing()
    {
        foreach (var definition in WidgetLibrary.Definitions)
        {
            var widget = WidgetLibrary.Create(definition.Name);
            Assert(widget.Span == definition.DefaultSpan && widget.GridColumns == 20 && !widget.CustomSize && widget.Height >= definition.MinHeight, definition.Name + " starts with its purpose-specific size");
            var data = new DashboardData { Widgets = [widget], WidgetLayoutInitialized = true };
            for (var size = 1; size <= 20; size++)
            {
                WidgetLibrary.Place(data, widget, 0, 0, size, size * 24);
                WidgetLibrary.Validate(data);
                if (widget.Span != size || widget.Height != size * 24) throw new Exception("Grid size was clamped for " + definition.Name);
            }
            Assert(widget.CustomSize && widget.Span == 20, definition.Name + " supports all 20 width and height steps");
        }
        var legacy = new WidgetInstance { Column = 6, Span = 6, Text = "Keep my notes" };
        Assert(WidgetLibrary.UpgradeGrid([legacy]) && legacy.Column == 10 && legacy.Span == 10 && legacy.Text == "Keep my notes", "Legacy grid migration preserves relative width and notes");
        Assert(!WidgetLibrary.UpgradeGrid([legacy]), "Grid migration is idempotent");

        CheckWidgetTiling();
    }
}
