using System.Windows;
using Microsoft.Win32;

namespace SessionDashboard;

internal static class WallpaperPicker
{
    internal static Func<Window,string?> Select = Native;
    private static string? Native(Window owner)
    {
        var picker=new OpenFileDialog{Title="Choose wallpaper image",Filter="Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files|*.*",CheckFileExists=true,Multiselect=false};
        return picker.ShowDialog(owner)==true?picker.FileName:null;
    }
    internal static string? Choose(Window owner)
    {
        var chain=new List<(Window Window,bool Topmost)>();
        for(Window? window=owner;window!=null;window=window.Owner)chain.Add((window,window.Topmost));
        try
        {
            foreach(var (window,_) in chain)window.Topmost=false;
            owner.Activate();return Select(owner);
        }
        finally
        {
            foreach(var (window,topmost) in chain)if(window.IsVisible)window.Topmost=topmost;
            if(owner.IsVisible)owner.Activate();
        }
    }
}
