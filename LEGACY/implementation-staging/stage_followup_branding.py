from pathlib import Path
import shutil,re
r=Path('M:/Nexa');s=Path(__file__).parent
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
for name,target in [('NexaBranding.cs','NexaBranding.cs'),('SplashWindow.followup.cs','SplashWindow.cs')]:shutil.copy2(s/name,r/target)
purple=re.search(r'\["AccentBrush"\]="(#[0-9A-Fa-f]+)"',(r/'Theme.cs').read_text(encoding='utf-8-sig')).group(1)
for name in ['nexa-logo-full-dark.svg','nexa-logo-full.svg','nexa-icon.svg']:
 p=r/'assets'/name
 target=p.with_name(p.stem+'-seasonal.svg')
 t=p.read_text(encoding='utf-8-sig').replace('#65A30D','#1C102A').replace('#7F77DD',purple).replace('#CECBF6',purple).replace('#AFA9EC',purple)
 target.write_text(t,encoding='utf-8')
# Retain the old native-vector icon renderer, generate from vectors at every size.
t=Path('C:/Users/Sean Marsh/Documents/ChatGPT/Nexa/green_nexa_brand.ps1').read_text(encoding='utf-8-sig')
a=t.index('function New-NexaPng');t=t[a:]
t=t.replace("'#65A30D'","'#1C102A'").replace("'#7F77DD'",repr(purple)).replace("'#CECBF6'",repr(purple))
t=t.replace("'nexa-icon-256.png'","'nexa-icon-seasonal-256.png'").replace("'nexa-icon-1024.png'","'nexa-icon-seasonal-1024.png'").replace("'nexa-icon.ico'","'nexa-icon-seasonal.ico'")
t="$ErrorActionPreference='Stop'\nAdd-Type -AssemblyName PresentationCore,WindowsBase\n$taskAssets='M:\\Nexa\\assets'\n"+t
(s/'render_seasonal_brand.ps1').write_text(t,encoding='utf-8-sig')
p=r/'NexaLogo.cs';t=p.read_text(encoding='utf-8-sig');t=t.replace('private readonly DrawingImage logo;','private readonly DrawingImage normalLogo,seasonalLogo;')
t=t.replace('''    internal NexaLogo()
    {
        using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Nexa;component/assets/nexa-logo-full-dark.svg")).Stream;''','''    internal NexaLogo()
    {
        normalLogo=Load("nexa-logo-full-dark.svg");seasonalLogo=Load("nexa-logo-full-dark-seasonal.svg");System.Windows.Automation.AutomationProperties.SetName(this,"Nexa full logo");
    }
    private static DrawingImage Load(string asset)
    {
        using var stream=Application.GetResourceStream(new Uri("pack://application:,,,/Nexa;component/assets/"+asset)).Stream;''')
t=t.replace('drawing.Freeze(); logo = new DrawingImage(drawing);','drawing.Freeze();return new DrawingImage(drawing);').replace('        System.Windows.Automation.AutomationProperties.SetName(this, "Nexa full logo");','')
t=t.replace('        var scale = Math.Min','        var logo=Theme.SeasonalEnabled?seasonalLogo:normalLogo;\n        var scale = Math.Min');p.write_text(t,encoding='utf-8-sig')
edit('NexaChrome.cs',lambda t:t.replace('new Uri("pack://application:,,,/Nexa;component/assets/nexa-icon-256.png")','new Uri("pack://application:,,,/Nexa;component/assets/"+(Theme.SeasonalEnabled?"nexa-icon-seasonal-256.png":"nexa-icon-256.png"))'))
edit('Theme.cs',lambda t:t.replace('                Retarget(window, replacements);','                window.Icon=NexaChrome.Icon;\n                Retarget(window, replacements);'))
edit('MainWindow.xaml',lambda t:t.replace('Source="assets/nexa-icon-256.png"','Source="{Binding Source={x:Static local:NexaChrome.Icon}}"') if False else t)
edit('HomeWindow.cs',lambda t:t.replace('HomeWindow(StateStore store)','HomeWindow(StateStore store,AppState? loadedState=null)').replace('Saved = store.Load();','Saved = loadedState ?? store.Load();'))
edit('HomeWindow.About.cs',lambda t:t.replace('        const string phrase = "a space for whatever comes next.";','').replace('            if (fading) return;','            var phrase=NexaBranding.Tagline;\n            if(position>phrase.Length)position=phrase.Length;\n            if (fading) return;').replace('tagline.Text = phrase[..++position];','NexaBranding.ApplyTagline(tagline,phrase[..++position]);'))
edit('App.xaml.cs',lambda t:t.replace('var profile=store.LoadProfile();','var profile=await Task.Run(()=>store.LoadProfile());splash.Report(.2,"Profile settings loaded");').replace('                    await Task.Delay(600);\n',''))
edit('SessionDashboard.csproj',lambda t:t.replace('<ApplicationIcon>assets\\nexa-icon.ico</ApplicationIcon>','<ApplicationIcon>assets\\nexa-icon-seasonal.ico</ApplicationIcon>').replace('<Resource Include="assets\\nexa-icon.ico" />','<Resource Include="assets\\nexa-icon-seasonal.ico" /><Resource Include="assets\\nexa-icon-seasonal-256.png" /><Resource Include="assets\\nexa-logo-full-dark-seasonal.svg" />\n    <Resource Include="assets\\nexa-icon.ico" />'))
print('Derived seasonal brand accent from the checked Task checkbox AccentBrush:',purple)
