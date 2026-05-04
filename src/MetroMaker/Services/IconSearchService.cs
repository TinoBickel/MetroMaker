using System.Text.RegularExpressions;
using Material.Icons;

namespace MetroMaker.Services;

public partial class IconSearchService
{
    private readonly List<MaterialIconKind> _allIcons;

    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["papierkorb"] = ["delete", "trash"],
        ["loeschen"] = ["delete", "trash", "remove"],
        ["löschen"] = ["delete", "trash", "remove"],
        ["speichern"] = ["save", "floppy", "content"],
        ["drucken"] = ["print", "printer"],
        ["bearbeiten"] = ["edit", "pencil"],
        ["hinzufügen"] = ["add", "plus"],
        ["hinzufuegen"] = ["add", "plus"],
        ["entfernen"] = ["remove", "minus", "delete"],
        ["suchen"] = ["search", "magnify"],
        ["einstellungen"] = ["settings", "cog"],
        ["benutzer"] = ["account", "person", "user"],
        ["ordner"] = ["folder"],
        ["datei"] = ["file", "document"],
        ["kopieren"] = ["copy", "content"],
        ["einfügen"] = ["paste", "clipboard"],
        ["ausschneiden"] = ["cut", "scissors"],
        ["schliessen"] = ["close", "window"],
        ["öffnen"] = ["open", "folder"],
        ["herunterladen"] = ["download"],
        ["hochladen"] = ["upload"],
        ["aktualisieren"] = ["refresh", "reload"],
        ["rückgängig"] = ["undo"],
        ["wiederherstellen"] = ["redo", "restore"],
        ["bild"] = ["image", "photo", "picture"],
        ["kamera"] = ["camera"],
        ["kalender"] = ["calendar"],
        ["uhr"] = ["clock", "time"],
        ["nachricht"] = ["message", "email", "mail"],
        ["warnung"] = ["alert", "warning"],
        ["fehler"] = ["error", "alert", "bug"],
        ["information"] = ["info", "information"],
        ["hilfe"] = ["help", "question"],
        ["startseite"] = ["home"],
        ["stern"] = ["star"],
        ["herz"] = ["heart"],
        ["pfeil"] = ["arrow"],
        ["link"] = ["link", "chain"],
        ["schloss"] = ["lock"],
        ["entsperren"] = ["lock-open", "unlock"],
        ["export"] = ["export"],
        ["import"] = ["import"],
    };

    public IconSearchService()
    {
        _allIcons = Enum.GetValues<MaterialIconKind>().Distinct().ToList();
    }

    public IReadOnlyList<MaterialIconKind> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _allIcons;

        var terms = ExpandSynonyms(query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return _allIcons
            .Select(icon => new { Icon = icon, Score = CalculateScore(icon.ToString(), terms) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Icon.ToString())
            .Select(x => x.Icon)
            .ToList();
    }

    private static string[] ExpandSynonyms(string[] terms)
    {
        var expanded = new List<string>();
        foreach (var term in terms)
        {
            expanded.Add(term);
            if (Synonyms.TryGetValue(term, out var synonyms))
                expanded.AddRange(synonyms);
        }
        return expanded.ToArray();
    }

    private static int CalculateScore(string iconName, string[] terms)
    {
        var lowerName = iconName.ToLowerInvariant();
        var splitName = SplitPascalCase(iconName).ToLowerInvariant();

        int score = 0;
        bool anyMatch = false;

        foreach (var term in terms)
        {
            if (lowerName.Contains(term) || splitName.Contains(term))
            {
                anyMatch = true;
                score += lowerName == term ? 100 : 10;
                if (lowerName.StartsWith(term))
                    score += 5;
            }
        }

        if (!anyMatch) return 0;

        if (lowerName.Contains("outline"))
            score += 3;

        return score;
    }

    private static string SplitPascalCase(string input)
    {
        return PascalCaseRegex().Replace(input, " $1");
    }

    [GeneratedRegex(@"(?<=[a-z])([A-Z])|(?<=[A-Z])([A-Z][a-z])")]
    private static partial Regex PascalCaseRegex();
}
