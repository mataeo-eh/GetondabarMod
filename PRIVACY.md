# Public source policy

Commit only source and non-secret mod metadata. Keep credentials, local configuration, game files, build outputs, logs and agent state outside this repository or ignored.

Before staging, review git ls-files -co --exclude-standard and scan for personal identifiers, absolute workstation paths and credential values. An ignore rule does not protect an already tracked file or Git history. Use a neutral repository-local commit author and email. Never copy an existing mod's public account links into this repository.

Future project files should set deterministic builds and PathMap to map the repository root to /src; inspect generated binaries for personal identifiers before distributing them. Ignoring binaries protects Git source history but does not sanitize release archives.
