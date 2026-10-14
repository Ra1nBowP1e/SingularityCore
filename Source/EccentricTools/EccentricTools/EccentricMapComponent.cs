using RimWorld;
using Verse;

namespace EccentricTools;

public class EccentricMapComponent : MapComponent
{
	private static void FixAuroraCores(Map map)
	{
		foreach (Building item in map.listerBuildings.AllBuildingsColonistOfDef(CoreDefOf.EccentricAuroraCore))
		{
			if (!(item is Building_AuroraCore) && item.Spawned)
			{
				CopyBuilding(map, item, CoreDefOf.EccentricAuroraCore);
			}
		}
	}

	private static void FixNanofabricators(Map map)
	{
		foreach (Building item in map.listerBuildings.AllBuildingsColonistOfDef(CoreDefOf.EccentricNanofabricator))
		{
			if (!(item is Building_Nanofabricator) && item.Spawned)
			{
				CopyWorkBench(map, item, CoreDefOf.EccentricNanofabricator);
			}
		}
	}

	private static void FixNanoassemblers(Map map)
	{
		foreach (Building item in map.listerBuildings.AllBuildingsColonistOfDef(CoreDefOf.EccentricNanoassembler))
		{
			if (!(item is Building_Nanoassembler) && item.Spawned)
			{
				CopyWorkBench(map, item, CoreDefOf.EccentricNanoassembler);
			}
		}
	}

	private static void CopyBuilding(Map map, Thing oldBuilding, ThingDef def)
	{
		int hitPoints = oldBuilding.HitPoints;
		IntVec3 position = oldBuilding.Position;
		Thing thing = ThingMaker.MakeThing(def);
		thing.HitPoints = hitPoints;
		thing.SetFaction(Faction.OfPlayer);
		oldBuilding.Destroy();
		GenSpawn.Spawn(thing, position, map);
	}

	private static void CopyWorkBench(Map map, Thing oldBuilding, ThingDef def)
	{
		int hitPoints = oldBuilding.HitPoints;
		IntVec3 position = oldBuilding.Position;
		Building_WorkTable to = (Building_WorkTable)ThingMaker.MakeThing(def);
		to.HitPoints = hitPoints;
		to.Rotation = oldBuilding.Rotation;
		to.SetFaction(Faction.OfPlayer);
		CopyBills((Building_WorkTable)oldBuilding, ref to);
		oldBuilding.Destroy();
		GenSpawn.Spawn(to, position, map);
	}

	private static void CopyBills(Building_WorkTable from, ref Building_WorkTable to)
	{
		foreach (Bill bill in from.BillStack.Bills)
		{
			to.BillStack.AddBill(bill.Clone());
		}
	}

	public EccentricMapComponent(Map map)
		: base(map)
	{
	}

	public override void FinalizeInit()
	{
		base.FinalizeInit();
		FixAuroraCores(map);
		FixNanofabricators(map);
		FixNanoassemblers(map);
	}
}
