using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckInstallerWait035()
    {
        var name=@"Local\nexa.installer-fixture."+Guid.NewGuid().ToString("N");
        using(var held=new Mutex(false,name))
        {
            var expired=false;try{InstallerUpdates.WaitForSetup(name,TimeSpan.Zero).GetAwaiter().GetResult();}catch(TimeoutException){expired=true;}
            Assert(expired,"Installer busy wait has a bounded, actionable timeout");
            var wait=InstallerUpdates.WaitForSetup(name,TimeSpan.FromSeconds(5));
            Assert(!wait.IsCompleted,"Installer retry waits while a cleanup mutex still exists");
            held.Dispose();wait.GetAwaiter().GetResult();
            Assert(wait.IsCompletedSuccessfully,"Installer retry resumes only after cleanup releases its mutex");
        }
    }
    private static void Setup035(string setup,string root)
    {
        var info=new ProcessStartInfo(setup){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};
        foreach(var arg in new[]{"/CURRENTUSER","/VERYSILENT","/SUPPRESSMSGBOXES","/NORESTART","/LOG="+Path.Combine(root,"native-"+Guid.NewGuid().ToString("N")+".log")})info.ArgumentList.Add(arg);
        using var child=Process.Start(info)!;
        if(!child.WaitForExit(180000))throw new TimeoutException("Native installer fixture timed out.");
        Assert(child.ExitCode==0,"Native fixture Setup/uninstaller completes successfully");
    }
    private static string? Registered035()
    {using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\"+NexaInstallation.TestAppId+"_is1");return key?.GetValue("DisplayVersion") as string;}
    private static void CheckInstaller035(string baseline,string candidate,string root,string bridge)
    {
        baseline=Path.GetFullPath(baseline);candidate=Path.GetFullPath(candidate);root=Path.GetFullPath(root);bridge=Path.GetFullPath(bridge);
        Assert(UpdateTransaction.IsWithin(Path.GetFullPath("verification"),root) && Path.GetFileName(root).Contains("fixture",StringComparison.OrdinalIgnoreCase),"Installer-update tests are limited to an isolated fixture");
        Assert(Registered035()==null,"No existing test registration is overwritten");Directory.CreateDirectory(root);
        Setup035(baseline,root);
        var target=Path.Combine(root,"app","Nexa.exe");var store=new StateStore(Path.Combine(root,"data","state.json"));store.Save(new AppState{Notes="Preserve actual installer update data"});var bytes=File.ReadAllText(store.FilePath);
        Assert(NexaInstallation.IsInstalled(target) && Registered035()=="0.0.34","Prior-version fixture has genuine .34 registration and binary metadata");
        var previousHash=UpdateTransaction.Hash(target);var plan=InstallerUpdates.Prepare(store,target,candidate,UpdateTransaction.Hash(candidate),"0.1.2").GetAwaiter().GetResult();
        Assert(JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!.Installer && File.Exists(Path.Combine(Path.GetDirectoryName(plan)!,"previous-setup.exe")),"Installer update stages the verified candidate and rollback Setup");
        var rejected=false;try{UpdateTransaction.Apply(plan,_=>Task.FromResult(false),waitForParent:false).GetAwaiter().GetResult();}catch(IOException){rejected=true;}
        Assert(rejected && JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!.State=="RolledBack","Failed installed-app health runs the prior Setup and records rollback");
        Assert(UpdateTransaction.Hash(target)==previousHash && Registered035()=="0.0.34","Rollback restores exact application bytes and Installed Apps version");
        Assert(File.ReadAllText(store.FilePath)==bytes,"Failed installer upgrade preserves exact user-data bytes");
        plan=InstallerUpdates.Prepare(store,target,candidate,UpdateTransaction.Hash(candidate),"0.1.2").GetAwaiter().GetResult();
        UpdateTransaction.Apply(plan,p=>UpdateTransaction.Health(p,Path.Combine(root,"upgraded-health")),waitForParent:false).GetAwaiter().GetResult();
        Assert(Registered035()=="0.1.2" && FileVersionInfo.GetVersionInfo(target).FileVersion=="0.1.2.0","Successful upgrade advances registered and executable versions together");
        Assert(File.ReadAllText(store.FilePath)==bytes && NexaInstallation.IsInstalled(target),"Successful upgrade preserves data and registered installation identity");
        var cached=InstallerRepair.Cached(target);Assert(cached!=null && File.Exists(cached),"Rerun Installer resolves the installed digest-verified cache");
        var cachedBytes=File.ReadAllBytes(cached!);try{File.WriteAllText(cached!,"corrupt fixture cache");var refused=false;try{InstallerRepair.Cached(target);}catch(Exception ex)when(ex is IOException or InvalidDataException){refused=true;}Assert(refused,"Rerun Installer refuses a corrupt cached Setup");}finally{File.WriteAllBytes(cached!,cachedBytes);}

        Assert(File.Exists(Path.Combine(root,"desktop","nexa.lnk")) && File.Exists(Path.Combine(root,"start-menu","nexa.lnk")),"Installer upgrade retains desktop and Start menu shortcuts");
        UpdateTransaction.Apply(plan,_=>throw new InvalidOperationException("Must not replay"),waitForParent:false).GetAwaiter().GetResult();Assert(true,"Completed installer upgrade is idempotent");
        Setup035(Directory.GetFiles(Path.Combine(root,"app"),"unins*.exe").Single(),root);
        foreach(var tag in new[]{"v0.0.29-alpha","v0.0.31-alpha"})
        {
            var old=Path.GetFullPath(Path.Combine("verification","release-audit033",tag,"Nexa.exe"));
            var portableRoot=Path.Combine(root,"legacy",tag);Directory.CreateDirectory(portableRoot);var portable=Path.Combine(portableRoot,"Nexa.exe");File.Copy(old,portable,true);
            var helper=Path.Combine(root,"legacy-helper-"+tag+"-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(old,helper);
            var legacyPlan=UpdateTransaction.Prepare(store,portable,bridge,UpdateTransaction.Hash(bridge),"0.1.2").GetAwaiter().GetResult();
            var prepared=JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(legacyPlan))!;prepared.ParentPid=int.MaxValue;UpdateTransaction.Save(legacyPlan,prepared);
            var info=new ProcessStartInfo(helper){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,ArgumentList={"--apply-update",legacyPlan}};
            using(var child=Process.Start(info)!){if(!child.WaitForExit(180000))throw new TimeoutException("Legacy helper did not finish.");Assert(child.ExitCode==0,"Unmodified "+tag+" helper accepts the migration bridge");}
            var watch=Stopwatch.StartNew();while((Registered035()!="0.1.2") && watch.Elapsed<TimeSpan.FromMinutes(3))Thread.Sleep(200);
            Assert(Registered035()=="0.1.2" && File.Exists(portable),"Legacy "+tag+" restart installs nexa while retaining the standalone migration copy");
            Assert(File.ReadAllText(store.FilePath)==bytes,"Legacy "+tag+" migration preserves user data");
            Assert(JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(legacyPlan))!.State=="Installed","Legacy "+tag+" helper records a durable installed checkpoint");
        }
        Setup035(Directory.GetFiles(Path.Combine(root,"app"),"unins*.exe").Single(),root);
        Assert(Registered035()==null && File.Exists(store.FilePath),"Final fixture uninstall clears registration and keeps saves");
        var resolver=InstallerUpdates.InstallTarget;InstallerUpdates.InstallTarget=()=>target;
        try
        {
            var source=Path.Combine(root,"portable-current","nexa-portable.exe");Directory.CreateDirectory(Path.GetDirectoryName(source)!);File.Copy(Path.GetFullPath("verification/release035-final/Application/Nexa.exe"),source,true);var portableHash=UpdateTransaction.Hash(source);
            plan=InstallerUpdates.Prepare(store,source,candidate,UpdateTransaction.Hash(candidate),"0.1.2").GetAwaiter().GetResult();
            Assert(JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!.MigratePortable,"Current portable mode stages a real installer migration with recovery data");
            rejected=false;try{UpdateTransaction.Apply(plan,_=>Task.FromResult(false),waitForParent:false).GetAwaiter().GetResult();}catch(IOException){rejected=true;}
            Assert(rejected && Registered035()==null && UpdateTransaction.Hash(source)==portableHash && JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!.State=="RolledBack","Failed first installer migration removes only the new test installation and restores the exact portable executable");
            Assert(File.ReadAllText(store.FilePath)==bytes,"Failed first migration preserves exact private data bytes");
            plan=InstallerUpdates.Prepare(store,source,candidate,UpdateTransaction.Hash(candidate),"0.1.2").GetAwaiter().GetResult();
            UpdateTransaction.Apply(plan,p=>UpdateTransaction.Health(p,Path.Combine(root,"portable-migration-health")),waitForParent:false).GetAwaiter().GetResult();
            Assert(Registered035()=="0.1.2" && File.Exists(source) && JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!.State=="Installed","Current portable migration installs registered nexa and preserves its portable source");
            Assert(File.ReadAllText(store.FilePath)==bytes,"Successful current portable migration preserves user data");
            Setup035(Directory.GetFiles(Path.Combine(root,"app"),"unins*.exe").Single(),root);
            Assert(Registered035()==null,"Current portable migration fixture removes its test registration afterward");
        }
        finally{InstallerUpdates.InstallTarget=resolver;}

    }
}
