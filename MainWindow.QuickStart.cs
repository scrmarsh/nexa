using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace SessionDashboard;
public partial class MainWindow
{
    private Grid? guideLayer;
    private TourShade? guideShade;
    private Border? guideCard;
    private readonly DispatcherTimer guidePosition=new(){Interval=TimeSpan.FromMilliseconds(120)};
    private WidgetInstance? guideBaseline;
    internal bool TutorialActive=>guideLayer!=null;
    internal void StartQuickStart(bool replay=true)
    {
        if(replay){Options.QuickStartStep=0;Options.QuickStartWidget=null;Options.QuickStartCompleted=false;state.OfferQuickStart=true;}
        if(TutorialActive)return;
        notifications?.Dismiss("tip");
        guideLayer=new Grid();guideShade=new TourShade();guideLayer.Children.Add(guideShade);Panel.SetZIndex(guideLayer,160);((Grid)Content).Children.Add(guideLayer);
        guidePosition.Tick+=PositionGuide;guidePosition.Start();RenderGuide();Changed();SaveNow();
    }
    private void RemoveQuickStart()
    {
        guidePosition.Stop();guidePosition.Tick-=PositionGuide;
        if(guideLayer!=null)((Grid)Content).Children.Remove(guideLayer);
        guideLayer=null;guideShade=null;guideCard=null;
    }
    private async void FinishQuickStart(bool skipped)
    {
        Options.QuickStartCompleted=true;Options.QuickStartSkipped=skipped;Options.QuickStartStep=0;state.OfferQuickStart=false;
        Changed();SaveNow();
        if(!NexaMotion.Allowed(Options)){RemoveQuickStart();return;}
        var layer=guideLayer;if(layer==null)return;layer.IsHitTestVisible=false;
        await NexaMotion.Fade(layer,0,Options);
        await Dispatcher.InvokeAsync(()=>{if(ReferenceEquals(layer,guideLayer))RemoveQuickStart();});
    }
    private FrameworkElement? GuideTarget()
    {
        var step=Options.QuickStartStep;if(step==1){workspaceAdd?.BringIntoView();return workspaceAdd;}
        if(Options.QuickStartWidget is not {} id || !widgetFrames.TryGetValue(id,out var frame))return null;
        if(step==3 && widgetResizeHandles.TryGetValue(id,out var handles))return handles.LastOrDefault().Handle;
        if(step==4)return FindGuideChildren(frame).OfType<TextBox>().FirstOrDefault(t=>System.Windows.Automation.AutomationProperties.GetName(t).StartsWith("Widget title "));
        return step is 2 or 3 or 4?frame:null;
    }
    private static IEnumerable<DependencyObject> FindGuideChildren(DependencyObject parent)
    {
        foreach(var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>()){yield return child;foreach(var nested in FindGuideChildren(child))yield return nested;}
    }
    private void PositionGuide(object? sender,EventArgs e)
    {
        if(guideShade==null)return;var target=GuideTarget();
        if(target==null || !target.IsVisible){guideShade.Hole=Rect.Empty;return;}
        try{var hole=target.TransformToVisual((Visual)Content).TransformBounds(new Rect(target.RenderSize));hole.Inflate(7,7);guideShade.Hole=hole;if(guideCard!=null)guideCard.VerticalAlignment=hole.Top+hole.Height/2>ActualHeight/2?VerticalAlignment.Top:VerticalAlignment.Bottom;}
        catch(InvalidOperationException){guideShade.Hole=Rect.Empty;}
    }
    internal void TutorialAction(string action,Guid id)
    {
        if(!TutorialActive)return;
        if(Options.QuickStartStep==1 && action=="added")
        {
            // Pixel Timer is anchored; practice on a movable card if the chosen tool was the banner.
            if(!widgetFrames.ContainsKey(id))return;
            Options.QuickStartWidget=id;AdvanceGuide();return;
        }
        if(Options.QuickStartWidget!=id || guideBaseline==null)return;
        var item=state.Widgets.FirstOrDefault(w=>w.Id==id);if(item==null)return;
        var changed=Options.QuickStartStep switch{2=>item.PlacementX!=guideBaseline.PlacementX || item.PlacementY!=guideBaseline.PlacementY,3=>item.Span!=guideBaseline.Span || item.Height!=guideBaseline.Height,4=>item.Title!=guideBaseline.Title,_=>false};
        if(changed)AdvanceGuide();
    }
    internal void AdvanceGuide()
    {
        Options.QuickStartStep=Math.Min(6,Options.QuickStartStep+1);RenderGuide();Changed();SaveNow();
    }
    private void RenderGuide()
    {
        if(guideLayer==null)return;while(guideLayer.Children.Count>1)guideLayer.Children.RemoveAt(1);
        var step=Math.Clamp(Options.QuickStartStep,0,6);Options.QuickStartStep=step;
        guideBaseline=Continuity.Clone(state.Widgets.FirstOrDefault(w=>w.Id==Options.QuickStartWidget)??new WidgetInstance());
        var panel=new StackPanel();string[] headings=["Welcome to nexa!","Add your first widget","Move your widget","Try resizing","Make it yours","Connect your apps","You're ready!"];
        string[] messages=["Your own little corner of productivity. Capture notes, tackle tasks, manage time, track progress and stay hydrated. Arrange your tools your way.","Click the highlighted Add Widget button and choose any movable widget. Your preset tools will stay where they are.","Drag the dotted grip on your new widget to an open area. This step advances when its saved position changes.","Drag the highlighted corner to change the widget's size. Try growing it wider or taller.","Click the widget title and give it a new name.","Todoist brings your tasks into your Space. Hevy connects your workout account. Both are optional, and Hedy recording notes remain a separate service.","Your Space is ready. Make it yours!"];
        panel.Children.Add(new TextBlock{Text=headings[step],FontSize=23,FontWeight=FontWeights.Bold});
        panel.Children.Add(new TextBlock{Text=messages[step],TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,15),FontSize=15});
        if(step==0)
        {
            var previews=new WrapPanel();foreach(var name in new[]{"Pixel timer","Task List","Notes","Productivity","Progress bar","Water Bottle Counter","Clock","Focus Reset","Calendar"})
            {
                var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(ToolPreview(name));row.Children.Add(new TextBlock{Text=name,Margin=new Thickness(8,0,0,0),VerticalAlignment=VerticalAlignment.Center});
                previews.Children.Add(new Border{Padding=new Thickness(8),Margin=new Thickness(0,0,6,6),Child=row});
            }
            panel.Children.Add(previews);NexaMotion.Stagger(previews.Children.OfType<FrameworkElement>(),Options);
        }
        if(step==5)
        {
            foreach(var service in new[]{"Todoist","Hedy","Hevy"}){var connect=new Button{Content="Connect "+service,Margin=new Thickness(0,0,8,6),HorizontalAlignment=HorizontalAlignment.Left};connect.Click+=(_,_)=>OpenMainSettings(service=="Hedy"?"Hedy":"Integrations");panel.Children.Add(connect);}
        }
        if(step==6)panel.Children.Add(SpaceIcons.Create(SpaceIcons.Resolve(state.SpaceIcon,""),34));
        var controls=new Grid();controls.ColumnDefinitions.Add(new());controls.ColumnDefinitions.Add(new(){Width=GridLength.Auto});panel.Children.Add(controls);
        var leftControls=new WrapPanel();controls.Children.Add(leftControls);var rightControls=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};Grid.SetColumn(rightControls,1);controls.Children.Add(rightControls);
        void Button(string label,Action click,bool right=false){var b=new Button{Content=label,Margin=new Thickness(0,0,8,0),MinWidth=90};b.Click+=(_,_)=>click();(right?rightControls:leftControls).Children.Add(b);}
        if(step>0)Button("Back",()=>{Options.QuickStartStep--;RenderGuide();Changed();SaveNow();});
        Button(step==0?"I Know What I'm Doing":"Skip guide",()=>FinishQuickStart(true));
        if(step is 1 or 2 or 3 or 4)Button("Skip step",AdvanceGuide);
        else Button(step==6?"Get Started":step==5?"Skip for Now":"Continue",()=>{if(step==6)FinishQuickStart(false);else AdvanceGuide();},true);
        var card=new Border{Child=panel,Padding=new Thickness(22),CornerRadius=new CornerRadius(16),BorderThickness=new Thickness(2),MaxWidth=580,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(24)};
        card.SetResourceReference(Border.BackgroundProperty,"PanelBrush");card.SetResourceReference(Border.BorderBrushProperty,"AccentBrush");guideCard=card;guideLayer.Children.Add(card);NexaMotion.Entrance(card,Options);
        Dispatcher.BeginInvoke(new Action(()=>{PositionGuide(null,EventArgs.Empty);if(GuideTarget() is {} target && step is 2 or 3 or 4)target.BringIntoView();}),DispatcherPriority.Loaded);
        card.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}
