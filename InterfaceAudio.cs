using System.IO;
using System.Media;

namespace SessionDashboard;

internal static class InterfaceAudio
{
    internal static MemoryStream Create(double volume)
    {
        const int rate=22050,count=4410;
        var audio=new MemoryStream();
        using(var writer=new BinaryWriter(audio,System.Text.Encoding.UTF8,true))
        {
            writer.Write("RIFF"u8); writer.Write(36+count*2); writer.Write("WAVEfmt "u8); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate*2); writer.Write((short)2); writer.Write((short)16); writer.Write("data"u8); writer.Write(count*2);
            volume=double.IsFinite(volume)?Math.Clamp(volume,0,1):.25;
            for(var i=0;i<count;i++) { var t=i/(double)rate; var envelope=Math.Sin(Math.PI*i/count)*Math.Exp(-t*12); writer.Write((short)(Math.Sin(2*Math.PI*(i<count/2?660:880)*t)*envelope*volume*5000)); }
        }
        audio.Position=0; return audio;
    }
    internal static void Preview(double volume) => _=Task.Run(()=>
    {
        try { using var audio=Create(volume); using var player=new SoundPlayer(audio); player.PlaySync(); }
        catch(Exception ex) when(ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
    });
    internal static void PreviewSeasonal() => _=Task.Run(()=>
    {
        try
        {
            const int rate=22050,count=8820;using var audio=new MemoryStream();
            using(var writer=new BinaryWriter(audio,System.Text.Encoding.UTF8,true))
            {
                writer.Write("RIFF"u8);writer.Write(36+count*2);writer.Write("WAVEfmt "u8);writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write("data"u8);writer.Write(count*2);
                for(var i=0;i<count;i++){var t=i/(double)rate;var envelope=Math.Sin(Math.PI*i/count)*Math.Exp(-t*7);var wave=(Math.Sin(2*Math.PI*220*t)+.45*Math.Sin(2*Math.PI*261.63*t)+.25*Math.Sin(2*Math.PI*311.13*t))/1.7;writer.Write((short)(wave*envelope*850));}
            }
            audio.Position=0;using var player=new SoundPlayer(audio);player.PlaySync();
        }
        catch(Exception ex) when(ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception){}
    });
}
