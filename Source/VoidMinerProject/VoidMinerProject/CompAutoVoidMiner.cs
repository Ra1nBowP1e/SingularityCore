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

	private const int TICKS_PER_DAY = 60000;

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

	private int checkCounter = 0;

	private const int CHECK_INTERVAL = 30;

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
				return "TimeFormatDaysHours".Translate(num2, num3);
			}
			return "TimeFormatDays".Translate(num2);
		}
		return "TimeFormatHours".Translate(num3);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
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
		CompPowerTrader comp = parent.GetComp<CompPowerTrader>();
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
		if (currentState == ProductionState.Paused && currentProductionTarget != null && pausedByPowerOutage)
		{
			ResumeProduction();
			pausedByPowerOutage = false;
		}
		checkCounter++;
		if (checkCounter >= 30)
		{
			checkCounter = 0;
			CheckMaintainQuantityMode();
		}
		switch (currentState)
		{
		case ProductionState.Producing:
			productionTicks++;
			if ((float)productionTicks >= currentWorkAmount)
			{
				CompleteProduction();
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
	}

	private void CompleteProduction()
	{
		if (currentProductionTarget != null)
		{
			ProduceItems(currentProductionTarget);
		}
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
			if ((int)qualityCategory >= 4)
			{
				Log.Message("[VoidExtractor] 生产出高品质物品: " + product.Label + " - " + qualityCategory.GetLabel());
			}
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

	private ThingDef CreateDynamicSingleGeneThingDef(GeneDef geneDef)
	{
		if (geneDef == null)
		{
			return null;
		}
		ThingDef thingDef = new ThingDef();
		thingDef.defName = "genepack_" + geneDef.defName;
		thingDef.label = "Genepack(" + geneDef.label + ")";
		thingDef.description = "包含单个基因'" + geneDef.label + "'的基因组。" + geneDef.description;
		thingDef.category = ThingCategory.Item;
		thingDef.thingClass = typeof(Thing);
		thingDef.useHitPoints = true;
		thingDef.tradeability = Tradeability.All;
		thingDef.stackLimit = 1;
		thingDef.techLevel = TechLevel.Ultra;
		thingDef.graphicData = new GraphicData
		{
			texPath = "Things/Item/Special/Genepack",
			graphicClass = typeof(Graphic_Single),
			drawSize = new Vector2(0.75f, 0.75f)
		};
		thingDef.uiIconPath = "Things/Item/Special/Genepack";
		return thingDef;
	}

	private void ProduceItems(ThingDef productDef)
	{
		int num = CalculateOutputAmount(productDef);
		if (num <= 0)
		{
			return;
		}
		Thing thing = MakeProduct(productDef);
		if (productDef.stackLimit > 1)
		{
			thing.stackCount = Math.Min(num, productDef.stackLimit);
		}
		IntVec3 position = parent.Position;
		IntVec3 intVec = position + new IntVec3(0, 0, -2);
		if (intVec.InBounds(parent.Map))
		{
			if (GenPlace.TryPlaceThing(thing, intVec, parent.Map, ThingPlaceMode.Near) && thing.MarketValue > 1000f)
			{
				Messages.Message("VoidMinerProductionComplete".Translate(productDef.label, thing.stackCount), MessageTypeDefOf.PositiveEvent);
			}
		}
		else if (GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near) && thing.MarketValue > 1000f)
		{
			Messages.Message("VoidMinerProductionComplete".Translate(productDef.label, thing.stackCount), MessageTypeDefOf.PositiveEvent);
		}
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

	private void ForceSelectRandomProductionTarget()
	{
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
		productionCount++;
	}

	public void DecreaseProductionCount()
	{
		productionCount = Math.Max(1, productionCount - 1);
	}

	private void CheckMaintainQuantityMode()
	{
		if (productionMode == ProductionMode.Maintain && currentProductionTarget != null)
		{
			int currentStorageCount = GetCurrentStorageCount();
			if (currentStorageCount < productionCount && (currentState == ProductionState.Stopped || currentState == ProductionState.Paused))
			{
				StartProduction();
			}
			else if (currentStorageCount >= productionCount && currentState == ProductionState.Producing)
			{
				pausedProgress = GetProductionProgress();
				currentState = ProductionState.Paused;
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
		List<Thing> list = map.listerThings.ThingsOfDef(currentProductionTarget);
		int num = 0;
		foreach (Thing item in list)
		{
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

	public void SetProductionTarget(ThingDef target)
	{
		if (target != null && VoidMinerSettings.Instance.IsProductAllowed(target))
		{
			currentProductionTarget = target;
			productionTicks = 0;
			pausedProgress = 0f;
			CompPowerTrader comp = parent.GetComp<CompPowerTrader>();
			if (comp != null && comp.PowerOn)
			{
				StartProduction();
			}
			else
			{
				currentState = ProductionState.Stopped;
			}
		}
	}

	public void PauseProduction()
	{
		if (currentState == ProductionState.Producing)
		{
			pausedProgress = GetProductionProgress();
			currentState = ProductionState.Paused;
			pausedByPowerOutage = false;
		}
	}

	public void ResumeProduction()
	{
		if (currentState == ProductionState.Paused)
		{
			currentState = ProductionState.Producing;
			pausedByPowerOutage = false;
		}
	}

	public void StopProduction()
	{
		if (currentState == ProductionState.Producing || currentState == ProductionState.Paused)
		{
			currentState = ProductionState.Stopped;
			productionTicks = 0;
			pausedProgress = 0f;
			currentProductionTarget = null;
			pausedByPowerOutage = false;
		}
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
		if (Scribe.mode != LoadSaveMode.PostLoadInit)
		{
			return;
		}
		if (!string.IsNullOrEmpty(productionTargetDefNameForSave))
		{
			currentProductionTarget = DefDatabase<ThingDef>.GetNamed(productionTargetDefNameForSave, errorOnFail: false);
			if (ModsConfig.BiotechActive && currentProductionTarget == null && productionTargetDefNameForSave.StartsWith("genepack_"))
			{
				VoidMinerSettings instance = VoidMinerSettings.Instance;
				if (instance != null)
				{
					List<ThingDef> allowedProductDefs = instance.GetAllowedProductDefs();
					currentProductionTarget = allowedProductDefs.FirstOrDefault((ThingDef def) => def.defName == productionTargetDefNameForSave);
				}
				if (currentProductionTarget == null)
				{
					try
					{
						string defName = productionTargetDefNameForSave.Substring("genepack_".Length);
						GeneDef named = DefDatabase<GeneDef>.GetNamed(defName, errorOnFail: false);
						if (named != null)
						{
							currentProductionTarget = CreateDynamicSingleGeneThingDef(named);
						}
					}
					catch (Exception ex)
					{
						Log.Warning("[VoidMiner] Failed to create dynamic genepack for " + productionTargetDefNameForSave + ": " + ex.Message);
					}
				}
			}
			if (currentProductionTarget != null)
			{
				float num = CalculateWorkAmount(currentProductionTarget);
				currentWorkAmount = num;
			}
		}
		else
		{
			if (productionTicks <= 0 && currentState != ProductionState.Producing && currentState != ProductionState.Paused)
			{
				return;
			}
			Log.Warning("[VoidMiner] Detected corrupted save data: production progress exists but no target found. Attempting recovery...");
			bool flag = false;
			if (ModsConfig.BiotechActive)
			{
				VoidMinerSettings instance2 = VoidMinerSettings.Instance;
				if (instance2 != null)
				{
					List<ThingDef> allowedProductDefs2 = instance2.GetAllowedProductDefs();
					List<ThingDef> list = allowedProductDefs2.Where((ThingDef def) => def.defName.StartsWith("genepack_")).ToList();
					Log.Message($"[VoidMiner] Found {list.Count} genepacks available for recovery");
					if (list.Count > 0)
					{
						List<ThingDef> source = list.OrderBy((ThingDef def) => def.defName).ToList();
						currentProductionTarget = source.FirstOrDefault();
						currentWorkAmount = CalculateWorkAmount(currentProductionTarget);
						flag = true;
						Log.Message("[VoidMiner] Smart recovery: selected genepack " + currentProductionTarget.defName + " (attempting to restore progress)");
					}
					else
					{
						Log.Warning("[VoidMiner] No genepacks found in settings for recovery");
					}
				}
				else
				{
					Log.Warning("[VoidMiner] Settings instance is null, cannot recover genepacks");
				}
			}
			if (!flag)
			{
				Log.Message("[VoidMiner] Genepack recovery failed, trying force random recovery...");
				ForceSelectRandomProductionTarget();
				if (currentProductionTarget != null)
				{
					currentWorkAmount = CalculateWorkAmount(currentProductionTarget);
					Log.Message($"[VoidMiner] Force random recovery successful: assigned {currentProductionTarget.defName} with {productionTicks}/{currentWorkAmount} progress");
					return;
				}
				Log.Warning("[VoidMiner] Force random recovery also failed: no valid targets available");
				Log.Warning("[VoidMiner] Recovery failed: no valid target available. Resetting production state.");
				currentState = ProductionState.Stopped;
				productionTicks = 0;
				currentWorkAmount = 0f;
				pausedProgress = 0f;
			}
		}
	}
}
