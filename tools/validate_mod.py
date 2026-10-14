#!/usr/bin/env python3
"""Check mod XML links against Core and installed official DLC (never other mods)."""
import argparse
from collections import defaultdict
from pathlib import Path
import sys
import re
import xml.etree.ElementTree as ET

SCALARS = {
    "designationCategory": "DesignationCategoryDef", "researchPrerequisite": "ResearchProjectDef",
    "requiredResearchBuilding": "ThingDef", "tab": "ResearchTabDef",
    "defaultProjectile": "ThingDef", "turretGunDef": "ThingDef", "unfinishedThingDef": "ThingDef",
    "damageDef": "DamageDef", "explosiveDamageType": "DamageDef", "explosionCellFleck": "FleckDef",
    "soundCast": "SoundDef", "soundInteract": "SoundDef", "soundDrop": "SoundDef",
    "soundExplosion": "SoundDef", "soundWorking": "SoundDef", "soundCastTail": "SoundDef",
    "fleckDef": "FleckDef", "effecterDef": "EffecterDef", "explosionEffect": "EffecterDef",
    "effectWorking": "EffecterDef", "aimingChargeMote": "ThingDef", "stateEffecter": "EffecterDef",
    "hediff": "HediffDef", "hediffSolid": "HediffDef", "capacity": "PawnCapacityDef",
    "workSkill": "SkillDef", "workSpeedStat": "StatDef", "workType": "WorkTypeDef",
    "armorCategory": "DamageArmorCategoryDef", "victimSeverityScaling": "StatDef",
}
LISTS = {
    "researchPrerequisites": "ResearchProjectDef", "prerequisites": "ResearchProjectDef",
    "requiredResearchFacilities": "ThingDef", "recipes": "RecipeDef", "recipeUsers": "ThingDef",
    "fixedBillGiverDefs": "ThingDef", "linkableFacilities": "ThingDef", "thingDefs": "ThingDef",
    "thingCategories": "ThingCategoryDef", "weaponClasses": "WeaponClassDef",
    "layers": "ApparelLayerDef", "bodyPartGroups": "BodyPartGroupDef",
    "requiredCapacities": "PawnCapacityDef", "affectedDamageDefs": "DamageDef",
}
STAT_MAPS = {"statBases", "statOffsets", "statFactors", "equippedStatOffsets", "equippedStatFactors"}
ASSET_TAGS = {"texPath", "uiIconPath", "wornGraphicPath", "texturePath", "iconPath", "modIconPath", "clipPath"}


def read_xml(path):
    try:
        return ET.parse(path).getroot()
    except (ET.ParseError, OSError) as exc:
        raise ValueError(f"{path}: {exc}") from exc


def validate(mod_dir, game_dir):
    errors = []
    known = defaultdict(set)
    parents = set()
    game_assets = set()
    mod_roots = []
    local_defs = {}
    for path in sorted((game_dir / "Data").glob("*/Defs/**/*.xml")):
        root = read_xml(path)
        if root.tag != "Defs":
            continue
        for definition in root:
            name = definition.findtext("defName")
            if name:
                known[definition.tag.lower()].add(name)
            if definition.get("Name"):
                parents.add(definition.get("Name"))
        for node in root.iter():
            if node.tag in ASSET_TAGS and node.text:
                game_assets.add(node.text.strip())
    if not known["thingdef"]:
        return ["No official game defs found; --game-dir must point to RimWorld 1.6."]
    version_path = game_dir / "Version.txt"
    if version_path.exists() and not version_path.read_text(encoding="utf-8-sig").startswith("1.6."):
        errors.append("Game installation is not RimWorld 1.6.")
    for folder in (mod_dir / "1.6", mod_dir / "Patches", mod_dir / "About", mod_dir / "Languages"):
        if not folder.exists():
            continue
        for path in sorted(folder.rglob("*.xml")):
            root = read_xml(path)
            mod_roots.append((path, root))
            if root.tag != "Defs":
                continue
            for definition in root:
                name = definition.findtext("defName")
                if name:
                    key = (definition.tag.lower(), name)
                    if key in local_defs:
                        errors.append(f"{path}: duplicate {key}; first declared in {local_defs[key]}")
                    local_defs[key] = path
                    known[key[0]].add(name)
                if definition.get("Name"):
                    parents.add(definition.get("Name"))

    def check(path, owner, value, kind):
        if value and value not in known[kind.lower()]:
            errors.append(f"{path}: {owner}: missing {kind} '{value}'")

    for path, root in mod_roots:
        if root.tag == "Defs":
            for definition in root:
                owner = definition.findtext("defName") or definition.get("Name") or definition.tag
                parent = definition.get("ParentName")
                if parent and parent not in parents:
                    errors.append(f"{path}: {owner}: missing XML parent '{parent}'")
                if definition.tag == "ThingCategoryDef":
                    check(path, owner, definition.findtext("parent"), "ThingCategoryDef")
                for node in definition.iter():
                    if node.tag in SCALARS:
                        check(path, owner, (node.text or "").strip(), SCALARS[node.tag])
                    if node.tag in LISTS:
                        for child in node:
                            check(path, owner, (child.text or "").strip(), LISTS[node.tag])
                    if node.tag in STAT_MAPS:
                        for child in node:
                            check(path, owner, child.tag, "StatDef")
                    if node.tag in {"costList", "products"}:
                        for child in node:
                            check(path, owner, child.tag, "ThingDef")
        for node in root.iter():
            if node.tag not in ASSET_TAGS or not node.text:
                continue
            asset = node.text.strip()
            folder = "Sounds" if node.tag == "clipPath" else "Textures"
            candidate = mod_dir / folder / asset
            # Multi/StackCount graphics can use directional or stack suffixes.
            exists = candidate.exists() or (candidate.parent.exists() and any(candidate.parent.glob(candidate.name + "*")))
            if not exists and asset not in game_assets:
                errors.append(f"{path}: missing local/game asset '{asset}'")
    about = read_xml(mod_dir / "About" / "About.xml")
    if not re.fullmatch(r"[a-z0-9]+(?:\.[a-z0-9_]+)+", about.findtext("packageId") or ""):
        errors.append("About.xml: packageId must use a dotted author.mod identifier.")
    for language in ("English", "Russian"):
        translations = set()
        for path in (mod_dir / "Languages" / language / "Keyed").rglob("*.xml"):
            translations.update(node.tag for node in read_xml(path))
        used = set()
        for path in (mod_dir / "Source").rglob("*.cs"):
            used.update(re.findall(r'"(SC_[A-Za-z0-9_]+)"\.Translate\(', path.read_text()))
        for key in sorted(used - translations):
            errors.append(f"{language}: missing interface translation '{key}'")
    return errors


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-dir", type=Path, required=True)
    args = parser.parse_args()
    try:
        problems = validate(Path(__file__).resolve().parents[1], args.game_dir)
    except ValueError as exc:
        problems = [str(exc)]
    for problem in problems:
        print(problem, file=sys.stderr)
    print(f"XML validation: {len(problems)} error(s)")
    sys.exit(bool(problems))