internal sealed class TourShade:FrameworkElement
{
    private Rect hole=Rect.Empty;
    internal Rect Hole{get=>hole;set{if(hole==value)return;hole=value;InvalidateVisual();}}
    protected override HitTestResult? HitTestCore(PointHitTestParameters parameters)=>!hole.IsEmpty && hole.Contains(parameters.HitPoint)?null:base.HitTestCore(parameters);
    protected override void OnRender(DrawingContext dc)
    {
        var shade=new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(new Rect(RenderSize)),hole.IsEmpty?Geometry.Empty:new RectangleGeometry(hole,8,8));
        dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(190,0,0,0)),null,shade);
        if(!hole.IsEmpty)dc.DrawRoundedRectangle(null,new Pen(TryFindResource("AccentBrush") as Brush??Brushes.MediumPurple,2),hole,8,8);
        if(!hole.IsEmpty)
        {
            var brush=TryFindResource("AccentBrush") as Brush??Brushes.Gold;var x=Math.Clamp(hole.Left+hole.Width/2,20,Math.Max(20,ActualWidth-20));
            var above=hole.Top>55;var tip=new Point(x,above?hole.Top-9:hole.Bottom+9);var direction=above?-1:1;
            dc.DrawLine(new Pen(brush,3),new Point(x+10,tip.Y+direction*34),tip);
            dc.DrawLine(new Pen(brush,3),tip,new Point(x-7,tip.Y+direction*12));dc.DrawLine(new Pen(brush,3),tip,new Point(x+11,tip.Y+direction*8));
        }
    }
}
