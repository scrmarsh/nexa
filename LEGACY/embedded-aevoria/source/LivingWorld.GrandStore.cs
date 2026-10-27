using System.IO;
using System.Windows;
namespace SessionDashboard;
internal static partial class LivingWorlds
{
    private static readonly SemaphoreSlim grandWorker=new(1,1);
    internal static async Task<LivingWorldSession> CreateGrand(StateStore store,string name,string seed,int width,int height,GrandGeneration settings,IProgress<string>? progress=null,CancellationToken cancel=default)
    {
        settings.Validate();var info=new LivingWorldInfo{Name=string.IsNullOrWhiteSpace(name)?"A new age":name.Trim(),Seed=string.IsNullOrWhiteSpace(seed)?Guid.NewGuid().ToString("N"):seed.Trim(),Tiles=width,TilesY=height,GeneratorVersion=3,Generation=settings,CivilizationMode=true};info.Validate();
        var root=Root(store);var staging=Path.Combine(root,".creating-"+info.WorldId.ToString("N"));var destination=Path.Combine(root,info.WorldId.ToString("N"));
        await grandWorker.WaitAsync(cancel);
        try
        {
            var prepared=await Task.Run(()=>
            {
                cancel.ThrowIfCancellationRequested();progress?.Report("Shaping continents and connected rivers…");var terrain=new LivingTerrain(info);
                cancel.ThrowIfCancellationRequested();progress?.Report("Indexing traversable land…");var land=new PoliticalLand(terrain,cancel);
                cancel.ThrowIfCancellationRequested();progress?.Report("Founding six civilizations…");var manager=new BugManager(23){WorldId=info.WorldId,LivingGeneratorVersion=3};
                manager.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade,new ArcadeSettings{HardCap=settings.PopulationBudget,SoftCap=Math.Min(150,settings.PopulationBudget-1)}));manager.InitializeInfiniteWorld(new Size(info.Extent,info.HeightExtent));manager.SeedLivingColonies(terrain);
                manager.Nations=LivingNationWorld.Initialize(land,manager,true,kingdoms:true);manager.PlacePoliticalHomes(manager.Nations);
                cancel.ThrowIfCancellationRequested();manager.Civilization=LivingCivilization.Create(manager,staging);
                manager.Nations.EnableExpansion();
                var home=manager.Colonies.Values.First().Territory;info.FocusX=home.X;info.FocusY=home.Y;
                progress?.Report("Saving the new world and history…");LivingFiles.Save(Path.Combine(staging,"world.json"),info);new ArcadeWorldStore(Path.Combine(staging,"arcade-world.json")).Save(manager.CaptureWorld());
                cancel.ThrowIfCancellationRequested();Directory.Move(staging,destination);return (terrain,land);
            },cancel);
            return await Application.Current.Dispatcher.InvokeAsync(()=>{var world=Open(store,info.WorldId,prepared.terrain,prepared.land);Select(store,new(ArcadeGame.LivingWorlds,info.WorldId));return world;});
        }
        finally
        {
            // Preserve incomplete/cancelled data outside the selectable world library.
            try{if(Directory.Exists(staging)){var recovery=Path.Combine(Path.GetDirectoryName(root)!,"IncompleteWorlds");Directory.CreateDirectory(recovery);Directory.Move(staging,Path.Combine(recovery,Path.GetFileName(staging)));}}finally{grandWorker.Release();}
        }
    }
}
