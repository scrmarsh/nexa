using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace SessionDashboard;
internal static class SeasonalBackdrop
{
    internal static Grid Wrap(UIElement content,Func<NexaOptions> options,bool permanentCorners=false,Func<bool>? advanced=null,Func<EcosystemOptions>? appearance=null)
    {
        var root=new Grid{ClipToBounds=true};var art=new NexaweenDecorations{Opacity=.35};root.Children.Add(art);
        var corners=new SeasonalCorners{IsHitTestVisible=false};if(permanentCorners)root.Children.Add(corners);root.Children.Add(content);
        bool? wasEnabled=null;
        void Refresh()
        {
            var settings=options();var enabled=NexaSeason.Active(settings,DateTimeOffset.UtcNow) && settings.SeasonalDecorations && settings.DecorationDensity!="Off";corners.Appearance=appearance?.Invoke()??new();
            NexaMotion.SetShown(art,enabled,settings,.35);NexaMotion.SetShown(corners,enabled,settings);corners.Options=settings;
            var viewport=new Rect(0,0,Math.Max(1,root.ActualWidth),Math.Max(1,root.ActualHeight));art.ConfigureAnimation(settings,viewport);
            var reserved=permanentCorners?new List<Rect>{new Rect(Math.Max(0,viewport.Width/2-480),0,Math.Min(960,viewport.Width),190)}:[];
            art.Update(viewport.Width,viewport.Height,reserved,settings.DecorationDensity);
            if(enabled && wasEnabled!=true)art.BeginReveal();wasEnabled=enabled;
        }
        void Changed(object? sender,EventArgs e){corners.AdvancedAnimations=advanced?.Invoke()??true;Refresh();}
        root.SizeChanged+=(_,_)=>Refresh();
        root.Loaded+=(_,_)=>{corners.AdvancedAnimations=advanced?.Invoke()??true;Theme.SeasonalChanged-=Changed;Theme.SeasonalChanged+=Changed;Refresh();};
        root.Unloaded+=(_,_)=>Theme.SeasonalChanged-=Changed;
        root.IsVisibleChanged+=(_,_)=>Refresh();return root;
    }
}
