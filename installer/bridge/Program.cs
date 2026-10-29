using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

// Compatibility asset for immutable legacy updaters. This is an installer
// bootstrapper, not a separate application edition. It has no feed,
// update polling, user saves, or separate version-selection logic.
internal static class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int MessageBox(nint owner,string text,string title,uint type);
    [STAThread] private static int Main(string[] args)
    {
        try
        {
            using var source=Assembly.GetExecutingAssembly().GetManifestResourceStream("nexa.official-setup")??throw new IOException("Official installer is missing.");
            var expected=Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>().Single(a=>a.Key=="InstallerSHA256").Value!;
            var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Nexa","installer-cache","0.1.1");
#if BRIDGE_TEST
            root=Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!,"bridge-cache");
#endif
            Directory.CreateDirectory(root);var setup=Path.Combine(root,"NexaSetup.exe");
            var temporary=setup+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var output=File.Create(temporary))source.CopyTo(output);
                using(var input=File.OpenRead(temporary))if(Convert.ToHexString(SHA256.HashData(input))!=expected)throw new InvalidDataException("Bundled installer checksum does not match.");
                var info=FileVersionInfo.GetVersionInfo(temporary);
                if(info.ProductName?.Trim()!="nexa installer" || info.FileVersion?.Trim()!="0.1.1.0")throw new InvalidDataException("Bundled installer identity does not match.");
                File.Move(temporary,setup,true);
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);}
            if(args.Length==2 && args[0]=="--verify-startup")
            {Directory.CreateDirectory(args[1]);File.WriteAllText(Path.Combine(args[1],"startup.json"),JsonSerializer.Serialize(new{Success=true,Version="0.1.1",Role="Legacy installer migration bridge",InstallerSHA256=expected}));return 0;}
            var start=new ProcessStartInfo(setup){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};
            start.ArgumentList.Add("/CURRENTUSER");start.ArgumentList.Add("/LEGACYSOURCE="+Environment.ProcessPath);
#if BRIDGE_TEST
            foreach(var arg in new[]{"/VERYSILENT","/SUPPRESSMSGBOXES","/NORESTART"})start.ArgumentList.Add(arg);
#endif
            using var launched=Process.Start(start)??throw new IOException("Setup could not be started.");return 0;
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {if(args.Length>0 && args[0]=="--verify-startup")return 1;MessageBox(0,"The official nexa installer could not start. Existing saves are unchanged. Download NexaSetup.exe from the official release.\n\n"+ex.Message,"nexa installer",0x10);return 1;}
    }
}
