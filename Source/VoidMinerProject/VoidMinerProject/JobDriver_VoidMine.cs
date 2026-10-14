using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace VoidMinerProject;

public class JobDriver_VoidMine : JobDriver
{
	private const TargetIndex BuildingIndex = TargetIndex.A;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
		if (job.GetTarget(TargetIndex.A).Thing is Building_VoidMiner building)
		{
			int workAmount = (int)building.CalculateWorkAmountForJob();
			yield return Toils_General.Wait(workAmount, TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell)
				.WithProgressBarToilDelay(TargetIndex.A)
				.WithEffect(() => EffecterDefOf.ConstructMetal, TargetIndex.A)
				.PlaySustainerOrSound(SoundDefOf.Interact_ConstructDirt);
		}
		else
		{
			yield return Toils_General.Wait(1000, TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell)
				.WithProgressBarToilDelay(TargetIndex.A)
				.PlaySustainerOrSound(SoundDefOf.Interact_ConstructDirt);
		}
	}

	public override void Notify_Starting()
	{
		base.Notify_Starting();
		if (job.GetTarget(TargetIndex.A).Thing is Building_VoidMiner building_VoidMiner)
		{
			building_VoidMiner.Notify_ProductionStarted();
		}
	}

	public override void Notify_PatherArrived()
	{
		base.Notify_PatherArrived();
		Building_VoidMiner building_VoidMiner = job.GetTarget(TargetIndex.A).Thing as Building_VoidMiner;
		if (building_VoidMiner == null)
		{
		}
	}
}
