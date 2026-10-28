using System.IO;
using System.Media;

namespace SessionDashboard;

internal static class InterfaceAudio
{
    internal static readonly string[] PositiveChoices=["Chime","Bells","Marimba","Crystal","Pop"];
    internal static readonly string[] NegativeChoices=["Eerie","Low bell","Wood","Glass","Dip"];
    internal static MemoryStream Create(double volume)
    {
        const int rate=22050,count=4410;
        var audio=new MemoryStream();
        using(var writer=new BinaryWriter(audio,System.Text.Encoding.UTF8,true))
        {
            writer.Write("RIFF"u8); writer.Write(36+count*2); writer.Write("WAVEfmt "u8); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate*2); writer.Write((short)2); writer.Write((short)16); writer.Write("data"u8); writer.Write(count*2);
            volume=double.IsFinite(volume)?Math.Clamp(volume,0,1):.25;
            for(var i=0;i<count;i++) { var t=i/(double)rate; var envelope=Math.Sin(Math.PI*i/count)*Math.Exp(-t*12); writer.Write((short)(((Math.Sin(2*Math.PI*(780*t-650*t*t))+.22*Math.Sin(2*Math.PI*1170*t)*Math.Exp(-t*18))/1.22)*envelope*volume*5000)); }
        }
        audio.Position=0; return audio;
    }
    private static int previewPlaying;
    internal static MemoryStream CreateCue(double volume, string kind,string? selection=null)
    {
        const int rate = 22050;
        volume = double.IsFinite(volume) ? Math.Clamp(volume, 0, 1) : .25;
        var choice=Array.IndexOf(kind=="positive"?PositiveChoices:NegativeChoices,selection??(kind=="positive"?"Chime":"Eerie"));choice=Math.Max(0,choice);
        var duration = kind == "positive" ? .42 : kind == "negative" ? .32 : .18;
        var count = (int)(rate * duration); var audio = new MemoryStream();
        using (var writer = new BinaryWriter(audio, System.Text.Encoding.UTF8, true))
        {
            writer.Write("RIFF"u8); writer.Write(36 + count * 2); writer.Write("WAVEfmt "u8); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16); writer.Write("data"u8); writer.Write(count * 2);
            for (var i = 0; i < count; i++)
            {
                var t = i / (double)rate; var envelope = Math.Sin(Math.PI * i / count) * Math.Exp(-t * 8);
                var wave = kind == "positive" ? (Math.Sin(2 * Math.PI * 880 * t) + .45 * Math.Sin(2 * Math.PI * 1108.73 * t) + .25 * Math.Sin(2 * Math.PI * 1320 * t)) / 1.7
                    : kind == "negative" ? (Math.Sin(2 * Math.PI * 220 * t) + .25 * Math.Sin(2 * Math.PI * 233.08 * t)) / 1.25
                    : (Math.Sin(2 * Math.PI * (780 * t - 650 * t * t))+.22*Math.Sin(2*Math.PI*1170*t)*Math.Exp(-t*18))/1.22;
                if(choice>0)
                {
                    var frequency=kind=="positive"?new[]{880d,1046.5,659.25,1174.66,980}[choice]:new[]{220d,196,165,293.66,261.63}[choice];
                    var glide=kind=="negative"?-.18*t/duration:choice==4?-.25*t/duration:0;
                    wave=(Math.Sin(2*Math.PI*frequency*t*(1+glide))+.3*Math.Sin(2*Math.PI*frequency*(choice==2?3:1.5)*t))/1.3;
                    if(choice==2)envelope*=Math.Exp(-t*12);
                }
                writer.Write((short)(wave * envelope * volume * 3500));
            }
        }
        audio.Position = 0; return audio;
    }
    internal static void Preview(double volume) => PreviewCue(volume, "pop");
    internal static void PreviewCue(double volume, string kind,string? selection=null)
    {
        if (Interlocked.CompareExchange(ref previewPlaying, 1, 0) != 0) return;
        _ = Task.Run(() =>
        {
            try { using var audio = CreateCue(volume, kind,selection); using var player = new SoundPlayer(audio); player.PlaySync(); }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
            finally { Volatile.Write(ref previewPlaying, 0); }
        });
    }
    internal static void PreviewSeasonal(double volume = .25) => PreviewCue(volume, "negative");
}
