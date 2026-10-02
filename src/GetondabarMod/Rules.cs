using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace GetondabarMod;

public sealed class PetRule
{
    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; } = "Getondabar";
    [JsonProperty(Required = Required.Always)]
    public string Message { get; set; } = "{name} thinks you are an ugo";
    // Empty means all tameable creature types, including modded creatures.
    [JsonProperty(Required = Required.Always)]
    public List<string> Creatures { get; set; } = new();
    public PetRule Copy() => new() { Name = Name, Message = Message, Creatures = new List<string>(Creatures) };
}

public sealed class RuleDocument
{
    [JsonProperty(Required = Required.Always)]
    public int SchemaVersion { get; set; } = 1;
    [JsonProperty(Required = Required.Always)]
    public List<PetRule> Rules { get; set; } = new() { new PetRule() };
}

public static class RuleEngine
{
    public static string? Match(IEnumerable<PetRule> rules, string name, string prefab)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var named = rules.Where(r => string.Equals(r.Name, name, StringComparison.Ordinal));
        var rule = named.FirstOrDefault(r => r.Creatures.Contains(prefab, StringComparer.Ordinal))
                   ?? named.FirstOrDefault(r => r.Creatures.Count == 0);
        return rule?.Message.Replace("{name}", name);
    }

    public static void Validate(RuleDocument document)
    {
        if (document == null || document.SchemaVersion != 1 || document.Rules == null)
            throw new InvalidDataException("Unsupported or incomplete rules file.");
        for (int i = 0; i < document.Rules.Count; i++)
        {
            var rule = document.Rules[i];
            if (rule == null || string.IsNullOrWhiteSpace(rule.Name) || rule.Name.Length > 100 ||
                rule.Name.IndexOfAny(new[] { '\r', '\n', '<', '>' }) >= 0)
                throw new InvalidDataException("Enter an exact pet name (1–100 characters, no markup or line breaks).");
            if (string.IsNullOrWhiteSpace(rule.Message) || rule.Message.Length > 300 ||
                rule.Message.IndexOfAny(new[] { '\r', '\n', '<', '>' }) >= 0)
                throw new InvalidDataException("Enter a message (1–300 characters, no markup or line breaks).");
            if (rule.Creatures == null || rule.Creatures.Any(string.IsNullOrWhiteSpace) ||
                rule.Creatures.Distinct(StringComparer.Ordinal).Count() != rule.Creatures.Count)
                throw new InvalidDataException("Creature selections must be unique prefab IDs.");
            foreach (var other in document.Rules.Take(i).Where(r => r.Name == rule.Name))
                if ((rule.Creatures.Count == 0 && other.Creatures.Count == 0) ||
                    rule.Creatures.Intersect(other.Creatures, StringComparer.Ordinal).Any())
                    throw new InvalidDataException("This name already has a rule for one of these creature types. Edit that rule or choose other types.");
        }
    }
}

public sealed class RuleStore
{
    private readonly string path;
    public RuleDocument Document { get; private set; } = new();
    public string? LoadError { get; private set; }
    public RuleStore(string path) => this.path = path;

    public void Load()
    {
        try
        {
            if (!File.Exists(path)) { Save(new RuleDocument()); return; }
            var loaded = JsonConvert.DeserializeObject<RuleDocument>(File.ReadAllText(path),
                new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error, ObjectCreationHandling = ObjectCreationHandling.Replace });
            RuleEngine.Validate(loaded!);
            Document = loaded!;
            LoadError = null;
        }
        catch (Exception)
        {
            // Never replace a corrupt/unknown file with defaults or silently destroy user rules.
            Document = new RuleDocument { Rules = new List<PetRule>() };
            LoadError = "Rules could not be loaded. Custom messages are disabled. Repair or move the rules JSON and restart; the original file was preserved.";
        }
    }

    public void Save(RuleDocument document)
    {
        if (LoadError != null) throw new InvalidOperationException(LoadError);
        RuleEngine.Validate(document);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonConvert.SerializeObject(document, Formatting.Indented));
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
            Document = new RuleDocument { Rules = document.Rules.Select(r => r.Copy()).ToList() };
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
