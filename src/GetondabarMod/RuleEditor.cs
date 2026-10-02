using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GetondabarMod;

// Self-contained Unity UI: no configuration-manager or asset-bundle dependency.
internal sealed class RuleEditor
{
    private readonly Plugin plugin;
    public bool IsOpen { get; private set; }
    private PetRule? draft;
    private int editIndex = -1;
    private int deleteIndex = -1;
    private bool allTypes;
    private string search = "";
    private string error = "";
    private Vector2 listScroll, editorScroll;
    private readonly List<CreatureChoice> catalog = new();
    private GUISkin? skin;
    private readonly List<Texture2D> textures = new();
    private readonly Color accent = new(0.95f, 0.72f, 0.35f);

    private sealed class CreatureChoice
    {
        public string Id = "";
        public string Label = "";
        public Sprite? Icon;
    }

    public RuleEditor(Plugin plugin) => this.plugin = plugin;
    public void Open()
    {
        IsOpen = true;
        error = plugin.Store.LoadError ?? "";
        DiscoverCreatures();
        draft = null;
        deleteIndex = -1;
        ZCursor.LockState = CursorLockMode.None;
        ZCursor.Show();
    }
    public void Close()
    {
        IsOpen = false;
        draft = null; // Escape, F7 and Close all discard an unsaved draft.
        deleteIndex = -1;
    }

