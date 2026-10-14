using Verse;

namespace VoidMinerProject;

public class UnfinishedVoidWork : UnfinishedThing
{
	public ThingDef targetProduct;

	public float workProgress;

	public Building_VoidMiner sourceBuilding;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref targetProduct, "targetProduct");
		Scribe_Values.Look(ref workProgress, "workProgress", 0f);
		Scribe_References.Look(ref sourceBuilding, "sourceBuilding");
	}

	public override string GetInspectString()
	{
		string text = base.GetInspectString();
		if (targetProduct != null)
		{
			text += "\n" + "TargetProduct".Translate() + ": {targetProduct.label}";
			text += "\n" + "WorkProgress".Translate() + ": {(workProgress * 100):F1}%";
		}
		return text;
	}

	public string GetVoidWorkDescription()
	{
		if (targetProduct != null)
		{
			return "UnfinishedVoidWorkWithProduct".Translate(targetProduct.label);
		}
		return "UnfinishedVoidWork".Translate();
	}

	public bool CanContinueWork()
	{
		return sourceBuilding != null && sourceBuilding.Spawned && sourceBuilding.CanStartWork();
	}

	public float GetRemainingWork()
	{
		if (targetProduct == null)
		{
			return 0f;
		}
		VoidMinerSettings instance = VoidMinerSettings.Instance;
		float workAmountForProduct = instance.GetWorkAmountForProduct(targetProduct);
		return workAmountForProduct * (1f - workProgress);
	}
}
