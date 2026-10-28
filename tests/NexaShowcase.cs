using System.Diagnostics;
using System.IO;
using System.Text.Json;
using SessionDashboard;
internal static partial class Program
{
    private static void GenerateShowcase(string executable)
    {
        executable=Path.GetFullPath(executable);
        if(UpdateTransaction.Hash(executable)!="D836DE09B79E873E232ED6E9FAE67F9E1F73D5D6B2A62EFCCE1AB31C2E75B420")throw new InvalidOperationException("Use the checksum-verified published build for public screenshots.");
        var destination=Path.GetFullPath("docs/media/screenshots");Directory.CreateDirectory(destination);
        foreach(var study in new[]{true,false})
        {
            var slug=study?"study-space-dark":"meeting-space-dark";var directory=Path.Combine(output,slug);Directory.CreateDirectory(directory);
            var store=new StateStore(Path.Combine(directory,"isolated-state.json"));var now=DateTimeOffset.UtcNow;
            var pixel=WidgetLibrary.Create("Pixel timer","pixels");pixel.BannerSpan=12;pixel.BannerHeight=312;
            var note=WidgetLibrary.Create("Notes","notes");note.Title=study?"Study notes":"Meeting notes";note.Span=12;note.Height=288;note.PlacementX=0;note.PlacementY=365;
            note.Text=study?"Today’s focus\nUnderstand the ideas, then explain them simply.\n\nKey concepts\n• Connect new material to familiar ideas.\n• Break big questions into small steps.\n• Summarize in your own words.":"Meeting goal\nAgree on priorities and leave with clear next steps.\n\nDecisions\n• Keep the first milestone small and measurable.\n• Share a short update at the next check-in.\n\nNext: collect questions and share the summary.";
            var tasks=WidgetLibrary.Create("Task List","tasks");tasks.Title=study?"Study checklist":"Next steps";tasks.Span=7;tasks.Height=264;tasks.PlacementX=.65;tasks.PlacementY=365;
            tasks.Items=study?[new(){Text="Review today’s notes",Done=true},new(){Text="Practice with two examples",Done=true},new(){Text="Write a short summary"},new(){Text="Plan the next study block"}]:[new(){Text="Confirm the first milestone",Done=true},new(){Text="Assign follow-up actions"},new(){Text="Share the meeting summary"}];
            var clock=WidgetLibrary.Create("Clock");clock.Title="Local time";clock.Span=7;clock.Height=144;clock.PlacementX=.65;clock.PlacementY=8;
            var counter=WidgetLibrary.Create(study?"Water Bottle Counter":"Counter");counter.Title=study?"Stay hydrated":"Decisions made";counter.Value=study?3:2;counter.Span=7;counter.Height=216;counter.PlacementX=.65;counter.PlacementY=172;
            var data=new DashboardData{WidgetLayoutInitialized=true,Widgets=[pixel,note,tasks,clock,counter],Notes=note.Text,Tasks=tasks.Items,Session=new Session{Title=study?"Study space · example":"Meeting space · example",Start=now.AddMinutes(study?-20:-12),End=now.AddMinutes(study?30:18),ConfiguredDurationMinutes=study?50:30,TimeZoneId="America/Chicago",IntervalMinutes=5}};
            var dashboard=Continuity.NewDashboard(data.Session.Title,study?"Studying":"Meeting",data,store.UserId);var workspace=new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=dashboard.DashboardId,Dashboards=[dashboard]};
            workspace.Settings.SeasonalMode="Off";workspace.Settings.ShowQuickTips=false;workspace.Settings.ShowAutosaveNotifications=false;workspace.Settings.CelebrationEffects=false;
            var state=Continuity.Activate(workspace,store.UserId);state.WindowWidth=1440;state.WindowHeight=1080;store.Save(state);store.SavePreferences(ThemeMode.Dark,AppTheme.Nexa);
            using var process=Process.Start(new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,ArgumentList={"--verify-startup",directory}})??throw new IOException("Screenshot renderer did not start.");
            if(!process.WaitForExit(45000)){process.Kill();throw new TimeoutException("Screenshot capture timed out.");}
            if(process.ExitCode!=0)throw new InvalidOperationException("Published screenshot renderer failed: "+directory);
            using var report=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"startup.json")));
            Assert(report.RootElement.GetProperty("AppVersion").GetString()=="0.1.24","Screenshot comes from the published application: "+slug);
            File.Copy(Path.Combine(directory,"startup.png"),Path.Combine(destination,slug+".png"),true);
            Assert(new FileInfo(Path.Combine(destination,slug+".png")).Length>10000,"Real dashboard screenshot captured with isolated sample data: "+slug);
        }
    }
}
