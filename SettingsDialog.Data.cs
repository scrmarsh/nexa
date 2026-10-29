using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
    private UIElement DataActions()
    {
        var buttons=new WrapPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,24,0,0)};
        var export=new Button{Content="EXPORT ALL DATA",Margin=new Thickness(0,0,10,0)};
        var delete=new Button{Content="DELETE ALL DATA",Style=(Style)FindResource("DestructiveButton")};
        export.Click+=async(_,_)=>await ExportAll();delete.Click+=(_,_)=>ConfirmDelete();buttons.Children.Add(export);buttons.Children.Add(delete);return buttons;
    }
    private async Task ExportAll(Window? backupOwner=null)
    {
        try{await ExportAllCore(backupOwner??this);}
        catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {MessageBox.Show(backupOwner??this,"The backup could not start. Your existing data is unchanged.\n\n"+ex.Message,"nexa backup",MessageBoxButton.OK,MessageBoxImage.Warning);}
    }
    private async Task ExportAllCore(Window backupOwner)
    {
        if(!FlushPendingChanges())return;
        foreach(var window in Application.Current.Windows.OfType<MainWindow>().Where(w=>w.SettingsStore.FilePath==store.FilePath))
            if(!window.CheckpointForBackup())return;
        if(!NexaEcosystem.For(store).Save())throw new IOException("The ecosystem could not be checkpointed.");
        var picker=new SaveFileDialog{Title="Export all nexa data",FileName="Nexa-Backup-"+DateTime.Now.ToString("yyyy-MM-dd-HHmmss")+".zip",Filter="nexa backup (*.zip)|*.zip",AddExtension=true,OverwritePrompt=true};
        if(picker.ShowDialog(backupOwner)!=true)return;
        var dialog=new NexaDialog{Owner=backupOwner,Title="Export all data",Width=520,Height=230,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var panel=new StackPanel{Margin=new Thickness(24)};var status=new TextBlock{Text="Preparing a consistent local backup…",TextWrapping=TextWrapping.Wrap};panel.Children.Add(status);
        var cancel=new Button{Content="Cancel",HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,18,0,0)};panel.Children.Add(cancel);dialog.Content=panel;
        using var cancellation=new CancellationTokenSource();cancel.Click+=(_,_)=>cancellation.Cancel();dialog.Closing+=(_,_)=>cancellation.Cancel();dialog.Show();
        var progress=new Progress<string>(text=>status.Text=text);
        try
        {
            var count=await Task.Run(()=>NexaData.Export(NexaFiles.Data(store),picker.FileName,cancellation.Token,progress));
            status.Text=$"Backup verified: {count} files. Integration credentials were excluded. Reconnect integrations after recovery.";
        }
        catch(OperationCanceledException){status.Text="Export canceled. No backup was published.";}
        catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException){status.Text="Export failed: "+ex.Message;}
        cancel.Content="Close";cancel.Click+=(_,_)=>dialog.Close();
    }
    private void ConfirmDelete()
    {
        var dialog=new NexaDialog{Owner=this,Title="Delete all local data",Width=560,Height=430,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var consent=new NexaDeletionConsent();dialog.Closed+=(_,_)=>consent.Cancel();
        var page=new StackPanel{Margin=new Thickness(24)};dialog.Content=page;
        page.Children.Add(new TextBlock{Text="Delete all local nexa data?",FontSize=24,FontWeight=FontWeights.SemiBold});
        page.Children.Add(new TextBlock{Text="This permanently deletes local Spaces, sessions, notes, settings, history, recordings, transcripts, backups and locally stored account tokens. Connected cloud content and application files are preserved. Export a backup first. Preserved legacy installations are outside this reset.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,14,0,14)});
        var export=new Button{Content="Export all data first",HorizontalAlignment=HorizontalAlignment.Left};export.Click+=async(_,_)=>await ExportAll(dialog);page.Children.Add(export);
        var confirmation=new TextBox{Margin=new Thickness(0,16,0,8)};Field(page,"Type CONFIRM to continue",confirmation);
        var next=new Button{Content="Continue",IsEnabled=false,HorizontalAlignment=HorizontalAlignment.Right};page.Children.Add(next);
        confirmation.TextChanged+=(_,_)=>next.IsEnabled=confirmation.Text=="CONFIRM";
        next.Click+=(_,_)=>
        {
            if(!consent.Begin(confirmation.Text,DateTimeOffset.UtcNow))return;
            page.Children.Clear();var deadline=consent.Deadline!.Value;var running=false;var finished=false;
            dialog.Closing+=(_,e)=>{if(running && !finished)e.Cancel=true;else if(finished)Dispatcher.BeginInvoke(new Action(()=>Application.Current.Shutdown()));};
            var text=new TextBlock{FontSize=22,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,22)};page.Children.Add(text);
            var actions=new WrapPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};page.Children.Add(actions);
            var now=new Button{Content="Delete Now",Margin=new Thickness(0,0,10,0)};var cancel=new Button{Content="Cancel"};actions.Children.Add(now);actions.Children.Add(cancel);
            var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};
            async Task Delete(bool immediate=false)
            {
                if(running || !dialog.IsVisible || !consent.Authorize(DateTimeOffset.UtcNow,immediate))return;running=true;timer.Stop();now.IsEnabled=cancel.IsEnabled=false;
                try
                {
                    if(!FlushPendingChanges())throw new IOException("Pending settings could not be saved.");
                    foreach(var window in Application.Current.Windows.OfType<MainWindow>().Where(w=>w.SettingsStore.FilePath==store.FilePath))
                        if(!window.CloseDataForDeletion())throw new IOException("Recording or workspace checkpoint failed. Nothing was deleted.");
                    NexaEcosystem.For(store).Dispose();
                    var errors=await Task.Run(()=>NexaData.Delete(NexaFiles.Data(store)));
                    text.Text=errors.Length==0?"Local data deleted. Close nexa and launch it again for a fresh workspace.":"Some data could not be deleted. Close nexa before retrying.\n"+string.Join("\n",errors.Take(5));
                    finished=true;cancel.Content="Close nexa";cancel.IsEnabled=true;cancel.Click+=(_,_)=>Application.Current.Shutdown();
                }
                catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException){text.Text="Deletion stopped: "+ex.Message;finished=true;cancel.Content="Close nexa";cancel.IsEnabled=true;cancel.Click+=(_,_)=>Application.Current.Shutdown();}
            }
            void Tick(){var seconds=Math.Max(0,(int)Math.Ceiling((deadline-DateTimeOffset.UtcNow).TotalSeconds));text.Text=$"Nexa will delete your local data in {seconds} seconds.";if(seconds==0)_=Delete();}
            timer.Tick+=(_,_)=>Tick();dialog.Closed+=(_,_)=>timer.Stop();now.Click+=async(_,_)=>await Delete(true);cancel.Click+=(_,_)=>{if(!running)dialog.Close();};Tick();timer.Start();
        };
        dialog.ShowDialog();
    }
}

public partial class MainWindow
{
    internal bool CheckpointForBackup()=>StopLocalRecording(true) && SaveNow();
    internal bool CloseDataForDeletion()
    {
        if(!CheckpointForBackup())return false;
        autosave.Stop();saveDelay.Stop();PauseHedySync();boredomTicker.Stop();
        closing=true;closingAfterTransition=true;ready=false;dirty=false;saveTicket++;
        pendingWrite?.GetAwaiter().GetResult();CloseNoteEditors();return true;
    }
}
