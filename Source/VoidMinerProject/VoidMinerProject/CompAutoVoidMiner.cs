using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidMinerProject;

public class CompAutoVoidMiner : ThingComp
{
	public enum ProductionState
	{
		Producing,
		Paused,
		Stopped
	}

	public enum ProductionMode
	{
		Single,
		Maintain,
		Continuous
	}

	private int productionTicks = 0;

	private ThingDef currentProductionTarget;

	private ProductionState currentState = ProductionState.Stopped;

	private string productionTargetDefNameForSave;

	private float currentWorkAmount = 0f;

	private float pausedProgress = 0f;

	private bool pausedByPowerOutage = false;

	private ProductionMode productionMode = ProductionMode.Single;

	private int productionCount = 1;

	private int completedCount = 0;

	private CompPowerTrader powerComp;
	private bool manuallyPaused;
	private bool pausedForQuantity;
	private int pendingOutputCount;
	private Thing pendingProduct;

	public CompProperties_AutoVoidMiner PropsAutoVoidMiner => (CompProperties_AutoVoidMiner)props;

	public ProductionMode CurrentProductionMode
	{
		get
		{
			return productionMode;
		}
		set
		{
			productionMode = value;
		}
	}

	public int ProductionCount
	{
		get
		{
			return productionCount;
		}
		set
		{
			productionCount = Math.Max(1, value);
		}
	}

	public int CompletedCount => completedCount;

