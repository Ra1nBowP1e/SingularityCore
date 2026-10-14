# English and Russian localization

Both languages cover the mod's readable content. English remains the fallback text in the source defs and also has an explicit `DefInjected` catalog. Russian overrides those defs through `Languages/Russian/DefInjected`.

Each language has 115 def fields across nine types: buildings/items/apparel/weapons/projectiles, categories, research projects/tabs, workgivers, recipes, damage definitions and hediffs. This includes inherited stone-conversion job strings, named hediff stages, turret barrel labels and the dark-matter material adjective. UI text, the generated single-gene pack label/description and Aurora's pilot name are in `Keyed`.

The existing source lore is translated without inventing new mechanics or rewriting the setting. Company/brand names and abbreviations such as Tisiphone, Greydale, Rimsenal, YP, JI, TE and GD remain proper names. Several copied descriptions still describe planned or absent mechanics (notably kinetic slowdown); localization preserves their meaning. Progression and lore revisions remain separate work.

Sounds, motes, flecks and effecters without player-facing text have no translation entries. XML parents without `defName` cannot be injected as defs, so inherited readable text is translated on the concrete definitions. Def names, class names, save keys, texture/sound paths and diagnostic log messages are technical identifiers rather than localized UI.

RimWorld 1.6 does not expose a language-specific mod metadata description. `About.xml` therefore includes English and Russian descriptions together; the mod's brand name is unchanged.

## Checking changes

```sh
python3 tools/validate_mod.py --game-dir "/path/to/RimWorld"
python3 tools/run_runtime_checks.py --game-dir "/path/to/RimWorld" --dotnet "/path/to/dotnet" --language English
python3 tools/run_runtime_checks.py --game-dir "/path/to/RimWorld" --dotnet "/path/to/dotnet" --language Russian
```

The XML checker verifies coverage, duplicate/empty entries, matching language keys and format placeholders. Runtime checks verify that the game applies nested translations, inherited recipe text, generated crafting labels and Aurora's name. Restart the game after changing the mod files or language to rebuild cached production labels.

The Russian official DLC catalogs in the tested 1.6.4871 installation contain nine stale entries (Anomaly render-tree debug labels, revenant blood and an apparel comp path). They are recorded separately in the runtime test's `translation-errors.txt`; all 115 entries supplied by this mod are applied successfully. Official game translation files are not modified.

Some repeated headless runs on this host hit a native Boehm/Mono GC crash during startup. For the exhaustive translation inspection only, the test process was run with `GC_DONT_GC=1`. This is a temporary diagnostic environment setting, is not set by the runner or mod, and is not a performance benchmark. Both language smoke checks also passed with normal GC before that native failure recurred.
