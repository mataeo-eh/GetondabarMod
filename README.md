# Getondabar mod

Independent Valheim mod repository. Gameplay implementation has not started.

To integrate with the shared Valheim_Mods tooling:

1. Add the project, source, VERSION and CHANGELOG.md.
2. Add packaging/thunderstore/{manifest.json,README.md,icon.png} and packaging/nexusmods/README.txt.
3. Add release.config.json with this mod's Thunderstore identity and Nexus v3 IDs, plus thunderstore.toml. Use the bagpipes repository as a reference; do not reuse its package identity or Nexus IDs.
4. Adjust mod.json paths and pluginType, then set status to ready.

Build directly with dotnet, or from the parent with scripts/build.ps1 -Mod GetondabarMod.
