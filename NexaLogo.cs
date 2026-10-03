using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace SessionDashboard;

// Render the supplied full SVG as vectors. The original file remains the source of truth.
internal sealed class NexaLogo : FrameworkElement
{
    private readonly DrawingImage logo;
    internal NexaLogo()
    {
        using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Nexa;component/assets/nexa-logo-full-dark.svg")).Stream;
        var svg = XDocument.Load(stream); var drawing = new DrawingGroup();
        using (var dc = drawing.Open())
        {
            foreach (var group in svg.Root!.Elements().Where(e => e.Name.LocalName == "g"))
            {
                var icon = group.Attribute("transform") != null;
                if (icon) { dc.PushTransform(new TranslateTransform(40, 40)); dc.PushTransform(new ScaleTransform(.546875, .546875)); dc.PushClip(new RectangleGeometry(new Rect(0, 0, 512, 512), 116, 116)); }
                foreach (var e in group.Elements())
                {
                    double Number(string attr, double fallback = 0) => double.TryParse(e.Attribute(attr)?.Value ?? group.Attribute(attr)?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : fallback;
                    Brush? Color(string attr) { var value = e.Attribute(attr)?.Value ?? group.Attribute(attr)?.Value; return value == null || value == "none" ? null : (Brush)new BrushConverter().ConvertFromString(value)!; }
                    var stroke = Color("stroke"); var pen = stroke == null ? null : new Pen(stroke, Number("stroke-width", 1)) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
                    Geometry? geometry = e.Name.LocalName switch
                    {
                        "rect" => new RectangleGeometry(new Rect(0, 0, Number("width"), Number("height")), Number("rx"), Number("rx")),
                        "circle" => new EllipseGeometry(new Point(Number("cx"), Number("cy")), Number("r"), Number("r")),
                        "line" => new LineGeometry(new Point(Number("x1"), Number("y1")), new Point(Number("x2"), Number("y2"))),
                        "path" => Geometry.Parse(e.Attribute("d")!.Value),
                        "polygon" or "polyline" => Geometry.Parse("M" + e.Attribute("points")!.Value + (e.Name.LocalName == "polygon" ? " Z" : "")),
                        _ => null
                    };
                    if (geometry != null) dc.DrawGeometry(Color("fill"), pen, geometry);
                }
                if (icon) { dc.Pop(); dc.Pop(); dc.Pop(); }
            }
        }
        drawing.Freeze(); logo = new DrawingImage(drawing);
        System.Windows.Automation.AutomationProperties.SetName(this, "Nexa full logo");
    }
    protected override void OnRender(DrawingContext dc)
    {
        var scale = Math.Min(ActualWidth / 1140, ActualHeight / 360);
        dc.PushTransform(new TranslateTransform((ActualWidth - 1140 * scale) / 2, (ActualHeight - 360 * scale) / 2)); dc.PushTransform(new ScaleTransform(scale, scale));
        var text = TryFindResource("TextBrush") as SolidColorBrush;
        var light = text != null && text.Color.R + text.Color.G + text.Color.B < 360;
        if (light)
        {
            dc.PushOpacityMask(new ImageBrush(logo));
            dc.DrawRectangle(text, null, logo.Drawing.Bounds);
            dc.Pop();
        }
        else dc.DrawImage(logo, logo.Drawing.Bounds);
        dc.Pop(); dc.Pop();
    }
}
