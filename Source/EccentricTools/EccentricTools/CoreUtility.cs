using RimWorld;
using UnityEngine;
using Verse;

namespace EccentricTools;

[StaticConstructorOnStartup]
public static class CoreUtility
{
	public static readonly Texture2D LaunchButton = ContentFinder<Texture2D>.Get("UI/Commands/LaunchGravship", reportFailure: false);

	private static Pawn aurora;

	public static Pawn Aurora => aurora ?? (aurora = GenerateAuroraPawn());

	private static Pawn GenerateAuroraPawn()
	{
		Pawn pawn = (Pawn)ThingMaker.MakeThing(ThingDefOf.Human);
		PawnComponentsUtility.CreateInitialComponents(pawn);
		pawn.Name = new NameTriple("SC_AuroraName".Translate(), "SC_AuroraName".Translate(), "AP-0176");
		pawn.gender = Gender.Female;
		if (ModsConfig.BiotechActive)
		{
			pawn.genes.SetXenotype(XenotypeDefOf.Baseliner);
		}
		pawn.kindDef = PawnKindDefOf.Colonist;
		pawn.apparel.DestroyAll();
		return pawn;
	}

	public static Building_GravEngine GetConnectedGravship(this Thing thing)
	{
		if (ModsConfig.OdysseyActive)
		{
			Map map = thing.Map;
			if (map != null)
			{
				CellRect cellRect = thing.OccupiedRect();
				foreach (Thing item in map.listerThings.ThingsOfDef(ThingDefOf.GravEngine))
				{
					if (!(item is Building_GravEngine building_GravEngine))
					{
						continue;
					}
					bool flag = true;
					foreach (IntVec3 item2 in cellRect)
					{
						if (!building_GravEngine.AllConnectedSubstructure.Contains(item2))
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						return building_GravEngine;
					}
				}
				return null;
			}
		}
		return null;
	}

	public static CompPilotConsole FindPilotConsole(this Building_GravEngine engine)
	{
		if (engine == null)
		{
			return null;
		}
		foreach (CompGravshipFacility gravshipComponent in engine.GravshipComponents)
		{
			if (gravshipComponent is CompPilotConsole result)
			{
				return result;
			}
		}
		return null;
	}
}
