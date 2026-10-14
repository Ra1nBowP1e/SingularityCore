using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidMinerProject;

public class WorkGiver_VoidMine : WorkGiver_Scanner
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("VoidMiner"));

	public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

	public override Danger MaxPathDanger(Pawn pawn)
	{
		return Danger.Deadly;
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		List<Building_VoidMiner> list = (from b in pawn.Map.listerBuildings.allBuildingsColonist.OfType<Building_VoidMiner>()
			where b.CanStartWork() && !b.IsForbidden(pawn)
			select b).ToList();
		return list.Count == 0;
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!(t is Building_VoidMiner building_VoidMiner))
		{
			return false;
		}
		if (building_VoidMiner.IsForbidden(pawn))
		{
			return false;
		}
		if (!building_VoidMiner.CanStartWork())
		{
			return false;
		}
		if (!pawn.CanReserve(building_VoidMiner, 1, -1, null, forced))
		{
			return false;
		}
		if (building_VoidMiner.IsBurning())
		{
			return false;
		}
		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!(t is Building_VoidMiner building_VoidMiner))
		{
			return null;
		}
		List<ThingDef> availableProducts = building_VoidMiner.GetAvailableProducts();
		if (availableProducts.Count == 0)
		{
			return null;
		}
		ThingDef thingDef = availableProducts.OrderByDescending((ThingDef p) => p.BaseMarketValue).FirstOrDefault();
		if (thingDef == null)
		{
			return null;
		}
		Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("VoidMine"), building_VoidMiner);
		job.targetA = building_VoidMiner;
		job.count = 1;
		return job;
	}

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.listerBuildings.allBuildingsColonist.OfType<Building_VoidMiner>();
	}
}