	public static string FormatTimeFromTicks(int ticks)
	{
		float num = (float)ticks / 2500f;
		int num2 = Mathf.FloorToInt(num / 24f);
		int num3 = Mathf.FloorToInt(num % 24f);
		if (num2 > 0)
		{
			if (num3 > 0)
			{
				return "SC_TimeFormatDaysHours".Translate(num2, num3);
			}
			return "SC_TimeFormatDays".Translate(num2);
		}
		return "SC_TimeFormatHours".Translate(num3);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		powerComp = parent.GetComp<CompPowerTrader>();
		if (!respawningAfterLoad)
		{
			if (currentProductionTarget == null && currentState == ProductionState.Stopped)
			{
				SelectRandomProductionTarget();
				currentState = ProductionState.Stopped;
			}
		}
		else if (currentProductionTarget != null && currentState == ProductionState.Producing && currentWorkAmount <= 0f)
		{
			currentWorkAmount = CalculateWorkAmount(currentProductionTarget);
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		CompPowerTrader comp = powerComp;
		if (comp == null || !comp.PowerOn)
		{
			if (currentState == ProductionState.Producing)
			{
				pausedProgress = GetProductionProgress();
				currentState = ProductionState.Paused;
				pausedByPowerOutage = true;
			}
			return;
		}
		if (currentState == ProductionState.Paused && currentProductionTarget != null && pausedByPowerOutage && !manuallyPaused)
		{
			ResumeProduction();
			pausedByPowerOutage = false;
		}
		// Stagger inventory scans between buildings; idle/single mode never scans the map.
		if (productionMode == ProductionMode.Maintain && !manuallyPaused && parent.IsHashIntervalTick(250))
		{
			CheckMaintainQuantityMode();
		}
		switch (currentState)
		{
		case ProductionState.Producing:
			if (productionTicks < currentWorkAmount) productionTicks++;
			if ((float)productionTicks >= currentWorkAmount)
			{
				// Retry blocked output periodically, retaining completed work and the product.
				if (pendingOutputCount == 0 || parent.IsHashIntervalTick(250)) CompleteProduction();
			}
			break;
		case ProductionState.Paused:
			break;
		case ProductionState.Stopped:
			break;
		}
	}

	private void StartProduction()
	{
		if (currentProductionTarget == null)
		{
			SelectRandomProductionTarget();
			if (currentProductionTarget == null)
			{
				return;
			}
		}
		currentWorkAmount = CalculateWorkAmount(currentProductionTarget);
		if (currentState == ProductionState.Paused)
		{
			productionTicks = Mathf.RoundToInt(pausedProgress * currentWorkAmount);
		}
		else
		{
			productionTicks = 0;
		}
		currentState = ProductionState.Producing;
		pausedForQuantity = false;
	}

	private void CompleteProduction()
	{
		if (currentProductionTarget == null || !ProduceItems(currentProductionTarget)) return;
		completedCount++;
		if (ShouldContinueProduction())
		{
			productionTicks = 0;
			currentWorkAmount = CalculateWorkAmount(currentProductionTarget);
		}
		else
		{
			currentState = ProductionState.Stopped;
			completedCount = 0;
		}
	}

	private ThingDef GetRandomCompatibleStuff(ThingDef productDef)
	{
		if (productDef == null || !productDef.MadeFromStuff)
		{
			return null;
		}
		return GenStuff.RandomStuffFor(productDef);
	}

	private void SetRandomQuality(Thing product)
	{
		if (product.TryGetComp<CompQuality>() == null)
		{
			return;
		}
		float value = Rand.Value;
		QualityCategory qualityCategory = ((value < 0.1f) ? QualityCategory.Legendary : ((value < 0.25f) ? QualityCategory.Masterwork : ((value < 0.4f) ? QualityCategory.Excellent : ((!(value < 0.7f)) ? QualityCategory.Normal : QualityCategory.Good))));
		CompQuality compQuality = product.TryGetComp<CompQuality>();
		if (compQuality != null)
		{
			compQuality.SetQuality(qualityCategory, ArtGenerationContext.Outsider);
		}
	}

	private Thing MakeProduct(ThingDef productDef)
	{
		Thing thing;
		if (!ModsConfig.BiotechActive || productDef == null || !productDef.defName.StartsWith("genepack_"))
		{
			thing = ((productDef == null || !productDef.MadeFromStuff) ? ThingMaker.MakeThing(productDef) : ThingMaker.MakeThing(productDef, GetRandomCompatibleStuff(productDef)));
		}
		else
		{
			string defName = productDef.defName.Substring("genepack_".Length);
			GeneDef named = DefDatabase<GeneDef>.GetNamed(defName, errorOnFail: false);
			thing = ((named == null) ? ThingMaker.MakeThing(productDef) : CreateSingleGeneGenepack(named));
		}
		SetRandomQuality(thing);
		return thing;
	}

	private Thing CreateSingleGeneGenepack(GeneDef geneDef)
	{
		if (!ModsConfig.BiotechActive)
		{
			return ThingMaker.MakeThing(ThingDefOf.Steel);
		}
		Genepack genepack = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);
		List<GeneDef> genes = new List<GeneDef> { geneDef };
		genepack.Initialize(genes);
		return genepack;
	}


	private bool ProduceItems(ThingDef productDef)
	{
		if (parent.Map == null) return false;
		if (pendingOutputCount == 0) pendingOutputCount = CalculateOutputAmount(productDef);
		IntVec3 cell = parent.Position + new IntVec3(0, 0, -2);
		if (!cell.InBounds(parent.Map)) cell = parent.Position;
		while (pendingOutputCount > 0)
		{
			if (pendingProduct == null)
			{
				pendingProduct = MakeProduct(productDef);
				pendingProduct.stackCount = Math.Min(pendingOutputCount, Math.Max(1, pendingProduct.def.stackLimit));
			}
			int before = pendingProduct.stackCount;
			bool placed = GenPlace.TryPlaceThing(pendingProduct, cell, parent.Map, ThingPlaceMode.Near);
			int remaining = placed || pendingProduct.Destroyed || pendingProduct.Spawned ? 0 : pendingProduct.stackCount;
			pendingOutputCount -= before - remaining;
			if (remaining > 0) return false;
			pendingProduct = null;
		}
		return true;
	}

	private float CalculateWorkAmount(ThingDef productDef)
	{
		if (productDef == null)
		{
			return 60000f;
		}
		return VoidMinerSettings.Instance?.GetWorkAmountForProduct(productDef) ?? 60000f;
	}

