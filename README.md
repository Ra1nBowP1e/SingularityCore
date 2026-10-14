# Singularity: Core

Standalone RimWorld **1.6** mod by Vazyan. All official DLC may be used; other mods are not required.

The planned progression starts with vanilla resources, uses Aurora Core to unlock advanced research, and culminates in black/purple equipment and the late-game Void Miner. This technical cleanup does not redesign that progression or rebalance existing item costs.

## Build

The two projects in `Source/` were recovered from the original compiled assemblies with ILSpy 9.1. Assembly names, namespaces and existing save field names are retained. The recovered EccentricTools assembly credits Aelanna Cessara; the original VoidMinerProject assembly has no author name in its metadata. Existing asset and def names retain their original naming for now.

Install a .NET SDK (8 or later) and RimWorld 1.6. Build against the game's `Managed` directory; game libraries are never copied into this repository or distributed with the mod.

```sh
dotnet build Source/EccentricTools/EccentricTools.csproj -c Release -p:RimWorldDir="/path/to/RimWorld" -p:DeployModAssemblies=true
dotnet build Source/VoidMinerProject/VoidMinerProject.csproj -c Release -p:RimWorldDir="/path/to/RimWorld" -p:DeployModAssemblies=true
```

The default path is the standard Linux Steam installation. For another layout, pass `-p:RimWorldManagedDir="/path/to/Managed"` as well. Without `DeployModAssemblies=true`, builds remain in each project's ignored `bin/` directory. The build rejects an installation whose `Version.txt` reports a version other than 1.6.

## Validation

Compile both projects and run the def checker against the 1.6 installation:

```sh
python3 tools/validate_mod.py --game-dir "/path/to/RimWorld"
python3 tools/run_runtime_checks.py --game-dir "/path/to/RimWorld" --dotnet "/path/to/dotnet"
```

The runtime runner uses the Linux 1.6 player with Core, all official DLC, this mod and a small test-only assembly. It creates its own game directory, config, logs and save data in a temporary directory, validates the actual loaded defs, and tests the actual Void Miner component. It never changes your normal mod list or saves. Null graphics mode produces Unity shader/atlas errors; the test report covers XML/code rather than rendering.

In-game checks should use only Core, official DLC and Singularity: Core. Check both worktables, Aurora research facilities, Void Miner output placement and stock counting, and weapon/turret fire. Automated checks do not replace these gameplay and rendering checks.

## Technical cleanup

- Corrected workgiver/building/research links and removed references to missing recipes.
- Removed unreachable manual-miner recipe/job code and nanofabricator code for buildings that are absent from this mod. The old map-component type remains as a harmless compatibility shim for saves.
- Restored kinetic damage using the vanilla injury worker and replaced missing weapon sounds with vanilla sounds. This does not implement an additional kinetic slowdown debuff; that remains a mechanics/design task.
- Corrected the toxic-protection patch and moved workgivers, sound defs and facility patches into their corresponding folders.
- Void Miner honors manual pauses, waits for power after target selection, retains finished output if placement is blocked, emits complete batches across multiple stacks, counts single-gene packs by their contents, and preserves cycle duration across save/load.
- Cached drawing resources and product lists, staggered stock checks every 250 ticks, limited production-list drawing to visible rows, and removed settings writes during every UI frame.
- Added English/Russian localization for all readable mod defs and UI strings with namespaced keys. See [localization coverage](docs/LOCALIZATION.md). Fixed the package ID to `vazyan.singularitycore`; enable that entry again when using a config that stored the old invalid ID.

Existing prices, damage amounts and armor values are retained. The atomic manipulator uses vanilla Fabrication as a temporary prerequisite until the full progression is designed. The singularity projector has a small forced-miss radius because the 1.6 engine requires that setting for explosive projectiles.
