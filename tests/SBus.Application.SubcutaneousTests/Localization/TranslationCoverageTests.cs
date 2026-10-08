using System.Text.RegularExpressions;
using System.Xml.Linq;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Localization;

public partial class TranslationCoverageTests
{
    private static readonly string Root = FindRepositoryRoot();

    [Fact]
    public void EveryArabicTextHasAnEnglishTranslation()
    {
        var translations = LoadEnglishResource();

        var missing = CollectArabicKeys()
            .Where(key => !translations.ContainsKey(key))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0, "Missing English translations:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void TranslationsKeepTheSamePlaceholders()
    {
        var mismatched = LoadEnglishResource()
            .Where(t => !Placeholders(t.Key).SetEquals(Placeholders(t.Value)))
            .Select(t => $"{t.Key}  =>  {t.Value}")
            .ToList();

        Assert.True(mismatched.Count == 0, "Placeholder mismatch:\n" + string.Join("\n", mismatched));
    }

    private static HashSet<string> CollectArabicKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        Collect(keys, Path.Combine(Root, "src", "SBus.Web"), "*.cshtml", ViewKey());
        Collect(keys, Path.Combine(Root, "src", "SBus.Web"), "*.cs", WebCodeKey());

        foreach (var project in new[] { "SBus.Domain", "SBus.Application" })
        {
            var directory = Path.Combine(Root, "src", project);
            Collect(keys, directory, "*.cs", DescriptionKey());
            Collect(keys, directory, "*.cs", PositionalErrorKey());
            Collect(keys, directory, "*.cs", ValidatorMessageKey());
            Collect(keys, directory, "*Errors.cs", SwitchArmKey());
        }

        return [.. keys.Where(k => ArabicLetter().IsMatch(k))];
    }

    private static void Collect(HashSet<string> keys, string directory, string pattern, Regex regex)
    {
        foreach (var file in Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match match in regex.Matches(File.ReadAllText(file)))
            {
                keys.Add(Regex.Unescape(match.Groups[1].Value));
            }
        }
    }

    private static Dictionary<string, string> LoadEnglishResource()
    {
        var path = Path.Combine(Root, "src", "SBus.Web", "Resources", "SharedResource.en.resx");

        return XDocument.Load(path)
            .Root!
            .Elements("data")
            .ToDictionary(e => (string)e.Attribute("name")!, e => (string)e.Element("value")!, StringComparer.Ordinal);
    }

    private static HashSet<string> Placeholders(string text) =>
        [.. Placeholder().Matches(text).Select(m => m.Value)];

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SBus.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("SBus.slnx not found above the test output folder.");
    }

    [GeneratedRegex("L\\[\"((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex ViewKey();

    [GeneratedRegex("(?:AddError|SetSuccess|localizer\\[)\\(?\\s*\"((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex WebCodeKey();

    [GeneratedRegex("description:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex DescriptionKey();

    [GeneratedRegex("Error\\.\\w+\\(\\s*\"[^\"]*\",\\s*\"((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex PositionalErrorKey();

    [GeneratedRegex("WithMessage\\(\\s*\"((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex ValidatorMessageKey();

    [GeneratedRegex("=>\\s*\"((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex SwitchArmKey();

    [GeneratedRegex("[\\u0600-\\u06FF]")]
    private static partial Regex ArabicLetter();

    [GeneratedRegex("\\{\\d+\\}")]
    private static partial Regex Placeholder();
}
