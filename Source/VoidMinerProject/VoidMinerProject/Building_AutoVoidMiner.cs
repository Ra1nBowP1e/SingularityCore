using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidMinerProject;

[StaticConstructorOnStartup]
public class Building_AutoVoidMiner : Building
{
	private static readonly CachedMaterial OrbMat = new CachedMaterial("AutoVoidMiner_attach", ShaderDatabase.Cutout);

	private const float BobHeight = 0.3f;

	private const float BobSpeed = 500f;

	private CompAutoVoidMiner compAutoMiner;

	private static readonly Mesh OrbMesh = MeshPool.GridPlane(new Vector2(0.75f, 0.75f));
	private static readonly Graphic WorkingGraphic = GraphicDatabase.Get<Graphic_Single>("AutoVoidMiner_Work", ShaderDatabase.Cutout, Vector2.one * 3f, Color.white);
	private MaterialPropertyBlock orbProperties;

	public override Graphic Graphic
	{
		get
		{
			if (compAutoMiner != null && compAutoMiner.GetCurrentState() == CompAutoVoidMiner.ProductionState.Producing)
			{
				return WorkingGraphic;
			}
			return base.Graphic;
		}
	}

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

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		compAutoMiner = GetComp<CompAutoVoidMiner>();
		if (compAutoMiner == null)
		{
			Log.Warning("[VoidExtractor] CompAutoVoidMiner not found on Building_AutoVoidMiner");
		}
	}

	public CompAutoVoidMiner.ProductionState GetCurrentState()
	{
		return compAutoMiner?.GetCurrentState() ?? CompAutoVoidMiner.ProductionState.Stopped;
	}

	public float GetProductionProgress()
	{
		return compAutoMiner?.GetProductionProgress() ?? 0f;
	}

	public int GetRemainingProductionTicks()
	{
		return compAutoMiner?.GetRemainingProductionTicks() ?? 0;
	}

	public bool SetProductionTarget(ThingDef target)
	{
		return compAutoMiner?.SetProductionTarget(target) ?? false;
	}

	public void PauseProduction()
	{
		compAutoMiner?.PauseProduction();
	}

	public void ResumeProduction()
	{
		compAutoMiner?.ResumeProduction();
	}

	public void StopProduction()
	{
		compAutoMiner?.StopProduction();
	}

	public ThingDef GetCurrentProductionTarget()
	{
		return compAutoMiner?.GetCurrentProductionTarget();
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		yield return new Command_Action
		{
			defaultLabel = "SC_SelectProductionTarget".Translate(),
			defaultDesc = "SC_SelectProductionTargetDesc".Translate(),
			icon = GetGizmoIcon(),
			action = delegate
			{
				Find.WindowStack.Add(new ProductionTargetWindow(this));
			}
		};
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
		if (OrbMat?.Material == null)
		{
			Log.WarningOnce("[SingularityCore] Void Miner orb material is missing.", 17361201);
			return;
		}
		drawLoc.z += 0.5f * (1f + Mathf.Sin(Mathf.PI * 2f * (float)GenTicks.TicksGame / 500f)) * 0.3f;
		drawLoc.y += 0.03658537f;
		Vector3 orbSize = GetOrbSize();
		Color orbColor = GetOrbColor();
		orbProperties ??= new MaterialPropertyBlock();
		orbProperties.SetColor("_Color", orbColor);
		Graphics.DrawMesh(OrbMesh, Matrix4x4.TRS(drawLoc, Quaternion.identity, orbSize), OrbMat.Material, 0, null, 0, orbProperties);
	}

	private Vector3 GetOrbSize()
	{
		Vector3 vector = new Vector3(def.graphicData.drawSize.x, 1f, def.graphicData.drawSize.y);
		if (GetCurrentState() == CompAutoVoidMiner.ProductionState.Producing)
		{
			float productionProgress = GetProductionProgress();
			float num = Mathf.Sin(productionProgress * Mathf.PI * 40f) * 0.1f + 1f;
			return vector * num;
		}
		return vector;
	}

	private Color GetOrbColor()
	{
		switch (GetCurrentState())
		{
		case CompAutoVoidMiner.ProductionState.Producing:
		{
			float productionProgress = GetProductionProgress();
			if (productionProgress <= 0.33f)
			{
				float t = productionProgress / 0.33f;
				Color a = new Color(0.5f, 0.1f, 0.1f);
				Color b = new Color(0.35f, 0.27f, 0.65f);
				return Color.Lerp(a, b, t);
			}
			if (productionProgress <= 0.66f)
			{
				float t2 = (productionProgress - 0.33f) / 0.33f;
				Color a2 = new Color(0.35f, 0.27f, 0.65f);
				Color b2 = new Color(1f, 0.9f, 0.2f);
				return Color.Lerp(a2, b2, t2);
			}
			float t3 = (productionProgress - 0.66f) / 0.34f;
			Color a3 = new Color(1f, 0.9f, 0.2f);
			Color b3 = new Color(0.6f, 0.92f, 0.04f);
			return Color.Lerp(a3, b3, t3);
		}
		case CompAutoVoidMiner.ProductionState.Paused:
			return new Color(1f, 0.6f, 0f);
		default:
			return new Color(0.3f, 0.5f, 0.8f);
		}
	}

	private Texture2D GetGizmoIcon()
	{
		return ContentFinder<Texture2D>.Get("UnfinishedMineWork", reportFailure: false);
	}

	public override string GetInspectString()
	{
		string inspectString = base.GetInspectString();
		CompPowerTrader comp = GetComp<CompPowerTrader>();
		if (comp == null || !comp.PowerOn)
		{
			return inspectString + "\n" + "NoPower".Translate();
		}
		ThingDef currentProductionTarget = GetCurrentProductionTarget();
		CompAutoVoidMiner.ProductionState currentState = GetCurrentState();
		if (currentProductionTarget == null || currentState == CompAutoVoidMiner.ProductionState.Stopped)
		{
			return inspectString + "\n" + "SC_ClickToSelectProductionTarget".Translate();
		}
		string text = "";
		switch (currentState)
		{
		case CompAutoVoidMiner.ProductionState.Producing:
		{
			float productionProgress2 = GetProductionProgress();
			int remainingProductionTicks2 = GetRemainingProductionTicks();
			string text3 = FormatTimeFromTicks(remainingProductionTicks2);
			text = "SC_StatusProgressWithRemaining".Translate("SC_StatusProducing".Translate(), productionProgress2, text3);
			break;
		}
		case CompAutoVoidMiner.ProductionState.Paused:
		{
			float productionProgress = GetProductionProgress();
			int remainingProductionTicks = GetRemainingProductionTicks();
			string text2 = FormatTimeFromTicks(remainingProductionTicks);
			text = "SC_StatusProgressWithRemaining".Translate("SC_StatusPaused".Translate(), productionProgress, text2);
			break;
		}
		case CompAutoVoidMiner.ProductionState.Stopped:
			text = "SC_ClickToSelectProductionTarget".Translate();
			break;
		}
		return inspectString + "\n" + "SC_CurrentTarget".Translate(currentProductionTarget.label) + "\n" + "SC_CurrentStatus".Translate() + ": " + text;
	}

	public override void ExposeData()
	{
		base.ExposeData();
	}
}
