using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Aevoria.Desktop;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckAevoriaA()
    {
        var normal=new StateStore(Path.Combine(output,"nexa","state.json"));Directory.CreateDirectory(Path.GetDirectoryName(normal.FilePath)!);normal.Save(new AppState());normal.SaveBugPreferences(new(true,BugSpecies.Spiders,BugDensity.Arcade));
        var selection=Path.Combine(LivingWorlds.Root(normal),"selection.json");LivingFiles.Save(selection,new ArcadeGameSelection(ArcadeGame.LivingWorlds,Guid.NewGuid()));var selectionBytes=File.ReadAllBytes(selection);
        var home=new HomeWindow(normal){Width=1100,Height=800};home.Show();Pump(TimeSpan.FromMilliseconds(100));
        var button=home.AevoriaButton;Assert(AutomationProperties.GetName(button)=="Try Aevoria" && button.IsTabStop,"Home launcher exposes an accessible keyboard-focusable Try Aevoria button");
        foreach(var (w,h) in new[]{(1100d,800d),(740d,550d)})
        {home.Width=w;home.Height=h;home.UpdateLayout();Pump(TimeSpan.FromMilliseconds(60));var bounds=button.TransformToAncestor(home).TransformBounds(new Rect(button.RenderSize));Assert(Math.Abs(bounds.X+bounds.Width/2-home.ActualWidth/2)<5 && bounds.Bottom<home.ActualHeight && bounds.Top>home.ActualHeight/2,$"Try Aevoria remains centered and visible at {w}×{h}");}
        Save018(home,(int)home.ActualWidth,(int)home.ActualHeight,"aevoria-home-minimum");home.Width=1100;home.Height=800;Pump(TimeSpan.FromMilliseconds(60));Save018(home,1100,800,"aevoria-home");
        Assert(!Descendants(home).OfType<Button>().Any(b=>Equals(b.Content,"Arcade")),"Home has no duplicate historical Arcade chooser entry");
        var menuLibrary=new AevoriaLibrary(Path.Combine(output,"game-menu"));GameMenu? menu=null;home.LaunchAevoria=()=>{menu=new GameMenu(menuLibrary);menu.Show();};button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(TimeSpan.FromMilliseconds(80));
        Assert(menu!=null && menu.Title.Contains("Aevoria") && menu.Game==null && menuLibrary.Playing==null,"Home button opens the Aevoria menu without loading a world");
        Assert(new[]{"Continue World","New World","Load World","Civilization Atlas","Settings","Exit Game"}.All(s=>Descendants(menu!).OfType<Button>().Any(b=>Equals(b.Content,s))),"Independent menu exposes six actual game actions");
        Assert(!Application.Current.Windows.OfType<MainWindow>().Any(),"Aevoria menu requires no Session Dashboard instance");
        Assert(typeof(GameMenu).Assembly.GetName().Name=="Aevoria" && typeof(App).Assembly.GetName().Name=="Nexa","Game and Nexa retain separate assembly identities");
        Save018(menu!,(int)menu!.ActualWidth,(int)menu.ActualHeight,"aevoria-main-menu");menu.Close();
        var settings=new SettingsDialog(home,normal,normal.Load(),null,"Add Bugs");settings.Show();Pump(TimeSpan.FromMilliseconds(40));
        var choices=Descendants(settings).OfType<RadioButton>().Select(b=>b.Content?.ToString()).ToArray();
        Assert(new[]{"Low","Medium","High","Arcade"}.All(s=>choices.Count(c=>c==s)==1) && !choices.Contains("Normal") && !choices.Contains("Infestation"),"Traditional settings show exactly the four requested intensity labels");
        Assert(!Descendants(settings).OfType<Button>().Any(b=>b.Content?.ToString()?.Contains("LIVING WORLDS")==true || b.Content?.ToString()?.Contains("Aevoria")==true),"Traditional settings provide no Aevoria or historical-game chooser");settings.FlushPendingChanges();settings.Close();
        var main=new MainWindow(normal){Width=1000,Height=720};main.Show();Pump(TimeSpan.FromMilliseconds(100));
        foreach(var density in Enum.GetValues<BugDensity>())
        {main.ApplyBugPreferences(new(true,BugSpecies.Ants,density));Assert(main.livingWorld==null && main.bugsOverlay!.TerrainView==null && main.bugsOverlay.Manager.Civilization==null,$"Nexa {TraditionalBugModes.Label(density)} never loads historical simulation state");}
        var classic=BattlefieldWindow.ShowFor(home,normal);Pump(TimeSpan.FromMilliseconds(60));Assert(classic.Living==null && classic.World==ArcadeWorlds.For(normal),"The tiny play-with-bugs path resumes classic Colony Wars despite an old Living Worlds selection");classic.Close();main.Close();
        Assert(selectionBytes.SequenceEqual(File.ReadAllBytes(selection)),"Traditional mode compatibility preserves the old selection and its worlds without migrating them");home.Close();
        normal.SaveBugPreferences(new(true,BugSpecies.Spiders,BugDensity.Arcade));
        var preferenceFile=Directory.EnumerateFiles(Path.GetDirectoryName(normal.FilePath)!,"*preferences*.json",SearchOption.AllDirectories).First(p=>File.ReadAllText(p).Contains("BugDensity"));var values=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(preferenceFile))!;values["BugDensity"]="LivingWorlds";values["UnrelatedSetting"]="keep";File.WriteAllText(preferenceFile,JsonSerializer.Serialize(values));
        Assert(normal.LoadBugPreferences().Density==BugDensity.Normal && normal.LoadBugPreferences().Species==BugSpecies.Spiders,"Unrecognized old historical mode values map to Medium while retaining species");Assert(File.ReadAllText(preferenceFile).Contains("UnrelatedSetting"),"Compatibility reads preserve unrelated preferences");
        var library=new AevoriaLibrary(Path.Combine(output,"aevoria-library"));var worlds=new List<LivingWorldSession>();
        for(var i=0;i<10;i++)worlds.Add(Complete023(library.Create("World "+i,"",512,512)));
        Assert(library.Worlds().Length==10 && worlds.Select(w=>w.Info.WorldId).Distinct().Count()==10 && worlds.Select(w=>w.Controller.SavePath).Distinct().Count()==10,"Ten library entries are ten independent worlds, not ten snapshots");
        Assert(worlds.All(w=>!string.IsNullOrWhiteSpace(w.Info.Seed)) && worlds.Select(w=>w.Info.Seed).Distinct().Count()==10,"Blank seeds resolve to distinct persistent seeds");
        Assert(worlds.All(w=>w.Controller.SavePath.StartsWith(library.Root,StringComparison.OrdinalIgnoreCase) && w.Civilization!=null && !w.Controller.Running),"Each world has isolated civilization/history storage and no menu-time simulation");
        bool full=false;try{Complete023(library.Create("Eleventh","limit",512,512));}catch(InvalidOperationException){full=true;}Assert(full && library.Worlds().Length==10,"World eleven is refused without deleting any existing world");
        var first=worlds[0];library.Rename(first.Info.WorldId,"Renamed world");Assert(library.Worlds().Single(w=>w.WorldId==first.Info.WorldId).Name=="Renamed world" && first.Info.Name=="Renamed world","Rename preserves world identity and updates cached metadata");
        var session=library.Open(first.Info.WorldId);Assert(ReferenceEquals(session,library.Open(first.Info.WorldId)) && ReferenceEquals(session.Controller,first.Controller),"Repeated opens reuse one authoritative simulation");
        first.Info.Quality=TerrainQuality.Performance;first.SaveMetadata();
        var window=new GameMenu(library);window.Show();window.Play(first.Info.WorldId);Pump(TimeSpan.FromMilliseconds(120));Assert(first.Info.Quality==TerrainQuality.Performance,"Playing preserves the world's saved advanced terrain quality");var running=window.Game;window.Play(first.Info.WorldId);Assert(ReferenceEquals(window.Game,running) && !Application.Current.Windows.OfType<MainWindow>().Any(),"Playing twice reuses a single game window without a dashboard");
        running!.Close();var clock=first.Controller.Manager.MatchAge;Pump(TimeSpan.FromMilliseconds(160));Assert(!first.Controller.Running && first.Controller.Manager.MatchAge==clock && window.Game==null,"Closing gameplay stops simulation and returns to the independent menu");window.Close();
        var backup=Path.Combine(library.Root,"full-world.zip");library.Backup(first.Info.WorldId,backup);using(var zip=ZipFile.OpenRead(backup))Assert(zip.Entries.Any(e=>e.FullName=="world.json") && zip.Entries.Any(e=>e.FullName.StartsWith("civilization-history/")),"Full-world backup includes the durable historical archive");
        var deleted=library.Delete(first.Info.WorldId);Assert(!Directory.Exists(library.WorldPath(first.Info.WorldId)) && Directory.Exists(Path.Combine(deleted,"civilization-history")),"Confirmed deletion is reversible and retains the whole world/history in DeletedWorlds");
        var restored=Complete023(library.Import(backup,true));var loaded=library.Open(restored);Until023(()=>loaded.PoliticsReady.IsCompleted,"Restored full-world history is initialized");Assert(restored==first.Info.WorldId && loaded.Civilization!=null && loaded.Civilization.State.Sequence==first.Civilization!.State.Sequence,"Full-world backup restores stable identities and usable history");
        var destination=new AevoriaLibrary(Path.Combine(output,"copied-library"));var source=library.WorldPath(restored);var hash=System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(source,"arcade-world.json")));
        Complete023(destination.Import(source));var afterImport=System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(source,"arcade-world.json")));Assert(hash.SequenceEqual(afterImport),"Explicit import copies an experimental world without altering its original snapshot");
        Assert(destination.Worlds().Single().WorldId==restored && Path.GetFullPath(destination.Root)!=Path.GetFullPath(Path.GetDirectoryName(normal.FilePath)!),"Imported worlds stay in the independent game library");
        library.SavePreferences(new(TerrainQuality.Performance,30));Assert(library.Preferences()==new AevoriaPreferences(TerrainQuality.Performance,30),"Aevoria game settings persist independently");
        var executable=AevoriaLauncher.Resolve(Path.GetFullPath("bin/Release/net10.0-windows"));Assert(File.Exists(executable),"Nexa development launcher resolves the built standalone executable");
    }
}
