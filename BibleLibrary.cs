using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SessionDashboard;

public sealed class ScripturePin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Book { get; set; } = "Genesis";
    public int Chapter { get; set; } = 1;
    public int Verse { get; set; } = 1;
    public int EndVerse { get; set; } = 1;
    public string Translation { get; set; } = "NLT";
    public string Text { get; set; } = "";
    public string Note { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore]
    public string Reference => $"{Book} {Chapter}:{Verse}" + (EndVerse > Verse ? $"–{EndVerse}" : "");
}
public sealed class BibleReaderState
{
    public string Translation { get; set; } = "NLT";
    public string Book { get; set; } = "Genesis";
    public int Chapter { get; set; } = 1;
    public int Verse { get; set; } = 1;
}
internal sealed record BibleVerse(string Book, int Chapter, int Verse, string Text, int Page)
{
    public string Reference => $"{Book} {Chapter}:{Verse}";
}
internal sealed class BibleEdition
{
    public string Translation { get; set; } = "";
    public string Source { get; set; } = "";
    public string SourceSha256 { get; set; } = "";
    public List<BibleVerse> Verses { get; set; } = [];
    private Dictionary<(string, int), BibleVerse[]> chapters = [];
    internal void Index() => chapters = Verses.GroupBy(v => (v.Book, v.Chapter)).ToDictionary(g => g.Key, g => g.OrderBy(v => v.Verse).ToArray());
    internal BibleVerse[] Chapter(string book, int chapter) => chapters.GetValueOrDefault((book, chapter)) ?? [];
    internal int[] Chapters(string book) => chapters.Keys.Where(k => k.Item1 == book).Select(k => k.Item2).Order().ToArray();
    internal IEnumerable<BibleVerse> Search(string query)
    {
        query = query.Trim();
        if (BibleLibrary.TryReference(query, out var book, out var chapter, out var verse, out var end))
            return Chapter(book, chapter).Where(v => verse == 0 || v.Verse >= verse && v.Verse <= end);
        if (query.Length < 2) return [];
        return Verses.Where(v => v.Text.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
internal static class BibleLibrary
{
    internal static readonly string[] Translations = ["NLT", "CSB", "NKJV"];
    internal static readonly string[] Books = "Genesis|Exodus|Leviticus|Numbers|Deuteronomy|Joshua|Judges|Ruth|1 Samuel|2 Samuel|1 Kings|2 Kings|1 Chronicles|2 Chronicles|Ezra|Nehemiah|Esther|Job|Psalms|Proverbs|Ecclesiastes|Song of Solomon|Isaiah|Jeremiah|Lamentations|Ezekiel|Daniel|Hosea|Joel|Amos|Obadiah|Jonah|Micah|Nahum|Habakkuk|Zephaniah|Haggai|Zechariah|Malachi|Matthew|Mark|Luke|John|Acts|Romans|1 Corinthians|2 Corinthians|Galatians|Ephesians|Philippians|Colossians|1 Thessalonians|2 Thessalonians|1 Timothy|2 Timothy|Titus|Philemon|Hebrews|James|1 Peter|2 Peter|1 John|2 John|3 John|Jude|Revelation".Split('|');
    private static readonly ConcurrentDictionary<string, Lazy<BibleEdition>> editions = new();
    internal static BibleEdition Load(string translation)
    {
        if (!Translations.Contains(translation)) throw new InvalidDataException("This Bible translation is not available.");
        return editions.GetOrAdd(translation, t => new Lazy<BibleEdition>(() =>
        {
            using var resource = typeof(BibleLibrary).Assembly.GetManifestResourceStream($"SessionDashboard.Bible.{t}.json.gz")
                ?? throw new InvalidDataException($"The integrated {t} Bible could not be found.");
            using var gzip = new GZipStream(resource, CompressionMode.Decompress);
            var edition = JsonSerializer.Deserialize<BibleEdition>(gzip) ?? throw new InvalidDataException("The Bible index is unreadable.");
            if (edition.Translation != t || edition.Verses.Count == 0) throw new InvalidDataException("The Bible translation index does not match.");
            edition.Index(); return edition;
        })).Value;
    }
    internal static bool TryReference(string query, out string book, out int chapter, out int verse, out int end)
    {
        book = ""; chapter = verse = end = 0;
        var match = Regex.Match(query.Trim(), @"^(.+?)\s+(\d{1,3})(?::(\d{1,3})(?:\s*[-–]\s*(\d{1,3}))?)?$", RegexOptions.CultureInvariant);
        if (!match.Success) return false;
        var name = match.Groups[1].Value.Trim().TrimEnd('.');
        if (name.Equals("Psalm", StringComparison.OrdinalIgnoreCase) || name.Equals("Ps", StringComparison.OrdinalIgnoreCase)) name = "Psalms";
        if (name.Equals("Song of Songs", StringComparison.OrdinalIgnoreCase)) name = "Song of Solomon";
        var matches = Books.Where(b => b.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length == 0) matches = Books.Where(b => b.StartsWith(name, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1) return false;
        book = matches[0]; chapter = int.Parse(match.Groups[2].Value);
        verse = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
        end = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : verse;
        return chapter > 0 && end >= verse;
    }
}
