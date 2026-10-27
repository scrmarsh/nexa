using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed partial class BattlefieldWindow : NexaDialog
{
    internal ArcadeWorldController World {get;}
    internal BugOverlay Battlefield {get;}
    internal LivingWorldSession? Living {get;}
    private readonly Grid viewport=new() {Background=new SolidColorBrush(Color.FromRgb(8,12,20)),ClipToBounds=true};
    private readonly TextBlock status=new() {FontSize=11,Margin=new Thickness(12,5,12,5),TextWrapping=TextWrapping.Wrap};
    private readonly Button pause;
    private readonly ComboBox follow=new() {MinWidth=150,MaxWidth=200,ToolTip="Follow a civilization"};
    private double zoom=1;
    private Vector cameraOffset;
    private Point? panStart;
    private int? followedFaction,followedUnit;
    private string factionList="";
    private bool ready;

    internal static BattlefieldWindow ShowFor(Window owner,StateStore store)
    {
        var living=store.GameContext ? LivingWorlds.Current(store) : null;var world=living?.Controller??ArcadeWorlds.For(store);
        var existing=Application.Current.Windows.OfType<BattlefieldWindow>().FirstOrDefault(w=>ReferenceEquals(w.World,world));
        if(existing!=null) {existing.WindowState=WindowState.Normal;existing.Activate();return existing;}
        var window=new BattlefieldWindow(world,living) {Owner=owner is SettingsDialog ? owner.Owner??owner : owner};window.Show();return window;
    }
    internal BattlefieldWindow(ArcadeWorldController world,LivingWorldSession? living=null,bool strategic=false):base(gameplay:strategic && living!=null)
    {
        Living=living;World=world;Title="Nexa \u00b7 Infinite Warfare";Width=1200;Height=800;MinWidth=700;MinHeight=480;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        if(strategic)viewport.Tag="AevoriaMapSurface";
        var root=new DockPanel();var controls=new WrapPanel {Margin=new Thickness(10,8,10,6)};
        DockPanel.SetDock(controls,Dock.Top);root.Children.Add(controls);
        Button Add(string label,Action action)
        {
            var b=new Button {Content=label,Padding=new Thickness(10,5,10,5),Margin=new Thickness(0,0,6,4),FontSize=12};
            System.Windows.Automation.AutomationProperties.SetName(b,label);b.Click+=(_,_)=>Attempt(action);controls.Children.Add(b);return b;
        }
        pause=Add("Pause",()=>World.SetPaused(!World.Manager.WorldPaused));
        Add(living==null ? "Center camera" : "Reset camera",CenterCamera);
        if(living!=null)Add("RETURN TO WORLD",FitWorld);
        if(living?.Info.GeneratorVersion>=2)
        {
            var quality=new ComboBox {MinWidth=100,Margin=new Thickness(0,0,8,4),ToolTip="Terrain detail. Labels and bug artwork retain their quality."};
            foreach(var value in Enum.GetValues<TerrainQuality>())quality.Items.Add(value);
            quality.SelectedItem=living.Info.Quality;quality.SelectionChanged+=(_,_)=> {if(quality.SelectedItem is TerrainQuality value) {living.Info.Quality=value;Battlefield?.RefreshFrame();}};
            controls.Children.Add(quality);
        }
        controls.Children.Add(follow);follow.Margin=new Thickness(0,0,8,4);
        follow.SelectionChanged+=(_,_)=> {followedFaction=follow.SelectedItem is ComboBoxItem {Tag:int id} && id>0 ? id : null;followedUnit=null;ApplyFollow();};
        var unitFollow=new CheckBox {Content="Follow a bug",VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(2,0,10,4),ToolTip="Select this, then click a bug to follow. Turn it off to squash bugs."};controls.Children.Add(unitFollow);
        unitFollowControl=unitFollow;
        Add("Maximize",()=>WindowState=WindowState==WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
        Add("Restore backup",()=>ArcadeWorldCommands.RestoreBackup(this,World));
        if(living==null)Add("Reset world",()=>ArcadeWorldCommands.Reset(this,World));
        Add("Return",Close);
        DockPanel.SetDock(status,Dock.Bottom);root.Children.Add(status);root.Children.Add(viewport);
        Battlefield=new BugOverlay(World.Manager) {ShowHud=false,StrategicMap=strategic};Battlefield.SetWidgetSurfaces([]);
        if(living!=null) {Battlefield.TerrainView=new LivingTerrainView(living,Battlefield);Title="Nexa - LIVING WORLDS - "+living.Info.Name;}
        viewport.Children.Add(Battlefield);
        unitFollow.Click+=(_,_)=> {Battlefield.SelectUnits=unitFollow.IsChecked==true;if(!Battlefield.SelectUnits)followedUnit=null;};
        Battlefield.UnitSelected+=FollowBug;
        if(living!=null){InitializeNationMap();if(strategic)InitializeArmySelection();}
        if(strategic && living!=null){root.Children.Clear();InitializeAevoriaPresentation(controls,status);Content=viewport;}else Content=root;
        viewport.SizeChanged+=(_,e)=> {if(ready){if(strategic)ResizeAevoriaCamera(e.PreviousSize,e.NewSize);else ApplyCamera();}};
        viewport.PreviewMouseWheel+=(_,e)=>
        {
            if(strategic && IsAevoriaHud(e.OriginalSource as DependencyObject))return;
            if(strategic)SmoothZoomAt(e.GetPosition(Battlefield),e.Delta);else ZoomAt(e.GetPosition(Battlefield),e.Delta);e.Handled=true;
        };
        viewport.PreviewMouseDown+=(_,e)=>
        {
            if(strategic && IsAevoriaHud(e.OriginalSource as DependencyObject))return;
            if(e.ChangedButton is not (MouseButton.Right or MouseButton.Middle))return;
            CancelNationFocus();
            panStart=e.GetPosition(viewport);followedFaction=followedUnit=null;viewport.CaptureMouse();e.Handled=true;
        };
        viewport.MouseMove+=(_,e)=> {if(panStart is not {} start)return;var next=e.GetPosition(viewport);Pan(next-start);panStart=next;e.Handled=true;};
        viewport.MouseUp+=(_,e)=> {if(panStart==null)return;panStart=null;viewport.ReleaseMouseCapture();e.Handled=true;};
        viewport.LostMouseCapture+=(_,_)=>panStart=null;
        PreviewKeyDown+=(_,e)=> {if(e.Key==Key.F11) {WindowState=WindowState==WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;e.Handled=true;}};
        Loaded+=(_,_)=> {ready=true;World.Attach(this,10);World.Changed+=WorldChanged;if(living?.Info.CameraZoom>0) {zoom=Math.Clamp(living.Info.CameraZoom,MinimumZoom,3);cameraOffset=new Vector(living.Info.CameraX,living.Info.CameraY);ApplyCamera();}else if(strategic)FitWorld();else CenterCamera();WorldChanged();};
        IsVisibleChanged+=(_,_)=>UpdateActivity();StateChanged+=(_,_)=>UpdateActivity();
        Closing+=(_,e)=>
        {
            try {World.SaveNow();Living?.SaveMetadata();}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
            {status.Text=ex.Message;e.Cancel=MessageBox.Show(this,"The Arcade world could not be saved. Close this view using the last successful save?","Arcade save",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)!=MessageBoxResult.Yes;}
        };
        Closed+=(_,_)=> {Battlefield.TerrainView?.Dispose();World.Changed-=WorldChanged;World.Detach(this);};
    }
    private void Attempt(Action action)
    {
        try {action();WorldChanged();}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException) {status.Text=ex.Message;}
    }
    private void UpdateActivity() {if(ready)World.SetActive(this,IsVisible && WindowState!=WindowState.Minimized);}
    private double MinimumZoom=>Living==null ? .2 : Math.Max(.005,Math.Min(Math.Max(1,viewport.ActualWidth)/Living.Info.Extent,Math.Max(1,viewport.ActualHeight)/Living.Info.HeightExtent)*.92);
    internal void CenterCamera()
    {
        fitCamera=false;
        if(Living==null) {FitWorld();return;}
        ClearNationSelection();followedFaction=followedUnit=null;zoom=Math.Clamp(.85,MinimumZoom,3);
        cameraOffset=new Vector(viewport.ActualWidth/2-Living.Info.FocusX*zoom,viewport.ActualHeight/2-Living.Info.FocusY*zoom);ApplyCamera();
    }
    internal void FitWorld()
    {
        ClearNationSelection();followedFaction=followedUnit=null;var size=World.Manager.BattleArena.Size;
        fitCamera=Battlefield.StrategicMap;
        zoom=Math.Clamp(Math.Min(Math.Max(1,viewport.ActualWidth)/size.Width,Math.Max(1,viewport.ActualHeight)/size.Height)*.96,MinimumZoom,3);
        cameraOffset=new Vector((viewport.ActualWidth-size.Width*zoom)/2,(viewport.ActualHeight-size.Height*zoom)/2);ApplyCamera();
    }
    private void ApplyCamera()
    {
        if(Battlefield.StrategicMap)ClampAevoriaCamera();
        Battlefield.Camera=new Matrix(zoom,0,0,zoom,cameraOffset.X,cameraOffset.Y);Battlefield.RefreshFrame();
        minimap?.InvalidateVisual();
        if(Living!=null) {Living.Info.CameraZoom=zoom;Living.Info.CameraX=cameraOffset.X;Living.Info.CameraY=cameraOffset.Y;}
    }
    internal void ZoomAt(Point point,int delta)
    {
        fitCamera=false;
        CancelNationFocus();
        var worldPoint=Battlefield.ScreenToWorld(point);zoom=Math.Clamp(zoom*Math.Pow(1.12,delta/120d),MinimumZoom,3);
        cameraOffset=(Vector)point-(Vector)worldPoint*zoom;followedFaction=followedUnit=null;ApplyCamera();
    }
    internal void SmoothZoomAt(Point point,int delta)
    {
        if(Battlefield.StrategicMap){ZoomAevoriaAt(point,delta);return;}
        if(nationCamera==null){ZoomAt(point,delta);return;}CancelNationFocus();var anchor=Battlefield.ScreenToWorld(point);followedFaction=followedUnit=null;
        startZoom=zoom;targetZoom=Math.Clamp(zoom*Math.Pow(1.12,delta/120d),MinimumZoom,3);startOffset=cameraOffset;targetOffset=(Vector)point-(Vector)anchor*targetZoom;focusDuration=.14;
        Battlefield.TerrainView?.DeferDetailUpdates(true);focusClock.Restart();nationCamera.Start();
    }
    internal void Pan(Vector delta) {if(!double.IsFinite(delta.X) || !double.IsFinite(delta.Y))return;CancelNationFocus();fitCamera=false;followedFaction=followedUnit=null;cameraOffset+=delta;if(Living!=null){var size=World.Manager.BattleArena.Size;cameraOffset.X=Math.Clamp(cameraOffset.X,-size.Width*zoom+40,Math.Max(40,viewport.ActualWidth-40));cameraOffset.Y=Math.Clamp(cameraOffset.Y,-size.Height*zoom+40,Math.Max(40,viewport.ActualHeight-40));}ApplyCamera();}
    internal int? FollowedUnitId=>followedUnit;
    internal void FollowBug(BugEntity bug)
    {
        if(!BugManager.Alive(bug))return;
        ClearNationSelection();
        followedFaction=null;follow.SelectedIndex=0;followedUnit=bug.UnitId;
        if(Living!=null)zoom=Math.Max(zoom,1.8);ApplyFollow();
    }
    private void ApplyFollow()
    {
        Point? point=null;
        if(followedUnit is {} unit) {point=World.Manager.Bugs.FirstOrDefault(b=>b.UnitId==unit && BugManager.Alive(b))?.Position;if(point==null)followedUnit=null;}
        else if(followedFaction is {} id)point=World.Manager.Colonies.Values.FirstOrDefault(c=>c.Id==id && !c.Eliminated)?.Objective;
        if(point is {} target) {var desired=new Vector(viewport.ActualWidth/2-target.X*zoom,viewport.ActualHeight/2-target.Y*zoom);cameraOffset=Living==null ? desired : cameraOffset+(desired-cameraOffset)*.18;ApplyCamera();}
    }
    private void WorldChanged()
    {
        Battlefield.BindManager(World.Manager);pause.Content=World.Manager.WorldPaused ? "Resume" : "Pause";
        var list=string.Join("|",World.Manager.Colonies.Values.Select(c=>$"{c.Id}:{c.Name}:{c.Eliminated}"));
        if(list!=factionList)
        {
            factionList=list;var selected=followedFaction;
            var selectedUnit=followedUnit;follow.Items.Clear();follow.Items.Add(new ComboBoxItem {Content="Free camera",Tag=0});
            foreach(var c in World.Manager.Colonies.Values.Where(c=>!c.Eliminated))follow.Items.Add(new ComboBoxItem {Content=c.Name,Tag=c.Id});
            follow.SelectedItem=follow.Items.OfType<ComboBoxItem>().FirstOrDefault(item=>Equals(item.Tag,selected??0))??follow.Items[0];followedUnit=selectedUnit;
        }
        ApplyFollow();
        status.Text=World.LoadBlocked ? "Recovery required. "+World.Status : World.Status ?? $"{(World.Manager.WorldPaused ? "Paused" : (Living==null ? "Infinite Warfare" : $"{Living.Info.Name} | Seed: {Living.Info.Seed}"))} \u00b7 {World.Manager.WorldTimeLabel} \u00b7 {World.Manager.Bugs.Count(BugManager.Alive)} bugs \u00b7 Right-drag to pan, wheel to zoom. Saved on this device.";
        if(Living!=null && World.Manager.WorldPaused)status.Text=$"{Living.Info.Name} | Seed: {Living.Info.Seed} | "+status.Text;
        Battlefield.RefreshFrame();
        UpdateNationSelection();
    }
}
internal static class ArcadeWorldCommands
{
    internal static bool TryReset(ArcadeWorldController world,Func<bool> confirmation)
    {if(!confirmation())return false;world.ResetConfirmed();return true;}
    internal static void Reset(Window owner,ArcadeWorldController world)
    {
        TryReset(world,()=>MessageBox.Show(owner,"Reset the shared Arcade world? All armies, territory and history will start over in both views. Your Nexa Spaces and widgets are unaffected. The previous valid world is kept as a backup.","Reset Arcade World",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)==MessageBoxResult.Yes);
    }
    internal static void RestoreBackup(Window owner,ArcadeWorldController world)
    {
        if(!world.HasBackup)throw new IOException("No previous Arcade save is available yet.");
        if(MessageBox.Show(owner,"Restore the previous valid Arcade save? Progress after that save will be replaced in both views. Existing files will be preserved for recovery.","Restore Arcade backup",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)==MessageBoxResult.Yes)world.RecoverBackup();
    }
}
