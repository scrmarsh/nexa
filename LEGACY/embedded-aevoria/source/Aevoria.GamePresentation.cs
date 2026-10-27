using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed partial class BattlefieldWindow
{
    private Border? gameMenu;
    private bool pauseBeforeMenu;
    private bool fitCamera;
    private bool wheelAnimating;
    private Border? gameHud;
    private Size lastViewport;
    private AevoriaMinimap? minimap;
    internal AevoriaMinimap? Minimap=>minimap;
    internal double ZoomSensitivity {get;set;}=3;
    internal event Action? SettingsRequested;
    internal event Action? ExitGameRequested;
    internal bool GameMenuOpen=>gameMenu?.Visibility==Visibility.Visible;
    internal Point CameraCenter=>Battlefield.ScreenToWorld(new Point(viewport.ActualWidth/2,viewport.ActualHeight/2));
    internal Rect VisibleWorldBounds
    {
        get{var top=Battlefield.ScreenToWorld(new Point());var bottom=Battlefield.ScreenToWorld(new Point(viewport.ActualWidth,viewport.ActualHeight));return new Rect(top,bottom);}
    }
    private void InitializeAevoriaPresentation(WrapPanel controls,TextBlock information)
    {
        Title="Aevoria — "+Living!.Info.Name;
        AevoriaWorkArea.Attach(this);
        var hud=new Border{Tag="AevoriaHud",Child=controls,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(8),MaxWidth=1450,CornerRadius=new CornerRadius(8),Background=new SolidColorBrush(Color.FromArgb(218,13,20,29))};
        gameHud=hud;
        if(nationPanel!=null){nationPanel.Tag="AevoriaHud";nationPanel.Margin=new Thickness(12,100,12,12);}
        if(armyPanel!=null){armyPanel.Tag="AevoriaHud";armyPanel.Margin=new Thickness(12,100,12,12);}
        controls.Margin=new Thickness(6);foreach(var button in controls.Children.OfType<Button>()){button.Padding=new Thickness(8,4,8,4);button.FontSize=11;}
        Button Add(string title,Action action){var b=new Button{Content=title,Margin=new Thickness(0,0,6,4),Padding=new Thickness(8,4,8,4),FontSize=11};b.Click+=(_,_)=>action();controls.Children.Add(b);return b;}
        Add("Menu / Esc",ToggleGameMenu);Add("Settings",()=>{OpenGameMenu();SettingsRequested?.Invoke();});Add("Minimize",()=>WindowState=WindowState.Minimized);
        minimap=new AevoriaMinimap(Living,this){Tag="AevoriaHud",Width=230,Height=160,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(12)};
        information.IsHitTestVisible=false;information.HorizontalAlignment=HorizontalAlignment.Left;information.VerticalAlignment=VerticalAlignment.Bottom;information.MaxWidth=750;information.Background=new SolidColorBrush(Color.FromArgb(190,8,12,20));
        viewport.Children.Add(hud);viewport.Children.Add(information);viewport.Children.Add(minimap);
        var commands=new StackPanel{Margin=new Thickness(22)};commands.Children.Add(new TextBlock{Text="Aevoria · Paused",FontSize=24,Foreground=Brushes.Gold,Margin=new Thickness(0,0,0,16)});
        void Command(string label,Action action){var b=new Button{Content=label,Margin=new Thickness(0,0,0,8),Padding=new Thickness(16,9,16,9)};b.Click+=(_,_)=>action();commands.Children.Add(b);}
        Command("Resume",CloseGameMenu);Command("Settings",()=>SettingsRequested?.Invoke());Command("Fit World",()=>{FitWorld();});Command("Minimize",()=>WindowState=WindowState.Minimized);Command("Return to main menu",Close);Command("Exit game",()=>{if(ExitGameRequested!=null)ExitGameRequested();else Close();});
        gameMenu=new Border{Tag="AevoriaHud",Child=commands,Width=350,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Background=new SolidColorBrush(Color.FromRgb(17,24,36)),BorderBrush=Brushes.SlateGray,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Visibility=Visibility.Collapsed};viewport.Children.Add(gameMenu);
        PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){ToggleGameMenu();e.Handled=true;}};
        InitializePerformanceOverlay();
        Closed+=(_,_)=>minimap.Dispose();WindowState=WindowState.Maximized;
    }
    internal void OpenGameMenu(){if(gameMenu==null || GameMenuOpen)return;pauseBeforeMenu=World.Manager.WorldPaused;World.SetPaused(true);gameMenu.Visibility=Visibility.Visible;if(gameHud!=null)gameHud.IsEnabled=false;Keyboard.Focus(gameMenu.Child);}
    internal void CloseGameMenu(){if(gameMenu==null || !GameMenuOpen)return;gameMenu.Visibility=Visibility.Collapsed;if(gameHud!=null)gameHud.IsEnabled=true;World.SetPaused(pauseBeforeMenu);Battlefield.Focus();}
    internal void ToggleGameMenu(){if(GameMenuOpen)CloseGameMenu();else OpenGameMenu();}
    private bool IsAevoriaHud(DependencyObject? source)
    {for(var node=source;node!=null && node!=viewport;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node))if(node is FrameworkElement{Tag:"AevoriaHud"})return true;return false;}
    private void ClampAevoriaCamera()
    {
        if(Living==null || viewport.ActualWidth<=0 || viewport.ActualHeight<=0)return;
        var width=Living.Info.Extent*zoom;var height=Living.Info.HeightExtent*zoom;
        cameraOffset.X=width<=viewport.ActualWidth?Math.Clamp(cameraOffset.X,0,viewport.ActualWidth-width):Math.Clamp(cameraOffset.X,viewport.ActualWidth-width,0);
        cameraOffset.Y=height<=viewport.ActualHeight?Math.Clamp(cameraOffset.Y,0,viewport.ActualHeight-height):Math.Clamp(cameraOffset.Y,viewport.ActualHeight-height,0);
    }
    private void ResizeAevoriaCamera(Size previous,Size next)
    {
        if(next.Width<=1 || next.Height<=1){if(previous.Width>1 && previous.Height>1)lastViewport=previous;return;}
        if(previous.Width<=1 || previous.Height<=1)previous=lastViewport;
        lastViewport=next;if(previous.Width<=1 || previous.Height<=1)return;
        var center=new Point((previous.Width/2-cameraOffset.X)/zoom,(previous.Height/2-cameraOffset.Y)/zoom);var wasFit=fitCamera;CancelNationFocus();wheelAnimating=false;
        if(wasFit){FitWorld();return;}
        zoom=Math.Clamp(zoom,MinimumZoom,3);cameraOffset=new Vector(next.Width/2-center.X*zoom,next.Height/2-center.Y*zoom);ApplyCamera();
    }
    private void ZoomAevoriaAt(Point point,int delta)
    {
        if(delta==0)return;var pending=wheelAnimating && nationCamera?.IsEnabled==true?targetZoom:zoom;
        var anchor=Battlefield.ScreenToWorld(point);CancelNationFocus();fitCamera=false;wheelAnimating=true;followedFaction=followedUnit=null;
        startZoom=zoom;targetZoom=Math.Clamp(pending*Math.Pow(1.12,delta/120d*Math.Clamp(ZoomSensitivity,1,5)),MinimumZoom,3);startOffset=cameraOffset;targetOffset=(Vector)point-(Vector)anchor*targetZoom;focusDuration=.09;
        if(nationCamera==null){zoom=targetZoom;cameraOffset=targetOffset;ApplyCamera();wheelAnimating=false;return;}
        Battlefield.TerrainView?.DeferDetailUpdates(true);focusClock.Restart();nationCamera.Start();
    }
    internal void NavigateTo(Point point)
    {if(Living==null || !double.IsFinite(point.X) || !double.IsFinite(point.Y))return;CancelNationFocus();fitCamera=false;wheelAnimating=false;followedFaction=followedUnit=null;point=new Point(Math.Clamp(point.X,0,Living.Info.Extent),Math.Clamp(point.Y,0,Living.Info.HeightExtent));cameraOffset=new Vector(viewport.ActualWidth/2-point.X*zoom,viewport.ActualHeight/2-point.Y*zoom);ApplyCamera();}
    internal void SetMinimapVisible(bool visible){if(minimap!=null)minimap.Visibility=visible?Visibility.Visible:Visibility.Collapsed;}
}
