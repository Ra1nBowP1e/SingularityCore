"""Enumerate the mod's own readable def fields, including inherited recipe text."""
import xml.etree.ElementTree as ET

TEXT_FIELDS = {
    "label", "description", "jobString", "reportString", "deathMessage", "verb", "gerund",
    "fuelLabel", "fuelGizmoLabel", "outOfFuelMessage", "stuffAdjective",
}


def required_def_keys(mod_dir):
    definitions = []
    parents = {}
    for path in sorted((mod_dir / "1.6" / "Defs").rglob("*.xml")):
        for definition in ET.parse(path).getroot():
            definitions.append(definition)
            if definition.get("Name"):
                parents[definition.get("Name")] = definition

    def fields(definition, chain=()):
        if definition.get("Name") in chain:
            raise ValueError("Cyclic XML inheritance in localization coverage")
        parent = parents.get(definition.get("ParentName"))
        result = fields(parent, chain + (definition.get("Name"),)) if parent is not None else set()

        def visit(node, prefix=""):
            for child in node:
                field = prefix + child.tag
                if child.tag in TEXT_FIELDS and child.text and child.text.strip():
                    result.add(field)
                elif child.tag == "comps":
                    for comp in child:
                        if comp.get("Class") == "CompProperties_Refuelable":
                            visit(comp, prefix + "comps.CompRefuelable.")
                elif child.tag == "stages":
                    for index, stage in enumerate(child):
                        label = stage.findtext("label")
                        name = label.replace(" ", "_") if label else str(index)
                        visit(stage, prefix + "stages." + name + ".")
                elif child.tag == "stuffProps":
                    visit(child, field + ".")
        visit(definition)
        return result

    keys = set()
    for definition in definitions:
        name = definition.findtext("defName")
        if name:
            keys.update((definition.tag, name + "." + field) for field in fields(definition))
    return keys
