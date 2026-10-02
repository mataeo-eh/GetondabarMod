# Getondabar

Your pets have opinions. Inspired by an idea from Valheim content creator **ThreadMenace**, Getondabar replaces a named pet's response with something a little less affectionate.

With a fresh installation, pet any tamed creature named **Getondabar** and it says:

> Getondabar thinks you are an ugo

**ThreadMenace thought up the original joke and mod idea.** Getondabar was created as a tribute to that idea. No setup is required for the joke to work.

## Make it your own

Press **F7** while in a world to open the response editor. The shortcut is configurable in the BepInEx config.

1. Choose **Add response**, or click an existing response to edit it. New responses are prefilled with the original joke.
2. Enter the pet's **exact name**, including capitalization and spaces.
3. Keep **All creature types** enabled, or turn it off and select any combination of creature cards. Selected cards turn gold. Search by creature name or prefab ID. The catalog discovers tameable creatures from the loaded game, including modded creatures; optional trophy art and initial badges identify the cards.
4. Write the full message. Use **{name}** to insert the pet's name. The preview updates as you type.
5. Click **Save response** to apply and persist it. **Cancel**, **Close**, **Escape**, or F7 discards an unsaved draft.

Create as many responses as you need. A wolf and a boar named Bob can have different messages; a single rule can also give wolves, hens and lox named Bob the same message. An exact creature rule overrides an all-creatures rule for the same name. Overlapping selections for the same name are rejected so results stay predictable. Remove a rule with **Remove**, then confirm **Delete**.

Messages are plain text, up to 300 characters. Names are case-sensitive and match the assigned tame name; unnamed creatures and other names keep their normal responses. The default rule can be edited or deleted and will not reappear after deletion.

## Installation and multiplayer

Requires **BepInExPack Valheim 5.4.2350**. Install through Thunderstore, or put `GetondabarMod.dll` in `BepInEx/plugins/GetondabarMod/`.

This is a client-side, profile-local text mod. Each player sees their own configured responses; a dedicated server does not need it. It does not change creature names, taming, ownership, pet effects, cooldowns or follow/stay commands. Commandable pets still follow/stay normally and show the custom response after that interaction. Mods that replace or suppress vanilla petting may prevent the message from appearing.

Settings live in `BepInEx/config/Therunner.Getondabar.cfg`; saved responses live in `BepInEx/config/Therunner.Getondabar.rules.json`. Saving replaces the JSON atomically and retains the previous file as `.bak`. A malformed or unsupported rules file is preserved, with custom responses disabled; repair or move that file and restart. Disabling **General / Enabled** restores normal messages while leaving the editor available.
