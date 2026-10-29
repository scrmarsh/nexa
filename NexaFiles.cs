using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

internal static class NexaFiles
{
    private static string StandardStatePath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SessionDashboard","state.json");
    internal static string StatePath=>NexaInstallation.FixtureDataRoot is {} fixture?Path.Combine(fixture,"state.json"):StandardStatePath;
    internal static string Data(StateStore store)=>Path.GetDirectoryName(Path.GetFullPath(store.FilePath))!;
    internal static string Documents(StateStore store)=>Path.Combine(Path.GetFullPath(store.FilePath).Equals(Path.GetFullPath(StandardStatePath),StringComparison.OrdinalIgnoreCase)?Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments):Path.Combine(Data(store),"documents"),"nexa");
    internal static string Spaces(StateStore store)=>Path.Combine(Documents(store),"Spaces");
    internal static string History(StateStore store)=>Path.Combine(Documents(store),"History");
    internal static void Open(string folder)
    {Directory.CreateDirectory(folder);Process.Start(new ProcessStartInfo(folder){UseShellExecute=true})?.Dispose();}
    internal static int WriteHistory(StateStore store)
    {
        var (entries,warning)=HistoryArchive.Read(store);
        if(warning!=null && !warning.StartsWith("This save has no recorded session archive",StringComparison.Ordinal))throw new InvalidDataException(warning);
        var folder=History(store);Directory.CreateDirectory(folder);
        var count=0;
        foreach(var entry in entries)
        {
            if(entry.Session==null || entry.Problem!=null)continue;
            var name=entry.DashboardId.ToString("N")+"-"+entry.Session.SessionId.ToString("N")+".md";
            StateStore.AtomicWrite(Path.Combine(folder,name),"# "+entry.Space+"\n\n"+entry.Report,false);count++;
        }
        StateStore.AtomicWrite(Path.Combine(folder,"README.txt"),"nexa history documents\n\nThese readable copies may contain private notes and transcripts. Share only the files you intend to share. The private application save remains authoritative. Refresh copies using Files & sharing in nexa. Deleting these copies does not delete the original saved sessions.\n",false);
        return count;
    }
}

internal sealed partial class SettingsDialog
{
    private UIElement BuildFiles()
    {
        var page=Page("Files & sharing","Private saves stay in your Windows account. Exported Spaces and readable history documents are easy to find in Documents → nexa.");
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)};
        void Action(string label,Action run)
        {
            var button=new Button{Content=label,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,8,0,0)};
            button.Click+=(_,_)=>{try{if(!FlushPendingChanges())return;run();}catch(Exception ex)when(HistoryArchive.Recoverable(ex)||ex is System.ComponentModel.Win32Exception){status.Text="Could not open these files: "+ex.Message;}};page.Children.Add(button);
        }
        Action("Open Data Folder",()=>NexaFiles.Open(NexaFiles.Data(store)));
        Action("Open Spaces Folder",()=>NexaFiles.Open(NexaFiles.Spaces(store)));
        Action("Open History Folder",()=>{var count=NexaFiles.WriteHistory(store);status.Text=$"Updated {count} readable history documents.";NexaFiles.Open(NexaFiles.History(store));});
        page.Children.Add(new TextBlock{Text="To copy or send a Space, open its menu → Export Space. Configuration only is the default; include session notes only when you intend to share them. Integration credentials are excluded. History documents may contain private notes and transcripts.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,18,0,0)});
        page.Children.Add(status);return new ScrollViewer{Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    }
}
