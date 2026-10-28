using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace SessionDashboard;
// Geometry avoids font fallback changing caption symbols into missing-glyph boxes.
internal sealed class NexaCaptionIcon : FrameworkElement
{
    internal string Kind { get; }
    internal NexaCaptionIcon(string kind) { Kind=kind;Width=12;Height=12;IsHitTestVisible=false; }
    protected override void OnRender(DrawingContext dc)
    {
        var brush=(Window.GetWindow(this)?.TryFindResource("TextBrush") as Brush)??Brushes.White;
        var pen=new Pen(brush,1);
        switch(Kind)
        {
            case "Minimize":dc.DrawLine(pen,new(1,6),new(11,6));break;
            case "Close":dc.DrawLine(pen,new(1,1),new(11,11));dc.DrawLine(pen,new(11,1),new(1,11));break;
            case "Restore":dc.DrawGeometry(null,pen,Geometry.Parse("M3,3 L3,1 11,1 11,9 9,9 M1,3 L9,3 9,11 1,11 Z"));break;
            default:dc.DrawRectangle(null,pen,new Rect(1,1,10,10));break;
        }
    }
    internal static void Set(Button button,string kind)
    {button.Content=new NexaCaptionIcon(kind);System.Windows.Automation.AutomationProperties.SetName(button,kind);}
}
