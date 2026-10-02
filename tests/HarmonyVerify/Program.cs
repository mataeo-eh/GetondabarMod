using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using GetondabarMod;
using HarmonyLib;

internal static class Program
{
    private static string expectedVersion = "";
    private static int Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 3) { System.Console.Error.WriteLine("Pass the Valheim Managed directory, BepInEx core directory, and optionally the VERSION file path."); return 2; }
        expectedVersion = File.ReadAllText(args.Length == 3 ? args[2] : "VERSION").Trim();
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            string name = new AssemblyName(e.Name).Name + ".dll";
            foreach (string directory in args.Take(2))
            {
                string path = Path.Combine(directory, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try { Verify(); return 0; }
        catch (Exception ex) { System.Console.Error.WriteLine(ex); return 1; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Verify()
    {
        var interact = AccessTools.Method(typeof(Tameable), "Interact");
        var original = PatchProcessor.GetOriginalInstructions(interact);
        var rewritten = Plugin.PetPatch.Transpiler(original).ToList();
        if (rewritten.Count(i => i.Calls(AccessTools.Method(typeof(Plugin), "PetMessage"))) != 1 ||
            rewritten.Count(i => i.Calls(AccessTools.Method(typeof(Plugin), "PetCommand"))) != 1)
            throw new Exception("Petting branch replacement failed.");
        System.Console.WriteLine("PASS: current game pet and command branches rewritten exactly once");
        bool rejected = false;
        try { Plugin.PetPatch.Transpiler(Array.Empty<CodeInstruction>()).ToList(); }
        catch (InvalidOperationException) { rejected = true; }
        if (!rejected) throw new Exception("Changed game IL was not rejected.");
        System.Console.WriteLine("PASS: incompatible petting IL rejected");
        var metadata = typeof(Plugin).GetCustomAttribute<BepInPlugin>();
        if (metadata?.Version.ToString() != expectedVersion) throw new Exception("Plugin version does not match VERSION.");
        System.Console.WriteLine("PASS: BepInPlugin version " + expectedVersion);
        var harmony = new Harmony(Plugin.Id);
        try
        {
            harmony.PatchAll(typeof(Plugin).Assembly);
            int count = Harmony.GetAllPatchedMethods().Count(m => Harmony.GetPatchInfo(m).Owners.Contains(Plugin.Id));
            if (count != 6) throw new Exception("Expected six installed patches; got " + count);
            System.Console.WriteLine("PASS: all six Harmony patches compiled and installed against the current game assemblies");
        }
        finally { harmony.UnpatchSelf(); }
        System.Console.WriteLine("Static Harmony verification complete; game/UI behavior still requires an in-game check.");
    }
}