    private void DiscoverCreatures()
    {
        catalog.Clear();
        if (ZNetScene.instance)
        {
            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                if (!prefab || !prefab.GetComponent<Tameable>()) continue;
                var character = prefab.GetComponent<Character>();
                if (!character) continue;
                string label = Localization.instance.Localize(character.m_name);
                // Vanilla trophy art is optional; modded creatures get a readable initial badge.
                var trophyId = prefab.name == "Hen" || prefab.name == "Chicken" ? "ChickenMeat" : "Trophy" + prefab.name;
                var trophy = ZNetScene.instance.GetPrefab(trophyId);
                var item = trophy ? trophy.GetComponent<ItemDrop>() : null;
                var icons = item ? item.m_itemData.m_shared.m_icons : null;
                catalog.Add(new CreatureChoice { Id = prefab.name, Label = label, Icon = icons?.FirstOrDefault() });
            }
        }
        foreach (var id in plugin.Store.Document.Rules.SelectMany(r => r.Creatures).Distinct())
            if (!catalog.Any(c => c.Id == id)) catalog.Add(new CreatureChoice { Id = id, Label = id + " (unavailable)" });
        catalog.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
    }

    private Texture2D Fill(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        textures.Add(texture);
        return texture;
    }

    private void CreateSkin()
    {
        skin = UnityEngine.Object.Instantiate(GUI.skin);
        skin.label.fontSize = 15;
        skin.label.normal.textColor = new Color(0.9f, 0.91f, 0.93f);
        skin.label.wordWrap = true;
        skin.label.richText = false;
        skin.button.fontSize = 15;
        skin.button.padding = new RectOffset(12, 12, 8, 8);
        skin.button.normal.background = Fill(new Color(0.17f, 0.21f, 0.26f));
        skin.button.hover.background = Fill(new Color(0.25f, 0.3f, 0.36f));
        skin.button.active.background = Fill(new Color(0.34f, 0.3f, 0.22f));
        skin.button.normal.textColor = Color.white;
        skin.button.richText = false;
        skin.box.normal.background = Fill(new Color(0.075f, 0.09f, 0.12f, 0.98f));
        skin.box.padding = new RectOffset(20, 20, 18, 18);
        skin.textField.fontSize = 17;
        skin.textField.padding = new RectOffset(10, 10, 8, 8);
        skin.textField.normal.textColor = Color.white;
        skin.textField.normal.background = Fill(new Color(0.13f, 0.16f, 0.2f));
        skin.textField.focused.background = Fill(new Color(0.2f, 0.23f, 0.28f));
        skin.textArea = new GUIStyle(skin.textField) { wordWrap = true, richText = false };
        skin.toggle.fontSize = 15;
    }

    public void Draw()
    {
        if (!IsOpen) return;
        if (skin == null) CreateSkin();
        var oldSkin = GUI.skin;
        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        var oldEnabled = GUI.enabled;
        float scale = Mathf.Min(1.25f, Mathf.Min(Screen.width / 940f, Screen.height / 720f));
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        GUI.skin = skin;
        try
        {
        float width = Screen.width / scale, height = Screen.height / scale;
        GUI.Box(new Rect(0, 0, width, height), GUIContent.none);
        GUILayout.BeginArea(new Rect((width - 900) / 2, (height - 680) / 2, 900, 680), GUIContent.none, skin!.box);
        GUILayout.BeginHorizontal();
        var heading = new GUIStyle(skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
        GUILayout.Label("GETONDABAR", heading);
        if (GUILayout.Button("Close", GUILayout.Width(85))) { Close(); GUIUtility.ExitGUI(); }
        GUILayout.EndHorizontal();
        GUILayout.Label("Pet responses · Idea and inspiration by ThreadMenace");
        GUILayout.Space(14);
        GUILayout.BeginHorizontal();
        DrawList();
        GUILayout.Space(20);
        GUILayout.BeginVertical(GUILayout.Width(520));
        if (draft == null)
        {
            DrawError();
            GUILayout.Label("Give your pets something to say", heading);
            GUILayout.Space(12);
            GUILayout.Label("Add a rule or choose one on the left. Match an exact pet name, select any combination of creature types, then write its response.");
            GUILayout.Space(12);
            GUILayout.Label("Names are case-sensitive. Specific creature rules take priority over an all-creatures rule for the same name.");
            GUILayout.Space(12);
            GUILayout.Label("Only Save changes a rule. Cancel, Close, Escape and your editor shortcut discard an unsaved draft.");
            GUI.enabled = plugin.Store.LoadError == null;
            if (GUILayout.Button("Create a response")) BeginEdit(-1);
            GUI.enabled = true;
        }
        else DrawDraft();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Local to your profile · Vanilla pet effects and follow/stay commands are preserved");
        GUILayout.EndArea();
        }
        finally
        {
            GUI.skin = oldSkin;
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            GUI.enabled = oldEnabled;
        }
    }

    private void DrawList()
    {
        GUILayout.BeginVertical(GUILayout.Width(300));
        GUILayout.Label("SAVED RESPONSES (" + plugin.Store.Document.Rules.Count + ")");
        GUI.enabled = plugin.Store.LoadError == null && draft == null;
        if (GUILayout.Button("+ Add response")) BeginEdit(-1);
        listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Height(445));
        for (int i = 0; i < plugin.Store.Document.Rules.Count; i++)
        {
            var rule = plugin.Store.Document.Rules[i];
            string types = rule.Creatures.Count == 0 ? "All creatures" : string.Join(", ", rule.Creatures.Select(id => catalog.FirstOrDefault(c => c.Id == id)?.Label ?? id));
            GUILayout.Space(8);
            if (GUILayout.Button(rule.Name + "\n" + types, GUILayout.MinHeight(58))) BeginEdit(i);
            if (deleteIndex == i)
            {
                GUILayout.Label("Delete this saved response?");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Delete")) Delete(i);
                if (GUILayout.Button("Keep")) { deleteIndex = -1; GUIUtility.ExitGUI(); }
                GUILayout.EndHorizontal();
            }
            else if (GUILayout.Button("Remove", GUILayout.Width(85))) { deleteIndex = i; GUIUtility.ExitGUI(); }
        }
        GUILayout.EndScrollView();
        GUI.enabled = true;
        GUILayout.EndVertical();
    }

    private void BeginEdit(int index)
    {
        editIndex = index;
        deleteIndex = -1;
        draft = index < 0 ? new PetRule() : plugin.Store.Document.Rules[index].Copy();
        allTypes = draft.Creatures.Count == 0;
        search = "";
        editorScroll = Vector2.zero;
        error = plugin.Store.LoadError ?? "";
        GUIUtility.ExitGUI();
    }

    private void DrawDraft()
    {
        editorScroll = GUILayout.BeginScrollView(editorScroll, GUILayout.Height(450));
        DrawError();
        GUILayout.Label(editIndex < 0 ? "NEW RESPONSE" : "EDIT RESPONSE");
        GUILayout.Label("Exact pet name");
        draft!.Name = GUILayout.TextField(draft.Name, 100);
        GUILayout.Space(8);
        GUILayout.Label("Message · use {name} wherever the pet’s name should appear");
        draft.Message = GUILayout.TextArea(draft.Message, 300, GUILayout.Height(65));
        GUI.color = accent;
        GUILayout.Label("Preview: " + draft.Message.Replace("{name}", draft.Name));
        GUI.color = Color.white;
        GUILayout.Space(8);
        bool nextAll = GUILayout.Toggle(allTypes, "All creature types (including modded creatures)");
        if (nextAll != allTypes) { allTypes = nextAll; draft.Creatures.Clear(); }
        GUILayout.Label(allTypes ? "Turn off All creature types to choose specific creatures." : "Choose one or more types · selected cards are gold");
        var nextSearch = GUILayout.TextField(search, 80);
        if (nextSearch != search) { search = nextSearch; GUIUtility.ExitGUI(); }
        GUILayout.Label("Filter by creature name or prefab ID");
        var choices = catalog.Where(c => c.Label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || c.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        GUI.enabled = !allTypes;
        for (int row = 0; row < choices.Count; row += 3)
        {
            GUILayout.BeginHorizontal();
            foreach (var choice in choices.Skip(row).Take(3))
            {
                bool selected = allTypes || draft.Creatures.Contains(choice.Id);
                GUI.color = selected && !allTypes ? accent : new Color(0.6f, 0.65f, 0.7f);
                var rect = GUILayoutUtility.GetRect(148, 72, GUILayout.Width(148), GUILayout.Height(72));
                if (GUI.Button(rect, GUIContent.none))
                {
                    if (selected) draft.Creatures.Remove(choice.Id);
                    else draft.Creatures.Add(choice.Id);
                }
                if (choice.Icon)
                {
                    var sprite = choice.Icon!;
                    var uv = sprite.textureRect;
                    GUI.DrawTextureWithTexCoords(new Rect(rect.x + 8, rect.y + 8, 30, 30), sprite.texture,
                        new Rect(uv.x / sprite.texture.width, uv.y / sprite.texture.height, uv.width / sprite.texture.width, uv.height / sprite.texture.height));
                }
                else GUI.Label(new Rect(rect.x + 10, rect.y + 8, 28, 28), choice.Label.Substring(0, 1));
                GUI.Label(new Rect(rect.x + 44, rect.y + 8, 100, 28), selected && !allTypes ? "Selected" : "");
                GUI.Label(new Rect(rect.x + 8, rect.y + 39, 134, 30), choice.Label);
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
        }
        GUI.enabled = true;
        if (choices.Count == 0) GUILayout.Label("No matching creature types.");
        GUILayout.EndScrollView();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUI.enabled = plugin.Store.LoadError == null;
        if (GUILayout.Button("Save response", GUILayout.Height(40))) SaveDraft();
        GUI.enabled = true;
        if (GUILayout.Button("Cancel", GUILayout.Height(40))) { draft = null; error = plugin.Store.LoadError ?? ""; GUIUtility.ExitGUI(); }
        GUILayout.EndHorizontal();
    }

    private void DrawError()
    {
        if (error.Length == 0) return;
        GUI.color = new Color(1, 0.65f, 0.55f);
        GUILayout.Label(error);
        GUI.color = Color.white;
    }

    private void SaveDraft()
    {
        if (!allTypes && draft!.Creatures.Count == 0) { error = "Select at least one creature type, or enable All creature types."; GUIUtility.ExitGUI(); return; }
        var document = new RuleDocument { Rules = plugin.Store.Document.Rules.Select(r => r.Copy()).ToList() };
        var saved = draft!.Copy();
        if (allTypes) saved.Creatures.Clear();
        if (editIndex < 0) document.Rules.Add(saved); else document.Rules[editIndex] = saved;
        try { plugin.Store.Save(document); draft = null; error = ""; }
        catch (Exception ex) { error = ex is System.IO.InvalidDataException ? ex.Message : "Could not save the response. Check that the profile config folder is writable."; }
        GUIUtility.ExitGUI();
    }

    private void Delete(int index)
    {
        var document = new RuleDocument { Rules = plugin.Store.Document.Rules.Select(r => r.Copy()).ToList() };
        document.Rules.RemoveAt(index);
        try { plugin.Store.Save(document); deleteIndex = -1; error = ""; }
        catch (Exception) { error = "Could not delete the response. Check that the profile config folder is writable."; }
        GUIUtility.ExitGUI();
    }

    public void Dispose()
    {
        Close();
        foreach (var texture in textures) UnityEngine.Object.Destroy(texture);
        if (skin) UnityEngine.Object.Destroy(skin);
    }
}
