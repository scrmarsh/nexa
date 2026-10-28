using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace SessionDashboard;
public partial class MainWindow
{
    private Canvas? completionLayer;
    private void RemoveCompletionVisuals(){if(completionLayer!=null){WidgetCanvas.Children.Remove(completionLayer);completionLayer=null;}}
    private Action<double> BeginCompletionVisuals(DashboardData source,DateTimeOffset now)
    {
        RemoveCompletionVisuals();
        completionLayer=new Canvas{Width=Math.Max(1,WidgetCanvas.ActualWidth),Height=Math.Max(1,WidgetCanvas.Height),IsHitTestVisible=false};
        Panel.SetZIndex(completionLayer,300);WidgetCanvas.Children.Add(completionLayer);
        var updates=new List<Action<double>>();
        foreach(var widget in source.Widgets.Where(w=>w.SessionScoped))
        {
            if(!widgetFrames.TryGetValue(widget.Id,out var frame))continue;
            var editor=WidgetLibrary.Definition(widget.Kind).Editor;
            if(editor is not ("counter" or "waterBottles" or "beer" or "stopwatch" or "timer" or "progress" or "squashes" or "text" or "timeline" or "hedy"))continue;
            var content=new Grid();var mask=new Border{Width=Math.Max(1,frame.Width-16),Height=Math.Max(1,frame.Height-50),Child=content,CornerRadius=new CornerRadius(8),Padding=new Thickness(12),ClipToBounds=true};mask.SetResourceReference(Border.BackgroundProperty,"PanelBrush");
            Canvas.SetLeft(mask,Canvas.GetLeft(frame)+8);Canvas.SetTop(mask,Canvas.GetTop(frame)+42);completionLayer.Children.Add(mask);
            if(editor is "text" or "timeline" or "hedy")
            {
                var value=editor=="hedy"?source.Session?.HedyTranscript:editor=="timeline"?string.Join("\n",source.Session?.TimelineEntries.Select(e=>e.Text)??[]):widget.PanelKey=="notes"?source.Notes:widget.Text;
                content.Children.Add(new TextBlock{Text=value??"",TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Top});
                updates.Add(t=>mask.Opacity=1-t);continue;
            }
            var stack=new StackPanel{VerticalAlignment=VerticalAlignment.Center};content.Children.Add(stack);
            if(editor is "waterBottles" or "beer")
            {
                var art=new DrinkArtwork(editor=="beer",false){Height=Math.Max(50,Math.Min(150,mask.Height-65)),Vessel=widget.BeverageVessel};stack.Children.Add(art);
                var fill=(double)Math.Clamp(widget.Value/(editor=="beer"?Math.Max(1,widget.BeerGoal):Math.Max(1,widget.WaterBottleGoal)),0,1);
                updates.Add(t=>art.FillFraction=fill*(1-t));
            }
            var label=new TextBlock{FontSize=30,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center,HorizontalAlignment=HorizontalAlignment.Stretch};stack.Children.Add(label);
            if(editor=="progress")
            {
                var bar=new ProgressBar{Minimum=0,Maximum=1,Height=8,Margin=new Thickness(0,10,0,0)};stack.Children.Add(bar);var progress=source.Session?.Progress(now)??0;
                updates.Add(t=>{bar.Value=progress*(1-t);label.Text=NexaTime.Percentage(bar.Value);});
            }
            else if(editor is "timer" or "stopwatch")
            {
                var seconds=editor=="stopwatch"?widget.StopwatchElapsedSeconds+(widget.StopwatchStartedAt is {} started?Math.Max(0,(now-started).TotalSeconds):0):widget.TimerEnd is {} end?Math.Max(0,(end-(widget.TimerStopped??now)).TotalSeconds):0;
                updates.Add(t=>label.Text=ClockText(TimeSpan.FromSeconds(seconds*(1-t)),false));
            }
            else
            {
                var from=editor=="squashes"?source.Productivity.BugsSquashed:widget.Value;var target=editor=="squashes"?0:widget.StartingValue;
                updates.Add(t=>label.Text=decimal.Round(target+(from-target)*(decimal)(1-t),0).ToString("N0"));
            }
            updates.Add(t=>mask.Opacity=t>.85?(1-t)/.15:1);
        }
        void Update(double fraction){foreach(var update in updates)update(Math.Clamp(fraction,0,1));}
        Update(0);return Update;
    }
}