	private int CalculateBaseOutputAmount(ThingDef productDef)
	{
		if (productDef == null)
		{
			return 1;
		}
		return VoidMinerSettings.Instance?.GetOutputAmountForProduct(productDef) ?? 1;
	}

	private int CalculateOutputAmount(ThingDef productDef)
	{
		if (productDef == null)
		{
			return 0;
		}
		int val = CalculateBaseOutputAmount(productDef);
		return Math.Max(val, 1);
	}

	private void SelectRandomProductionTarget()
	{
		if (currentProductionTarget != null)
		{
			return;
		}
		VoidMinerSettings instance = VoidMinerSettings.Instance;
		if (instance != null)
		{
			List<ThingDef> allowedProductDefs = instance.GetAllowedProductDefs();
			if (allowedProductDefs.Count > 0)
			{
				currentProductionTarget = allowedProductDefs[Rand.Range(0, allowedProductDefs.Count)];
			}
		}
	}


	public ProductionState GetCurrentState()
	{
		return currentState;
	}

	public float GetProductionProgress()
	{
		if (currentState == ProductionState.Producing && currentWorkAmount > 0f)
		{
			return (float)productionTicks / currentWorkAmount;
		}
		if (currentState == ProductionState.Paused)
		{
			return pausedProgress;
		}
		return 0f;
	}

	public int GetRemainingProductionTicks()
	{
		if (currentState == ProductionState.Producing && currentWorkAmount > 0f)
		{
			return Math.Max(0, (int)currentWorkAmount - productionTicks);
		}
		if (currentState == ProductionState.Paused && currentWorkAmount > 0f)
		{
			int num = Mathf.RoundToInt(pausedProgress * currentWorkAmount);
			return Math.Max(0, (int)currentWorkAmount - num);
		}
		return 0;
	}

	public void SetProductionMode(ProductionMode mode)
	{
		productionMode = mode;
		if (mode == ProductionMode.Single)
		{
			completedCount = 0;
		}
	}

	public void SetProductionCount(int count)
	{
		productionCount = Math.Max(1, count);
	}

	public void IncreaseProductionCount()
	{
		if (productionCount < int.MaxValue) productionCount++;
	}

	public void DecreaseProductionCount()
	{
		productionCount = Math.Max(1, productionCount - 1);
	}

	private void CheckMaintainQuantityMode()
	{
		if (productionMode == ProductionMode.Maintain && currentProductionTarget != null && !manuallyPaused)
		{
			int currentStorageCount = GetCurrentStorageCount();
			if (currentStorageCount < productionCount && (currentState == ProductionState.Stopped || pausedForQuantity))
			{
				StartProduction();
			}
			else if (currentStorageCount >= productionCount && currentState == ProductionState.Producing)
			{
				pausedProgress = GetProductionProgress();
				currentState = ProductionState.Paused;
				pausedForQuantity = true;
			}
		}
	}

	public int GetCurrentStorageCount()
	{
		if (currentProductionTarget == null)
		{
			return 0;
		}
		Map map = parent.Map;
		if (map == null)
		{
			return 0;
		}
		bool singleGene = currentProductionTarget.defName.StartsWith("genepack_");
		List<Thing> list = map.listerThings.ThingsOfDef(singleGene ? ThingDefOf.Genepack : currentProductionTarget);
		int num = 0;
		foreach (Thing item in list)
		{
			if (singleGene && (!(item is Genepack pack) || pack.GeneSet.GenesListForReading.Count != 1 || "genepack_" + pack.GeneSet.GenesListForReading[0].defName != currentProductionTarget.defName)) continue;
			num += item.stackCount;
		}
		return num;
	}

	private bool ShouldContinueProduction()
	{
		return productionMode switch
		{
			ProductionMode.Single => completedCount < productionCount,
			ProductionMode.Maintain => false,
			ProductionMode.Continuous => true,
			_ => false,
		};
	}

