using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    private UIElement BuildUpdates()
    {
        var page=Page("Updates","Update notices are always enabled. Choose automatic installation or install directly from a notice.");
        page.Children.Add(new TextBlock{Text="Current version: "+App.VersionLabel,FontSize=16,Margin=new Thickness(0,2,0,14)});
        var service=AutomaticUpdates.For(store);var flow=service.Flow;
        var preferences=store.LoadUpdatePreferences();
        var automatic=new CheckBox{Content="Automatically download updates and restart after a 30-second countdown",IsChecked=preferences.AutomaticUpdates && preferences.UpdateChoiceMade};page.Children.Add(automatic);
        var error=new TextBlock{TextWrapping=TextWrapping.Wrap};page.Children.Add(error);
        automatic.Click+=(_,_)=>
        {
            try{var latest=store.LoadUpdatePreferences();latest.AutomaticUpdates=automatic.IsChecked==true;latest.UpdateChoiceMade=true;latest.CheckOnStartup=true;store.SaveUpdatePreferences(latest);if(!latest.AutomaticUpdates)flow.AutomaticDisabled();}
            catch(Exception ex)when(ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException){error.Text="Could not save update choice: "+ex.Message;}
        };
        var check=new Button{Content="Check for updates",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,12,0,0)};page.Children.Add(check);
        var status=new TextBlock{Text=string.IsNullOrWhiteSpace(preferences.LastCheckError)?flow.Status:"Last check: "+preferences.LastCheckError,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,14,0,10)};page.Children.Add(status);
        var notes=new TextBox{IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Height=180,Visibility=Visibility.Collapsed};page.Children.Add(notes);
        var install=new Button{Content="Install update",HorizontalAlignment=HorizontalAlignment.Left,Visibility=Visibility.Collapsed};page.Children.Add(install);
        var restart=new Button{Content="Restart now",HorizontalAlignment=HorizontalAlignment.Left,Visibility=Visibility.Collapsed,Margin=new Thickness(0,8,0,0)};page.Children.Add(restart);
        void Refresh()
        {
            status.Text=flow.Status;check.IsEnabled=!flow.Busy && !flow.Restarting;
            if(flow.Available is {} release){notes.Text=release.Name+"\n\n"+release.Notes;notes.Visibility=Visibility.Visible;}
            install.Visibility=flow.Available!=null && flow.Prepared==null?Visibility.Visible:Visibility.Collapsed;install.IsEnabled=!flow.Busy;
            restart.Visibility=flow.Prepared!=null?Visibility.Visible:Visibility.Collapsed;restart.IsEnabled=!flow.Busy && !flow.Restarting;
        }
        flow.Changed+=Refresh;Closed+=(_,_)=>flow.Changed-=Refresh;
        check.Click+=async(_,_)=>await flow.CheckAsync();install.Click+=async(_,_)=>await flow.InstallAsync();restart.Click+=async(_,_)=>await flow.RestartNowAsync();
        var repairPanel=new StackPanel{HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,28,0,0)};
        repairPanel.Children.Add(new TextBlock{Text="Having trouble with nexa?",FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Right});
        repairPanel.Children.Add(new TextBlock{Text="Try reinstalling and cleaning up files.",TextAlignment=TextAlignment.Right,Margin=new Thickness(0,4,0,10)});
        var repair=new Button{Content="Rerun Installer",HorizontalAlignment=HorizontalAlignment.Right};repairPanel.Children.Add(repair);page.Children.Add(repairPanel);
        var launching=false;
        repair.Click+=async(_,_)=>
        {
            if(launching)return;launching=true;repair.IsEnabled=false;
            try
            {
                status.Text="Verifying the official installer…";var setup=await InstallerRepair.Resolve(store);
                if(!FlushPendingChanges())return;
                foreach(var window in Application.Current.Windows.OfType<MainWindow>().Where(w=>w.SettingsStore.FilePath==store.FilePath).ToArray())if(!window.PrepareForUpdateRestart())return;
                NexaEcosystem.CheckpointAll();
                var start=new System.Diagnostics.ProcessStartInfo(setup){UseShellExecute=false};start.ArgumentList.Add("/CURRENTUSER");
                if(NexaInstallation.IsInstalled())start.ArgumentList.Add("/DIR="+System.IO.Path.GetDirectoryName(Environment.ProcessPath));
                using var process=System.Diagnostics.Process.Start(start)??throw new System.IO.IOException("The installer could not start.");
                Application.Current.Shutdown();
            }
            catch(Exception ex)when(ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or System.Net.Http.HttpRequestException or System.Text.Json.JsonException or System.ComponentModel.Win32Exception or InvalidOperationException){status.Text="Could not rerun the installer: "+ex.Message;}
            finally{launching=false;repair.IsEnabled=true;}
        };
        return new ScrollViewer{Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    }
}
