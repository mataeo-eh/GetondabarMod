using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace GetondabarMod;

[BepInPlugin(Id, "Getondabar", GeneratedVersion.Value)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "Therunner.Getondabar";
    internal static Plugin Instance = null!;
    internal RuleStore Store = null!;
    internal RuleEditor Editor = null!;
    private Harmony harmony = null!;
    private ConfigEntry<KeyboardShortcut> shortcut = null!;
    private ConfigEntry<bool> messagesEnabled = null!;
    private static readonly MethodInfo TakeInputMethod = AccessTools.Method(typeof(Player), "TakeInput");
    private int menuBlockFrame = -1;
    internal static bool EditorOpen => Instance != null && Instance.Editor != null && Instance.Editor.IsOpen;

    private void Awake()
    {
        Instance = this;
        messagesEnabled = Config.Bind("General", "Enabled", true, "Show saved custom petting messages. The editor remains available when disabled.");
        shortcut = Config.Bind("Interface", "EditorShortcut", new KeyboardShortcut(KeyCode.F7), "Open or close the pet response editor while in a world.");
        Store = new RuleStore(Path.Combine(Paths.ConfigPath, Id + ".rules.json"));
        Store.Load();
        if (Store.LoadError != null) Logger.LogError(Store.LoadError);
        Editor = new RuleEditor(this);
        harmony = new Harmony(Id);
        harmony.PatchAll();
        Logger.LogInfo("Getondabar " + GeneratedVersion.Value + " loaded. Idea and inspiration: ThreadMenace. Press " + shortcut.Value + " to edit pet responses.");
    }

    private void Update()
    {
        if (!Player.m_localPlayer) { Editor.Close(); return; }
        if (Editor.IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || shortcut.Value.IsDown())
            {
                menuBlockFrame = Time.frameCount;
                Editor.Close();
            }
        }
        else if (shortcut.Value.IsDown() && (bool)TakeInputMethod.Invoke(Player.m_localPlayer, null)) Editor.Open();
    }
    private void OnGUI() => Editor?.Draw();
    private void OnDestroy() { Editor?.Dispose(); harmony?.UnpatchSelf(); if (Instance == this) Instance = null!; }

    internal static string? Response(Tameable pet)
    {
        if (Instance == null || !Instance.messagesEnabled.Value || !pet || !pet.IsTamed() || !pet.GetComponent<Character>()) return null;
        var name = pet.GetText(); // Assigned name only: never match an unnamed creature's species label.
        var prefab = Utils.GetPrefabName(pet.gameObject);
        return RuleEngine.Match(Instance.Store.Document.Rules, name, prefab);
    }

    // These wrappers run only at the successful petting branches of vanilla Interact.
    // Hold, rename, wild creatures, cooldown, effects, stats and RPC ownership remain vanilla.
    public static void PetMessage(Character user, MessageHud.MessageType type, string text, int amount,
        Sprite icon, bool showWhileDead, Tameable pet)
        => user.Message(type, Response(pet) ?? text, amount, icon, showWhileDead);

    public static void PetCommand(Tameable pet, Humanoid user, bool showMessage)
    {
        pet.Command(user, showMessage);
        var response = Response(pet);
        if (response != null) user.Message(MessageHud.MessageType.Center, response);
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
    public static class PetPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
        {
            var instructions = source.ToList();
            var message = AccessTools.Method(typeof(Character), nameof(Character.Message));
            var command = AccessTools.Method(typeof(Tameable), nameof(Tameable.Command));
            if (instructions.Count(i => i.Calls(message)) != 1 || instructions.Count(i => i.Calls(command)) != 1)
                throw new InvalidOperationException("Valheim petting code changed; Getondabar refuses an unsafe patch.");
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(message))
                {
                    var load = new CodeInstruction(OpCodes.Ldarg_0);
                    load.labels.AddRange(instruction.labels);
                    instruction.labels.Clear();
                    load.blocks.AddRange(instruction.blocks);
                    instruction.blocks.Clear();
                    yield return load;
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(Plugin), nameof(PetMessage));
                }
                else if (instruction.Calls(command))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(Plugin), nameof(PetCommand));
                }
                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    private static class InputPatch
    {
        private static void Postfix(ref bool __result) { if (EditorOpen) __result = false; }
    }

    [HarmonyPatch(typeof(PlayerController), "TakeInput")]
    private static class ControllerInputPatch
    {
        private static void Postfix(ref bool __result) { if (EditorOpen) __result = false; }
    }

    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
    private static class ScrollPatch
    {
        private static void Postfix(ref float __result) { if (EditorOpen) __result = 0; }
    }

    [HarmonyPatch(typeof(Menu), "Update")]
    private static class MenuPatch
    {
        private static bool Prefix() => !EditorOpen && (Instance == null || Instance.menuBlockFrame != Time.frameCount);
    }

    [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
    private static class CursorPatch
    {
        private static bool Prefix()
        {
            if (!EditorOpen) return true;
            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
            return false;
        }
    }
}
