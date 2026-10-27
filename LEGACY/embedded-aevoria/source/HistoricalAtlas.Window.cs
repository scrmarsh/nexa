using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;
internal sealed class HistoricalAtlasWindow : NexaDialog
{
    private sealed record Entry(string Id,string Name,string Category,string SearchText);
    private readonly HistoricalAtlas atlas;
    private readonly Entry[] entries;
    internal readonly TextBox Query=new() {MaxLength=200,MinWidth=180,Padding=new Thickness(6),ToolTip="Search names, aliases, and atlas origins"};
    internal readonly ComboBox Category=new() {MinWidth=160,Margin=new Thickness(8,0,8,0)};
    private readonly ComboBox era=new() {MinWidth=180,MaxWidth=300};
    internal readonly ListBox Results=new() {DisplayMemberPath="Name",MinWidth=230};
    internal readonly TextBlock Detail=new() {TextWrapping=TextWrapping.Wrap,Margin=new Thickness(16,0,8,12)};
    private readonly TextBlock count=new() {Margin=new Thickness(0,8,0,8)};
    internal static HistoricalAtlasWindow ShowFor(Window owner)
    {
        var existing=Application.Current.Windows.OfType<HistoricalAtlasWindow>().FirstOrDefault(w=>w.Owner==owner);
        if(existing!=null) {existing.Activate();return existing;}
        var window=new HistoricalAtlasWindow(owner);window.Show();return window;
    }
    internal HistoricalAtlasWindow(Window owner)
    {
        atlas=HistoricalAtlas.Default;Owner=owner;Title="Nexa - Civilization Atlas (development reference)";
        Width=1100;Height=760;MinWidth=760;MinHeight=500;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        entries=atlas.Data.Entities.Select(e=>new Entry(e.Id,e.Name,e.Status switch {AtlasEntityStatus.HistoricalReference=>"Historical inventory",AtlasEntityStatus.TraditionalReference=>"Textual traditions",_=>"Fictional civilizations"},e.Name+" "+string.Join(" ",e.Aliases)+" "+e.Origin))
            .Concat(LivingCultures.Default.Data.Profiles.Select(p=>new Entry(p.Id,p.Name,"Age of Kingdoms themes",p.Name+" "+p.Species)))
            .Concat(atlas.Data.Eras.Select(e=>new Entry(e.Id,e.Name,"Eras",e.Name)))
            .Concat(atlas.Data.Wars.Select(e=>new Entry(e.Id,e.Name,"Fictional conflicts",e.Name+" "+e.Description)))
            .Concat(atlas.Data.Technologies.Select(e=>new Entry(e.Id,$"{e.StartYear}-{e.EndYear}: technology scenario","Fictional technology",e.Vehicles+" "+e.Weapons)))
            .Concat(atlas.Data.Branches.Select(e=>new Entry(e.Id,e.Thread,"Possible successors",e.Thread+" "+e.NearText+" "+e.FarText)))
            .OrderBy(e=>e.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();
        var root=new DockPanel {Margin=new Thickness(20)};Content=root;
        var header=new StackPanel();DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        header.Children.Add(new TextBlock {Text="Civilization Atlas",FontSize=24,FontWeight=FontWeights.SemiBold});
        header.Children.Add(new TextBlock {Text="Reference inventory pending historical verification. Future civilizations, conflicts and technologies are fiction. Browsing does not change your world.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,12)});
        var filters=new WrapPanel();header.Children.Add(filters);filters.Children.Add(Query);filters.Children.Add(Category);filters.Children.Add(era);
        foreach(var name in new[]{"All entries","Historical inventory","Textual traditions","Fictional civilizations","Eras","Fictional conflicts","Fictional technology","Possible successors","Age of Kingdoms themes"})Category.Items.Add(name);Category.SelectedIndex=0;
        era.Items.Add(new ComboBoxItem {Content="All reference periods",Tag=""});foreach(var e in atlas.Data.Eras)era.Items.Add(new ComboBoxItem {Content=e.Name,Tag=e.Id});era.SelectedIndex=0;
        header.Children.Add(count);
        var body=new Grid();body.ColumnDefinitions.Add(new() {Width=new GridLength(1,GridUnitType.Star)});body.ColumnDefinitions.Add(new() {Width=new GridLength(2,GridUnitType.Star)});root.Children.Add(body);
        Results.SetResourceReference(Control.BackgroundProperty,"InputBrush");Results.SetResourceReference(Control.ForegroundProperty,"TextBrush");Results.SetResourceReference(Control.BorderBrushProperty,"LineBrush");
        ScrollViewer.SetCanContentScroll(Results,true);VirtualizingStackPanel.SetIsVirtualizing(Results,true);VirtualizingStackPanel.SetVirtualizationMode(Results,VirtualizationMode.Recycling);body.Children.Add(Results);
        var details=new StackPanel();var detailScroll=new ScrollViewer {Content=details,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetColumn(detailScroll,1);body.Children.Add(detailScroll);
        details.Children.Add(Detail);var reference=new Button {Content="Show reference excerpt",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(16,0,0,8),Padding=new Thickness(10,6,10,6)};details.Children.Add(reference);
        var excerpt=new TextBlock {TextWrapping=TextWrapping.Wrap,Margin=new Thickness(16,0,8,12)};details.Children.Add(excerpt);
        Query.TextChanged+=(_,_)=>Refresh();Category.SelectionChanged+=(_,_)=>Refresh();era.SelectionChanged+=(_,_)=>Refresh();
        Results.SelectionChanged+=(_,_)=> {excerpt.Text="";if(Results.SelectedItem is Entry entry)Detail.Text=Describe(entry);};
        reference.Click+=(_,_)=> {if(Results.SelectedItem is Entry entry)excerpt.Text=string.Join("\n\n",SourceIds(entry).Distinct().Select(id=>$"Atlas block {id}\n"+string.Join(" | ",atlas.SourceBlocks[id].Cells)));};
        System.Windows.Automation.AutomationProperties.SetName(Query,"Search civilization atlas");
        Refresh();
    }
    private void Refresh()
    {
        var query=Query.Text.Trim();var category=Category.SelectedItem as string;var eraId=(era.SelectedItem as ComboBoxItem)?.Tag as string;
        var visible=entries.Where(e=>(category=="All entries" || e.Category==category) && HistoricalAtlas.Matches(e.SearchText,query) &&
            (string.IsNullOrEmpty(eraId) || atlas.Entities.TryGetValue(e.Id,out var c) && c.EraIds.Contains(eraId) || e.Id==eraId)).ToArray();
        Results.ItemsSource=visible;count.Text=$"{visible.Length} entries shown | {atlas.Data.Entities.Length} civilization identities | No scenario or migration is applied";
        if(visible.Length>0)Results.SelectedIndex=0;else Detail.Text="No matching reference entries.";
    }
    private string Describe(Entry entry)
    {
        var text=new StringBuilder().AppendLine(entry.Name).AppendLine(entry.Category).AppendLine();
        if(LivingCultures.Default.Profiles.TryGetValue(entry.Id,out var theme))
        {
            var cultures=LivingCultures.Default;var reference=cultures.Reference(theme);var government=cultures.Governments[theme.GovernmentId];
            text.AppendLine("FICTIONAL MIXED-ERA SCENARIO — read-only cultural reference").AppendLine("These profiles are not applied to worlds by browsing. Enable civilizations when generating a new Living World to use cultural governments, settlements and construction.").AppendLine();
            text.AppendLine($"Species: {theme.Species}\nCulture ID: {theme.CultureId}\nAtlas reference: {reference.Name} ({reference.Id})\nReference classification: {reference.Status}\nScenario government: {theme.GovernmentId}\nOffices: {string.Join(", ",government.Offices.Select(o=>o.Title+" ("+o.Succession+")"))}");
            text.AppendLine("Political offices do not confer reproductive eligibility. Current succession recognizes eligible core colony recovery; election and hereditary genealogy engines are not implemented.");
            text.AppendLine($"\nPersonal name vocabulary: {string.Join(", ",theme.PersonalNames)}\nSettlement name vocabulary: {string.Join(", ",theme.SettlementNames)}\nStarting era inspiration: {theme.StartingEra}\nPlanned layout: {theme.Layout}\nPlanned defense: {theme.DefensePreference}\nArchitecture vocabulary: {string.Join(", ",theme.Architecture)}\n{theme.MilitaryMetadata}");
            text.AppendLine("\nNo verified dated name pools or historical succession claims. Cultural themes do not imply these states coexisted or had these governments. Detailed source titles/architecture may be unspecified in the atlas.");
            var relations=cultures.Relationships(theme).ToArray();text.AppendLine($"Catalog political relationships: {relations.Length} (not forced)");foreach(var r in relations)text.AppendLine($"{r.FromId} -> {r.ToId} ({r.Kind})");
            text.AppendLine(cultures.Data.Provenance);return text.ToString();
        }
        if(atlas.Entities.TryGetValue(entry.Id,out var c))
        {
            void Line(string label,string? value)=>text.AppendLine(label+": "+(string.IsNullOrWhiteSpace(value) ? "Not specified in reference" : value));
            Line("Catalog ID",c.Id);Line("Category",c.Kind.ToString());Line("Alternate names",string.Join(", ",c.Aliases));Line("Origin",c.Origin);
            Line("Culture",atlas.Data.Cultures.FirstOrDefault(x=>x.Id==c.CultureId)?.Name);Line("Dynasty",atlas.Data.Dynasties.FirstOrDefault(x=>x.Id==c.DynastyId)?.Name);
            Line("Government",atlas.Data.Governments.FirstOrDefault(x=>x.Id==c.GovernmentId)?.Name);Line("Ruler title conventions",string.Join(", ",c.RulerTitles));
            Line("Architecture",c.Architecture);Line("Symbolic avatar (not a playable animal unit)",atlas.Data.Representations.FirstOrDefault(x=>x.Id==c.RepresentationId)?.Name);
            Line("Emergence date",HistoricalAtlas.Year(c.EmergenceYear));Line("End date",HistoricalAtlas.Year(c.EndYear));
            Line("Atlas periods (not exact founding dates)",string.Join("; ",c.EraIds.Select(id=>atlas.Eras[id].Name)));Line("Military / design identity",string.Join("; ",c.MilitaryCharacteristics));Line("Reference diplomatic status",c.DiplomaticStatus);
            foreach(var note in c.Notes)text.AppendLine(note);
            text.AppendLine().AppendLine("Source classifications:");foreach(var o in c.Occurrences)text.AppendLine($"• {o.Name}: {HistoricalAtlas.SourceLabel(o.Classification)} (block {o.BlockId})");
            text.AppendLine().AppendLine("Catalog relationships (not compulsory outcomes):");foreach(var r in atlas.Data.Relationships.Where(r=>r.FromId==c.Id || r.ToId==c.Id))text.AppendLine($"{atlas.Entities[r.FromId].Name} → {atlas.Entities[r.ToId].Name} ({r.Kind})\n{r.Provenance}");
        }
        else if(atlas.Eras.TryGetValue(entry.Id,out var e))text.AppendLine($"{HistoricalAtlas.Year(e.StartYear)} — {HistoricalAtlas.Year(e.EndYear)}\n{(e.Fictional ? "FICTIONAL chronology" : "Approximate reference grouping; not a founding date for every listed entity")}\n\n{e.Features}");
        else if(atlas.Data.Wars.FirstOrDefault(e=>e.Id==entry.Id) is {} w)text.AppendLine($"FICTIONAL SCENARIO — not a prediction or recorded game event\n{w.StartYear}–{w.EndYear}\n\n{w.Description}\n\nThis template does not declare a war or force an outcome.");
        else if(atlas.Data.Technologies.FirstOrDefault(e=>e.Id==entry.Id) is {} t)text.AppendLine($"FICTIONAL technology scenario, {t.StartYear}–{t.EndYear}\n\nVehicles / infrastructure: {t.Vehicles}\n\nWeapons / defenses: {t.Weapons}\n\nDesign rule: {t.Rule}\n\nThis reference does not unlock gameplay technology.");
        else if(atlas.Data.Branches.FirstOrDefault(e=>e.Id==entry.Id) is {} b)text.AppendLine($"POSSIBLE FICTIONAL BRANCH\n\n{b.NearText}\n→ {b.FarText}\n\nThese names describe one possible branch, not an inevitable political future.");
        return text.ToString();
    }
    private IEnumerable<int> SourceIds(Entry entry)
    {
        if(LivingCultures.Default.Profiles.TryGetValue(entry.Id,out var theme))return atlas.Entities[theme.CivilizationId].Occurrences.Select(o=>o.BlockId);
        if(atlas.Entities.TryGetValue(entry.Id,out var e))return e.Occurrences.Select(o=>o.BlockId);
        if(atlas.Eras.TryGetValue(entry.Id,out var era))return [era.BlockId];
        if(atlas.Data.Wars.FirstOrDefault(e=>e.Id==entry.Id) is {} w)return [w.BlockId,w.BlockId+1];
        if(atlas.Data.Technologies.FirstOrDefault(e=>e.Id==entry.Id) is {} t)return [t.BlockId];
        if(atlas.Data.Branches.FirstOrDefault(e=>e.Id==entry.Id) is {} b)return [b.BlockId];
        return [];
    }
}
