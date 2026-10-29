using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace SessionDashboard;
// Stable, portable vector identities. Missing/unknown identifiers retain a preset-specific fallback.
internal static class SpaceIcons
{
    internal static readonly Dictionary<string,string> Paths=new()
    {
        ["Graduation cap"]="M1,7 L10,2 19,7 10,12 Z M4,9 L4,15 Q10,20 16,15 L16,9 M19,7 L19,15",
        ["Cap and gown"]="M2,4 L10,1 18,4 10,7 Z M5,6 L5,9 Q10,12 15,9 L15,6 M18,4 L18,10 M6,11 L10,14 14,11 18,15 15,17 14,15 14,20 6,20 6,15 5,17 2,15 Z M10,14 L10,20",
        ["Church"]="M3,10 L10,5 17,10 17,19 3,19 Z M10,1 L10,7 M7,3 L13,3 M8,19 L8,13 12,13 12,19",
        ["Book"]="M10,5 Q5,1 1,4 L1,17 Q5,14 10,18 Q15,14 19,17 L19,4 Q15,1 10,5 L10,18",
        ["Briefcase"]="M2,6 L18,6 18,18 2,18 Z M6,6 L6,2 14,2 14,6 M2,11 Q10,15 18,11",
        ["Home"]="M1,9 L10,1 19,9 M4,7 L4,19 8,19 8,13 12,13 12,19 16,19 16,7",
        ["Fitness"]="M1,7 L4,7 4,13 1,13 Z M4,4 L7,4 7,16 4,16 Z M7,10 L13,10 M13,4 L16,4 16,16 13,16 Z M16,7 L19,7 19,13 16,13",
        ["Lightning"]="M11,1 L3,11 9,11 7,19 17,8 11,8 Z",
        ["Calendar"]="M2,4 L18,4 18,19 2,19 Z M6,1 L6,7 M14,1 L14,7 M2,9 L18,9 M6,13 L9,13 M12,16 L15,16",
        ["Palette"]="M10,1 C-3,1 -2,19 8,19 C15,19 7,12 15,13 C22,14 21,1 10,1 Z M5,6 L5,7 M10,4 L10,5 M15,6 L15,7",
        ["Gamepad"]="M4,6 L16,6 Q19,8 19,16 Q17,19 13,14 L7,14 Q3,19 1,16 Q1,8 4,6 Z M4,10 L8,10 M6,8 L6,12 M13,10 L14,10 M16,12 L17,12",
        ["People"]="M7,6 A3,3 0 1 0 1,6 A3,3 0 1 0 7,6 M17,6 A3,3 0 1 0 11,6 A3,3 0 1 0 17,6 M1,18 L1,14 Q4,9 8,14 L8,18 M11,18 L11,14 Q15,9 19,14 L19,18",
        ["Sparkle"]="M10,1 L12,8 19,10 12,12 10,19 8,12 1,10 8,8 Z"
    };
    internal static string Resolve(string? id,string origin)
    {
        if(id!=null && Paths.ContainsKey(id))return id;
        var text=origin.ToLowerInvariant();return text.Contains("church")||text.Contains("bible")?"Church":text.Contains("class")||text.Contains("study")||text.Contains("school")?"Book":text.Contains("meeting")?"People":text.Contains("work")?"Briefcase":text.Contains("fitness")?"Fitness":text.Contains("creative")?"Palette":text.Contains("plan")?"Calendar":text.Contains("personal")?"Home":text.Contains("productiv")?"Lightning":text.Contains("gam")?"Gamepad":"Sparkle";
    }
    internal static FrameworkElement Create(string id,double size=20)
    {
        var icon=new System.Windows.Shapes.Path{Data=Geometry.Parse(Paths.GetValueOrDefault(id,Paths["Sparkle"])),Width=size,Height=size,Stretch=Stretch.Uniform,StrokeThickness=1.5,Margin=new Thickness(0,0,0,5)};
        icon.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty,"SpaceIconBrush");System.Windows.Automation.AutomationProperties.SetName(icon,id+" space icon");return icon;
    }
    internal static FrameworkElement Picker(string selected,Action<string> change)
    {
        var host=new Grid{HorizontalAlignment=HorizontalAlignment.Left};
        var selector=new Button{MinWidth=84,MinHeight=42,ToolTip="Choose an Icon"};
        System.Windows.Automation.AutomationProperties.SetName(selector,"Choose an Icon");
        var grid=new System.Windows.Controls.Primitives.UniformGrid{Columns=5};
        var scroll=new ScrollViewer{Content=grid,MaxHeight=260,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        var frame=new Border{Child=scroll,Width=300,Padding=new Thickness(10),CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1)};
        frame.SetResourceReference(Border.BackgroundProperty,"PanelBrush");frame.SetResourceReference(Border.BorderBrushProperty,"LineBrush");
        var popup=new System.Windows.Controls.Primitives.Popup{PlacementTarget=selector,Placement=System.Windows.Controls.Primitives.PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true,Child=frame};
        var buttons=new List<Button>();
        void Refresh(){var row=new StackPanel{Orientation=Orientation.Horizontal};var icon=Create(Resolve(selected,""),24);icon.Margin=new Thickness(0,0,12,0);row.Children.Add(icon);row.Children.Add(new TextBlock{Text="▾",VerticalAlignment=VerticalAlignment.Center});selector.Content=row;foreach(var button in buttons){button.BorderThickness=new Thickness(Equals(button.Tag,selected)?2:1);button.SetResourceReference(Control.BorderBrushProperty,Equals(button.Tag,selected)?"AccentBrush":"LineBrush");}}
        foreach(var name in Paths.Keys){var button=new Button{Tag=name,Content=Create(name,23),Width=50,Height=46,Padding=new Thickness(9),Margin=new Thickness(2),ToolTip=name};System.Windows.Automation.AutomationProperties.SetName(button,name+" space icon");button.Click+=(_,_)=>{selected=name;change(name);Refresh();popup.IsOpen=false;selector.Focus();};buttons.Add(button);grid.Children.Add(button);}
        selector.Click+=(_,_)=>{popup.IsOpen=!popup.IsOpen;if(popup.IsOpen){frame.Width=Math.Min(300,Math.Max(240,(Window.GetWindow(host)?.ActualWidth??340)-40));buttons.FirstOrDefault(b=>Equals(b.Tag,selected))?.Focus();}};
        frame.PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Escape){popup.IsOpen=false;selector.Focus();e.Handled=true;return;}var delta=e.Key switch{System.Windows.Input.Key.Right=>1,System.Windows.Input.Key.Left=>-1,System.Windows.Input.Key.Down=>5,System.Windows.Input.Key.Up=>-5,_=>0};if(delta!=0){var index=buttons.FindIndex(b=>b.IsKeyboardFocusWithin);buttons[Math.Clamp(index+delta,0,buttons.Count-1)].Focus();e.Handled=true;}};
        host.Children.Add(selector);host.Children.Add(popup);Refresh();return host;
    }
}
