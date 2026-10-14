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
```

In-game checks should use only Core, official DLC and Singularity: Core. Check the startup log, both worktables, Aurora research facilities, Void Miner production/pause/power loss/save-load, and weapon/turret fire. Static validation and compilation do not replace these gameplay checks.
