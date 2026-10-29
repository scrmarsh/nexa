using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace SessionDashboard;
internal static class PixelShapes
{
    internal static readonly string[] Names=["Square","Circle","Triangle","Octagon","Hearts"];
    internal static string Normalize(string? shape)=>Names.Contains(shape)?shape!:"Square";
    private static readonly Dictionary<string,Geometry> outlines=new()
    {
        ["Triangle"]=Geometry.Parse("M3,0 L5,0 L5,2 L6,2 L6,4 L7,4 L7,6 L8,6 L8,8 L0,8 L0,6 L1,6 L1,4 L2,4 L2,2 L3,2 Z"),
        ["Octagon"]=Geometry.Parse("M2,0 L6,0 L6,1 L7,1 L7,2 L8,2 L8,6 L7,6 L7,7 L6,7 L6,8 L2,8 L2,7 L1,7 L1,6 L0,6 L0,2 L1,2 L1,1 L2,1 Z"),
        ["Hearts"]=Geometry.Parse("M0,1 L1,1 L1,0 L3,0 L3,1 L5,1 L5,0 L7,0 L7,1 L8,1 L8,4 L7,4 L7,5 L6,5 L6,6 L5,6 L5,7 L3,7 L3,6 L2,6 L2,5 L1,5 L1,4 L0,4 Z")
    };
    private static readonly Dictionary<(string,double),Geometry> clips=[];
    internal static Geometry? Clip(string shape,double size)
    {
        if(!outlines.TryGetValue(shape,out var outline))return null;
        if(clips.TryGetValue((shape,size),out var cached))return cached;
        var geometry=outline.Clone();geometry.Transform=new ScaleTransform(size/8,size/8);geometry.Freeze();
        if(clips.Count>128)clips.Clear();clips[(shape,size)]=geometry;return geometry;
    }
    internal static FrameworkElement Preview(string shape)
    {
        var grid=new System.Windows.Controls.Primitives.UniformGrid{Columns=8};for(var i=0;i<24;i++){var pixel=new Border{Width=18,Height=18,Margin=new Thickness(2),CornerRadius=new CornerRadius(shape=="Circle"?9:0),Clip=Clip(shape,18)};pixel.SetResourceReference(Border.BackgroundProperty,i<13?"PixelBrush":"LineBrush");grid.Children.Add(pixel);}return grid;
    }
}
