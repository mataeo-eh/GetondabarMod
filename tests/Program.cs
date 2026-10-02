using GetondabarMod;

int passed = 0;
void Check(bool condition, string label)
{
    if (!condition) throw new Exception("FAIL: " + label);
    passed++;
    Console.WriteLine("PASS: " + label);
}
void Invalid(RuleDocument doc, string label)
{
    try { RuleEngine.Validate(doc); } catch (InvalidDataException) { Check(true, label); return; }
    throw new Exception("FAIL: " + label);
}

var defaults = new RuleDocument();
foreach (var species in new[] { "Wolf", "Boar", "Lox", "Hen", "Chicken", "Asksvin", "ModdedPet" })
    Check(RuleEngine.Match(defaults.Rules, "Getondabar", species) == "Getondabar thinks you are an ugo", "default on " + species);
Check(RuleEngine.Match(defaults.Rules, "getondabar", "Boar") == null, "case sensitive");
Check(RuleEngine.Match(defaults.Rules, "Getondabar ", "Boar") == null, "no implicit trim");
Check(RuleEngine.Match(defaults.Rules, "", "Wolf") == null, "unnamed pet untouched");
Check(RuleEngine.Match(defaults.Rules, "Other", "Wolf") == null, "other name untouched");
defaults.Rules.Add(new PetRule { Name = "Getondabar", Message = "{name} likes snacks", Creatures = new() { "Wolf", "Hen" } });
defaults.Rules.Add(new PetRule { Name = "Getondabar", Message = "Boar says oink", Creatures = new() { "Boar" } });
RuleEngine.Validate(defaults);
Check(RuleEngine.Match(defaults.Rules, "Getondabar", "Wolf") == "Getondabar likes snacks", "specific overrides all");
Check(RuleEngine.Match(defaults.Rules, "Getondabar", "Hen") == "Getondabar likes snacks", "multi-select applies to second type");
Check(RuleEngine.Match(defaults.Rules, "Getondabar", "Boar") == "Boar says oink", "same name different type and text");
Check(RuleEngine.Match(defaults.Rules, "Getondabar", "Lox") == "Getondabar thinks you are an ugo", "all-types fallback");
var reversed = defaults.Rules.AsEnumerable().Reverse();
Check(RuleEngine.Match(reversed, "Getondabar", "Wolf") == "Getondabar likes snacks", "priority independent of ordering");
Invalid(new RuleDocument { Rules = new() { new PetRule(), new PetRule() } }, "duplicate all-types rule rejected");
Invalid(new RuleDocument { Rules = new() { new PetRule { Creatures = new() { "Wolf", "Hen" } }, new PetRule { Creatures = new() { "Hen" } } } }, "overlap rejected");
Invalid(new RuleDocument { Rules = new() { new PetRule { Creatures = new() { "Wolf", "Wolf" } } } }, "duplicate creature ID rejected");
Invalid(new RuleDocument { Rules = new() { new PetRule { Name = " " } } }, "blank name rejected");
Invalid(new RuleDocument { Rules = new() { new PetRule { Message = "" } } }, "blank message rejected");
Invalid(new RuleDocument { Rules = new() { new PetRule { Message = "<color=red>bad</color>" } } }, "message markup rejected");
Invalid(new RuleDocument { SchemaVersion = 2 }, "future schema rejected");
Invalid(new RuleDocument { Rules = new() { new PetRule { Creatures = null! } } }, "null creature list rejected");

var root = Path.Combine(Path.GetTempPath(), "getondabar-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var path = Path.Combine(root, "rules.json");
    var store = new RuleStore(path);
    store.Load();
    Check(File.Exists(path) && store.Document.Rules.Count == 1, "first load persists default");
    var draft = store.Document.Rules[0].Copy();
    draft.Message = "unsaved draft";
    Check(RuleEngine.Match(store.Document.Rules, "Getondabar", "Wolf") == "Getondabar thinks you are an ugo", "draft leaves active rule unchanged");
    store.Save(defaults);
    Check(File.Exists(path + ".bak"), "atomic replacement retains previous backup");
    defaults.Rules[0].Message = "mutated after save";
    Check(RuleEngine.Match(store.Document.Rules, "Getondabar", "Lox") == "Getondabar thinks you are an ugo", "save isolates active document");
    var reloaded = new RuleStore(path);
    reloaded.Load();
    Check(reloaded.Document.Rules.Count == 3 && reloaded.LoadError == null, "saved multi-rule round trip");
    Check(RuleEngine.Match(reloaded.Document.Rules, "Getondabar", "Hen") == "Getondabar likes snacks", "persisted multi-select matches");
    var prior = File.ReadAllText(path);
    try { store.Save(new RuleDocument { Rules = new() { new PetRule { Name = "" } } }); } catch (InvalidDataException) { }
    Check(File.ReadAllText(path) == prior, "invalid save preserves disk");
    store.Save(new RuleDocument { Rules = new() });
    reloaded.Load();
    Check(reloaded.Document.Rules.Count == 0, "deleted defaults stay deleted after reload");
    File.WriteAllText(path, "{ invalid }");
    reloaded.Load();
    Check(reloaded.LoadError != null && reloaded.Document.Rules.Count == 0, "corrupt file disables overrides");
    try { reloaded.Save(new RuleDocument()); } catch (InvalidOperationException) { }
    Check(File.ReadAllText(path) == "{ invalid }", "corrupt file is never overwritten");
    File.WriteAllText(path, "{\"SchemaVersion\":99,\"Rules\":[]}");
    reloaded = new RuleStore(path);
    reloaded.Load();
    Check(reloaded.LoadError != null && File.ReadAllText(path).Contains("99"), "unknown schema preserved");
}
finally { Directory.Delete(root, true); }
Console.WriteLine($"{passed} checks passed.");
