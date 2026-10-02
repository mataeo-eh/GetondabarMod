# In-game acceptance check

Static tests do not establish that the UI renders correctly or that gameplay works. Use the Test_Mods profile, with BepInEx and this mod installed, for these checks. Restart Valheim after deployment.

1. Verify the BepInEx log reports Getondabar 0.1.1 and has no Getondabar/Harmony exceptions.
2. Name a tamed boar Getondabar. Pet it: the normal pet effect plays and the center message reads `Getondabar thinks you are an ugo`. Other names, lowercase `getondabar`, unnamed animals and wild animals keep vanilla behavior. Holding Use, renaming and petting within the normal cooldown must not trigger a new response.
3. Repeat with a named wolf, lox, hen and asksvin where available. Commandable animals still follow/stay; the custom response appears after the command. The mod does not add taming or pet interactions to creatures that lack them.
4. Press F7. Verify the window fits your screen, text is readable, the cursor is free and typing/clicking/scrolling does not move, attack or rotate/zoom the player camera. Escape closes only the editor. Check a smaller window and a larger resolution.
5. Add Bob for Wolf + Hen using the selectable cards and preview. Save `{name} wants snacks`. Add Bob for Boar with `Bob says oink`. Pet each type: the correct response appears. Add an all-types Bob rule and confirm specific types win, while an unselected type uses the fallback.
6. Try saving an overlapping Bob/Wolf selection, an empty name/message and a specific rule with no selected types. A clear error appears and the previously saved behavior stays active.
7. Edit Bob without saving; use Cancel, Close, Escape and F7 in separate attempts. No attempt changes the saved response. Delete only after confirming. Restart and verify saved rules survive and deleted defaults stay deleted.
8. Test with another client: each client sees its own rules and vanilla ownership/follow behavior remains shared. Test any installed tame-overhaul mods separately.
9. Back up the rules file, then test malformed JSON while the game is stopped. Restart: overrides are disabled, the error is shown/logged, and the file is preserved. Restore the backup afterward.

Record the game version, profile mods, resolution and focused BepInEx log if reporting failures.
