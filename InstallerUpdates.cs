using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SessionDashboard;

internal static class InstallerUpdates
{
    internal static Func<string> InstallTarget=()=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","nexa","Nexa.exe");
    internal static async Task<string> Prepare(StateStore store,string target,string candidate,string hash,string version)
    {
        var migrate=!NexaInstallation.IsInstalled(target);
        var portableSource=migrate?Path.GetFullPath(target):"";
        UpdateTransaction.ValidateBinary(target,UpdateTransaction.Hash(target));
        if(migrate && NexaInstallation.IsInstalled(InstallTarget()))throw new IOException("An installed nexa already exists. Open it from the Start menu to update; the portable copy was preserved.");
        if(migrate){target=InstallTarget();if(File.Exists(target))throw new IOException("An unregistered application occupies the install location. Run Setup manually to review it.");}
        UpdateTransaction.ValidateBinary(candidate,hash,version,"nexa installer");
        var root=Path.GetDirectoryName(Path.GetFullPath(target))!;
        var rollback=Path.Combine(root,"setup-cache","NexaSetup.exe");
        if(!migrate && !File.Exists(rollback))throw new IOException("The repair installer is missing. Rerun official Setup before updating.");
        if(!migrate)UpdateTransaction.ValidateBinary(rollback,UpdateTransaction.Hash(rollback),productName:"nexa installer");
        var directory=Path.Combine(store.UserPreferencesDirectory,"updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var free=new DriveInfo(Path.GetPathRoot(root)!).AvailableFreeSpace;
        var required=3L*(new FileInfo(candidate).Length+new FileInfo(migrate?portableSource:target).Length+(File.Exists(rollback)?new FileInfo(rollback).Length:0));
        if(free<required)throw new IOException("Not enough free disk space to install and retain rollback.");
        var staged=Path.Combine(directory,"candidate.exe");InstallerMaintenance.CopyVerified(candidate,staged);
        if(File.Exists(rollback))InstallerMaintenance.CopyVerified(rollback,Path.Combine(directory,"previous-setup.exe"));
        InstallerMaintenance.CopyVerified(migrate?portableSource:target,Path.Combine(directory,"previous.exe"));
        await UpdateTransaction.BackupData(store,directory);
        var plan=new UpdatePlan{Installer=true,MigratePortable=migrate,PortableSource=portableSource,UserId=store.UserId,Target=Path.GetFullPath(target),Candidate=staged,CandidateHash=hash,PreviousHash=UpdateTransaction.Hash(migrate?portableSource:target),PreviousSetupHash=File.Exists(rollback)?UpdateTransaction.Hash(rollback):"",Version=version,ParentPid=Environment.ProcessId};
        var path=Path.Combine(directory,"plan.json");UpdateTransaction.Save(path,plan);return path;
    }
    private static async Task Install(string setup,string target,string log,string? portable=null)
    {
        var info=new ProcessStartInfo(setup){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};
        foreach(var arg in new[]{"/CURRENTUSER","/VERYSILENT","/SUPPRESSMSGBOXES","/NORESTART","/UPGRADE=1","/DIR="+Path.GetDirectoryName(target),"/LOG="+log})info.ArgumentList.Add(arg);
        if(!string.IsNullOrWhiteSpace(portable)){info.ArgumentList.Add("/LEGACYSOURCE="+portable);info.ArgumentList.Add("/MERGETASKS=oldcopies");}
        using var process=Process.Start(info)??throw new IOException("Could not start official Setup.");
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(10));
        if(process.ExitCode!=0)throw new IOException("Setup failed with exit code "+process.ExitCode+". See "+log);
    }
    internal static async Task Apply(string path,UpdatePlan plan,Func<string,Task<bool>> health,Action<string>? restart,bool waitForParent)
    {
        var directory=Path.GetDirectoryName(Path.GetFullPath(path))!;
        if(plan.UserId!=System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value || !plan.MigratePortable && !NexaInstallation.IsInstalled(plan.Target) || !Path.GetFullPath(plan.Candidate).Equals(Path.Combine(directory,"candidate.exe"),StringComparison.OrdinalIgnoreCase))throw new UnauthorizedAccessException("Invalid installer-update ownership or target.");
        UpdateTransaction.RejectLinks(path);UpdateTransaction.RejectLinks(plan.Candidate);if(File.Exists(plan.Target))UpdateTransaction.RejectLinks(plan.Target);
        if(plan.MigratePortable && !Path.GetFullPath(plan.Target).Equals(Path.GetFullPath(InstallTarget()),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Unexpected migration install location.");
        if(plan.State=="Installed")return;
        if(plan.State is not ("Prepared" or "Installing"))throw new InvalidDataException("Installer update is no longer pending.");
        using var gate=new FileStream(Path.Combine(directory,".nexa-update.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        if(waitForParent)
        {try{using var parent=Process.GetProcessById(plan.ParentPid);await parent.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));}catch(ArgumentException){}}
        var previous=Path.Combine(directory,"previous-setup.exe");
        UpdateTransaction.ValidateBinary(plan.Candidate,plan.CandidateHash,plan.Version,"nexa installer");
        if(File.Exists(previous))UpdateTransaction.ValidateBinary(previous,plan.PreviousSetupHash,productName:"nexa installer");
        var replacing=false;
        try
        {
            if(plan.State=="Prepared")UpdateTransaction.ValidateBinary(plan.MigratePortable?plan.PortableSource:plan.Target,plan.PreviousHash);
            plan.State="Installing";UpdateTransaction.Save(path,plan);replacing=true;
            await Install(plan.Candidate,plan.Target,Path.Combine(directory,"install.log"),plan.MigratePortable?plan.PortableSource:null);
            if(!NexaInstallation.IsInstalled(plan.Target) || !await health(plan.Target))throw new IOException("Installed nexa failed its startup health check.");
            var installed=FileVersionInfo.GetVersionInfo(plan.Target);
            if(!Version.TryParse(installed.FileVersion,out var version)||version.ToString(3)!=plan.Version)throw new IOException("Installed binary version does not match the update.");
            plan.State="Installed";UpdateTransaction.Save(path,plan);
        }
        catch(Exception error)
        {
            plan.Error=error.Message;
            if(replacing)
            {
                try
                {
                    if(plan.MigratePortable && !File.Exists(previous))
                    {
                        UpdateTransaction.ValidateBinary(Path.Combine(directory,"previous.exe"),plan.PreviousHash);
                        if(NexaInstallation.IsInstalled(plan.Target))
                        {
                            var uninstaller=Directory.GetFiles(Path.GetDirectoryName(plan.Target)!,"unins*.exe").Single();
                            var remove=new ProcessStartInfo(uninstaller){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,ArgumentList={"/VERYSILENT","/SUPPRESSMSGBOXES","/NORESTART"}};
                            using var uninstall=Process.Start(remove)??throw new IOException("Could not roll back the new installation.");
                            await uninstall.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(3));
                            if(uninstall.ExitCode!=0 || NexaInstallation.IsInstalled(plan.Target))throw new IOException("The new installation could not be rolled back.");
                        }
                        if(File.Exists(plan.PortableSource))UpdateTransaction.ValidateBinary(plan.PortableSource,plan.PreviousHash);
                        else InstallerMaintenance.CopyVerified(Path.Combine(directory,"previous.exe"),plan.PortableSource);
                        plan.State="RolledBack";
                    }
                    else
                    {
                        await Install(previous,plan.Target,Path.Combine(directory,"rollback.log"));
                        UpdateTransaction.ValidateBinary(plan.Target,plan.PreviousHash);
                        if(!NexaInstallation.IsInstalled(plan.Target))throw new IOException("Previous installer registration was not restored.");
                        plan.State="RolledBack";
                    }

                }
                catch(Exception rollback){plan.State="RecoveryRequired";plan.Error+=" Rollback: "+rollback.Message;UpdateTransaction.Save(path,plan);throw;}
            }
            else plan.State="Failed";
            UpdateTransaction.Save(path,plan);throw;
        }
        restart?.Invoke(plan.Target);
    }
}
