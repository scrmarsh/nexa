using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SessionDashboard;

public sealed record TrackerIcon(string Id, string Name, string Group, string Glyph, string? Geometry = null);

// Extend the existing Segoe MDL2 icon family with matching outline pantry symbols.
public static class TrackerIcons
{
    public static IReadOnlyList<TrackerIcon> BuiltIn { get; } = Build();
    private static List<TrackerIcon> Build()
    {
        var icons = new List<TrackerIcon>();
        void Group(string group, params (string Name, int Glyph)[] values)
        {
            foreach (var (name, glyph) in values) icons.Add(new(name.ToLowerInvariant().Replace(' ', '-'), name, group, char.ConvertFromUtf32(glyph)));
        }
        icons.AddRange(new[] {
            new TrackerIcon("apple", "Apple", "Food", "", "M12,6 C5,2 2,8 4,14 C6,21 9,22 12,20 C15,22 18,21 20,14 C22,8 19,2 12,6 M12,6 C12,2 15,1 17,2 M12,5 C8,5 8,2 8,2"),
            new TrackerIcon("snowflake", "Snowflake", "Food", "", "M12,2 L12,22 M3,7 L21,17 M3,17 L21,7 M9,3 L12,6 L15,3 M9,21 L12,18 L15,21 M3,10 L6,9 L6,6 M18,18 L18,15 L21,14 M3,14 L6,15 L6,18 M18,6 L18,9 L21,10"),
            new TrackerIcon("refrigerator", "Refrigerator", "Food", "", "M5,2 L19,2 L19,22 L5,22 Z M5,10 L19,10 M8,5 L8,8 M8,13 L8,17"),
            new TrackerIcon("canned-food", "Canned Food", "Food", "", "M5,5 C5,1 19,1 19,5 C19,9 5,9 5,5 L5,20 C5,23 19,23 19,20 L19,5 M5,11 L19,11 M5,17 L19,17"),
            new TrackerIcon("soap", "Soap", "Supplies", "", "M7,9 L17,9 L19,22 L5,22 Z M10,9 L10,4 L14,4 L14,9 M12,4 L12,2 L19,2 L19,5 M9,14 L15,14 M9,17 L15,17") });
        Group("Food", ("Meal",0xED56), ("Coffee",0xEC32), ("Grocery Bag",0xE719), ("Water",0xE9CE), ("Utensils",0xED56), ("Beverage",0xEC32), ("Shopping",0xE7BF), ("Frozen Food",0xE9CA), ("Basket",0xE7BF), ("Nutrition",0xE95E));
        Group("People", ("Person",0xE77B), ("Group",0xE902), ("Student",0xE7BE), ("Teacher",0xE716), ("Staff",0xE716), ("Volunteer",0xEB51), ("Visitor",0xE8FA), ("Family",0xE716), ("Member",0xE77B), ("Guest",0xE8FA));
        Group("Events", ("Ticket",0xEB54), ("Calendar",0xE787), ("Badge",0xE8D4), ("Check In",0xE73E), ("Microphone",0xE720), ("Music",0xE8D6), ("Presentation",0xE7F4), ("Meeting",0xE902), ("Celebration",0xE734), ("Workshop",0xE90F));
        Group("Services", ("Help",0xE897), ("Information",0xE946), ("Customer Service",0xE8F2), ("Repair",0xE90F), ("Printing",0xE749), ("Wi Fi",0xE701), ("Support",0xE8F2), ("Consultation",0xE8F2), ("Registration",0xE8A5), ("Delivery",0xE7F4));
        Group("Supplies", ("Box",0xE7B8), ("Bag",0xE719), ("Paper",0xE8A5), ("Pen",0xE70F), ("Book",0xE82D), ("Clothing",0xE771), ("Bottle",0xE9CE), ("Cart",0xE7BF), ("Package",0xE7B8));
        Group("Locations", ("Building",0xE80F), ("Home",0xE80F), ("School",0xE7BE), ("Office",0xE821), ("Store",0xE719), ("Warehouse",0xE7B8), ("Classroom",0xE7F4), ("Pantry",0xE719), ("Entrance",0xE8FB), ("Exit",0xE8AC));
        Group("Technology", ("Laptop",0xE770), ("Desktop",0xE7F4), ("Tablet",0xE70A), ("Phone",0xE8EA), ("Monitor",0xE7F4), ("Keyboard",0xE765), ("Mouse",0xE962), ("Network",0xE968), ("Headphones",0xE7F6), ("Camera",0xE722));
        Group("Status", ("Check",0xE73E), ("Plus",0xE710), ("Minus",0xE738), ("Warning",0xE7BA), ("Priority",0xE814), ("Completed",0xE930), ("Waiting",0xE823), ("Rejected",0xE711), ("Star",0xE734), ("Flag",0xE7C1), ("Heart",0xEB51));
        Group("Equipment", ("Car",0xE804), ("Truck",0xE7F4), ("Toolbox",0xE90F), ("Key",0xE8D7), ("ID Card",0xE8D4));
        Group("Other", ("Clock",0xE917), ("Timer",0xE91E), ("Money",0xE8D4), ("Gift",0xE734), ("Document",0xE8A5), ("Folder",0xE8B7), ("Clipboard",0xE77F), ("Search",0xE721), ("Shield",0xEA18), ("Bell",0xE7ED));
        return icons;
    }
    public static FrameworkElement Create(string? id, DistributionTrackerState tracker, string assetDirectory, double size = 24)
    {
        if (id != null && tracker.CustomIcons.FirstOrDefault(i => i.Id == id) is { } custom)
        {
            try { return new Image { Source = Decode(AssetPath(assetDirectory, custom.StoredFile)), Width = size, Height = size, Stretch = Stretch.Uniform }; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or FileFormatException) { }
        }
        var icon = BuiltIn.FirstOrDefault(i => i.Id == id);
        if (icon?.Geometry is { } geometry)
        {
            var path = new System.Windows.Shapes.Path { Data = System.Windows.Media.Geometry.Parse(geometry), StrokeThickness = 1.6, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round, Width = size, Height = size, Stretch = Stretch.Uniform };
            path.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "TextBrush"); return path;
        }
        var text = new TextBlock { Text = id == null ? "" : icon?.Glyph ?? "\uE77F", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = size, Width = size + 2, VerticalAlignment = VerticalAlignment.Center };
        text.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush"); return text;
    }
    internal static string AssetPath(string directory, string filename)
    {
        if (string.IsNullOrWhiteSpace(filename) || Path.GetFileName(filename) != filename) throw new IOException("Invalid managed icon path.");
        return Path.Combine(directory, filename);
    }
    internal static BitmapImage Decode(string path)
    {
        using var stream = File.OpenRead(path);
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.DecodePixelWidth = 64; image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
    }
    internal static CustomTrackerIcon Import(string source, string directory)
    {
        var extension = Path.GetExtension(source).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg")) throw new IOException("Choose a PNG, JPG or JPEG image.");
        if (new FileInfo(source).Length > 10 * 1024 * 1024) throw new IOException("Icons must be smaller than 10 MB.");
        _ = Decode(source); Directory.CreateDirectory(directory);
        var icon = new CustomTrackerIcon { Name = Path.GetFileNameWithoutExtension(source), OriginalFilename = Path.GetFileName(source), StoredFile = Guid.NewGuid().ToString("N") + extension };
        File.Copy(source, AssetPath(directory, icon.StoredFile)); return icon;
    }
}