	public bool SetProductionTarget(ThingDef target)
	{
		if (pendingOutputCount > 0)
		{
			Messages.Message("SC_VoidMinerOutputBlocked".Translate(), MessageTypeDefOf.RejectInput, false);
			return false;
		}
		if (target != null && (VoidMinerSettings.Instance?.IsProductAllowed(target) ?? false))
		{
			completedCount = 0;
			manuallyPaused = false;
			pausedForQuantity = false;
			pausedByPowerOutage = false;
			currentProductionTarget = target;
			productionTicks = 0;
			pausedProgress = 0f;
			currentState = ProductionState.Stopped;
			StartProduction();
			if (powerComp == null || !powerComp.PowerOn)
			{
				currentState = ProductionState.Paused;
				pausedByPowerOutage = true;
			}
			return true;
		}
		return false;
	}

	public void PauseProduction()
	{
		manuallyPaused = true;
		pausedForQuantity = false;
		pausedByPowerOutage = false;
		if (currentState == ProductionState.Producing)
		{
			pausedProgress = GetProductionProgress();
			currentState = ProductionState.Paused;
			pausedByPowerOutage = false;
		}
	}

	public void ResumeProduction()
	{
		manuallyPaused = false;
		pausedForQuantity = false;
		if (currentState == ProductionState.Paused)
		{
			currentState = ProductionState.Producing;
			pausedByPowerOutage = false;
		}
	}

	public void StopProduction()
	{
		if (pendingOutputCount > 0)
		{
			PauseProduction();
			Messages.Message("SC_VoidMinerOutputBlocked".Translate(), MessageTypeDefOf.RejectInput, false);
			return;
		}
		currentState = ProductionState.Stopped;
		productionTicks = 0;
		pausedProgress = 0f;
		currentProductionTarget = null;
		pausedByPowerOutage = false;
		manuallyPaused = false;
		pausedForQuantity = false;
	}

	public ThingDef GetCurrentProductionTarget()
	{
		return currentProductionTarget;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			productionTargetDefNameForSave = currentProductionTarget?.defName;
		}
		Scribe_Values.Look(ref productionTargetDefNameForSave, "productionTargetDefName");
		Scribe_Values.Look(ref productionTicks, "productionTicks", 0);
		Scribe_Values.Look(ref currentState, "currentState", ProductionState.Stopped);
		Scribe_Values.Look(ref currentWorkAmount, "currentWorkAmount", 0f);
		Scribe_Values.Look(ref pausedProgress, "pausedProgress", 0f);
		Scribe_Values.Look(ref pausedByPowerOutage, "pausedByPowerOutage", defaultValue: false);
		Scribe_Values.Look(ref productionMode, "productionMode", ProductionMode.Single);
		Scribe_Values.Look(ref productionCount, "productionCount", 1);
		Scribe_Values.Look(ref completedCount, "completedCount", 0);
		Scribe_Values.Look(ref manuallyPaused, "manuallyPaused", false);
		Scribe_Values.Look(ref pausedForQuantity, "pausedForQuantity", false);
		Scribe_Values.Look(ref pendingOutputCount, "pendingOutputCount", 0);
		Scribe_Deep.Look(ref pendingProduct, "pendingProduct");
		if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
		productionCount = Math.Max(1, productionCount);
		productionTicks = Math.Max(0, productionTicks);
		currentProductionTarget = VoidMinerSettings.Instance?.ResolveProduct(productionTargetDefNameForSave);
		if (currentProductionTarget == null)
		{
			if (!string.IsNullOrEmpty(productionTargetDefNameForSave))
				Log.Warning("[SingularityCore] Void Miner target no longer exists: " + productionTargetDefNameForSave);
			currentState = ProductionState.Stopped;
			productionTicks = 0;
			currentWorkAmount = 0f;
			pausedProgress = 0f;
			pausedByPowerOutage = false;
			manuallyPaused = false;
			pausedForQuantity = false;
		}
		else if (currentWorkAmount <= 0f)
		{
			currentWorkAmount = CalculateWorkAmount(currentProductionTarget);
		}
		// Older saves did not distinguish manual pauses from inventory pauses.
		if (currentState == ProductionState.Paused && !pausedByPowerOutage && !pausedForQuantity)
			manuallyPaused = true;
	}
}
