using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidMinerProject;

public class Building_VoidMiner : Building_WorkTable
{
	private const float BobHeight = 0.3f;

	private const float BobSpeed = 500f;

	private UnfinishedVoidWork currentUnfinishedWork;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		try
		{
			DynamicRecipeManager.UpdateAllRecipes();
		}
		catch (Exception arg)
		{
			Log.Error($"[VoidExtractor] 初始化动态配方时发生错误: {arg}");
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		if (base.Spawned)
		{
			DrawOrb(drawLoc);
		}
	}

	private void DrawOrb(Vector3 drawLoc)
	{
		drawLoc.z += 0.5f * (1f + Mathf.Sin(Mathf.PI * 2f * (float)GenTicks.TicksGame / 500f)) * 0.3f;
		drawLoc.y += 0.03658537f;
		Vector3 s = new Vector3(def.graphicData.drawSize.x, 1f, def.graphicData.drawSize.y);
		CachedMaterial cachedMaterial = new CachedMaterial("AutoVoidMiner_attach", ShaderDatabase.Cutout, new Color(0.35f, 0.27f, 0.65f));
		Graphics.DrawMesh(MeshPool.GridPlane(new Vector2(0.75f, 0.75f)), Matrix4x4.TRS(drawLoc, Quaternion.identity, s), cachedMaterial.Material, 0, null, 0);
	}

	public bool CanStartWork()
	{
		CompPowerTrader comp = GetComp<CompPowerTrader>();
		if (comp == null || !comp.PowerOn)
		{
			return false;
		}
		return true;
	}

	public List<ThingDef> GetAvailableProducts()
	{
		VoidMinerSettings instance = VoidMinerSettings.Instance;
		if (instance != null)
		{
			return instance.GetAllowedProductDefs();
		}
		return new List<ThingDef>();
	}

	public float CalculateWorkAmountForJob()
	{
		VoidMinerSettings instance = VoidMinerSettings.Instance;
		if (instance != null)
		{
			return 42000f;
		}
		return 42000f;
	}

	public void Notify_ProductionStarted()
	{
	}

	public UnfinishedVoidWork GetUnfinishedWork()
	{
		if (base.Map == null)
		{
			return null;
		}
		return base.Map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("UnfinishedVoidWork")).OfType<UnfinishedVoidWork>().FirstOrDefault((UnfinishedVoidWork u) => u.sourceBuilding == this);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref currentUnfinishedWork, "currentUnfinishedWork");
	}
}
