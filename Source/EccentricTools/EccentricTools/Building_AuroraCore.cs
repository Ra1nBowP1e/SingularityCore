using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace EccentricTools;

[StaticConstructorOnStartup]
public class Building_AuroraCore : Building
{
	private static readonly Material GlowOverlayMaterial = MaterialPool.MatFrom("Eccentric/Things/Overlays/AuroraCore_Glow", ShaderDatabase.TransparentPostLight);

	private static readonly Material[] AuroraOverlays = new Material[6]
	{
		MaterialPool.MatFrom("Eccentric/Things/Overlays/Aurora1", ShaderDatabase.TransparentPostLight),
		MaterialPool.MatFrom("Eccentric/Things/Overlays/Aurora2", ShaderDatabase.TransparentPostLight),
		MaterialPool.MatFrom("Eccentric/Things/Overlays/Aurora3", ShaderDatabase.TransparentPostLight),
		MaterialPool.MatFrom("Eccentric/Things/Overlays/Aurora4", ShaderDatabase.TransparentPostLight),
		MaterialPool.MatFrom("Eccentric/Things/Overlays/Aurora5", ShaderDatabase.TransparentPostLight),
		MaterialPool.MatFrom("Eccentric/Things/Overlays/Aurora6", ShaderDatabase.TransparentPostLight)
	};

	private static readonly Vector3 OverlayDrawSize = new Vector3(3f, 1f, 3f);

	private static readonly Vector3 AuroraDrawSize = new Vector3(1.5f, 1f, 1.5f);

	private CompFlickable flickableComp;

	private CompPowerTrader powerComp;

	public bool IsActive
	{
		get
		{
			if (!base.Spawned)
			{
				return false;
			}
			CompFlickable compFlickable = flickableComp;
			if (compFlickable != null && !compFlickable.SwitchIsOn)
			{
				return false;
			}
			if (powerComp != null)
			{
				return powerComp.PowerOn;
			}
			return true;
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		flickableComp = GetComp<CompFlickable>();
		powerComp = GetComp<CompPowerTrader>();
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		if (IsActive)
		{
			int ticksGame = Find.TickManager.TicksGame;
			Vector3 pos = drawLoc;
			pos.y = AltitudeLayer.Building.AltitudeFor(1f);
			Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, OverlayDrawSize);
			Graphics.DrawMesh(MeshPool.plane10, matrix, GlowOverlayMaterial, 0);
			float num = 0.8f + 0.1f * Mathf.Sin((float)(ticksGame % 300) / 150f * Mathf.PI);
			int num2 = ticksGame / 10 % AuroraOverlays.Length;
			pos = drawLoc;
			pos.y = AltitudeLayer.Building.AltitudeFor(2f);
			pos.z += num;
			matrix = Matrix4x4.TRS(pos, Quaternion.identity, AuroraDrawSize);
			Graphics.DrawMesh(MeshPool.plane10, matrix, AuroraOverlays[num2], 0);
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (base.Spawned && this.GetConnectedGravship() != null)
		{
			yield return new Command_Action
			{
				defaultLabel = "EccentricCore_AutomatedLaunch".Translate(),
				defaultDesc = "EccentricCore_AutomatedLaunchDesc".Translate(),
				icon = CoreUtility.LaunchButton,
				action = StartAutomatedLaunch
			};
		}
	}

	private void StartAutomatedLaunch()
	{
		if (!base.Spawned)
		{
			return;
		}
		Building_GravEngine connectedGravship = this.GetConnectedGravship();
		if (connectedGravship == null)
		{
			return;
		}
		CompPilotConsole compPilotConsole = connectedGravship.FindPilotConsole();
		if (compPilotConsole == null)
		{
			Messages.Message("EccentricCore_AutomatedLaunchNoConsole".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		AcceptanceReport acceptanceReport = compPilotConsole.CanUseNow();
		if (!acceptanceReport.Accepted)
		{
			Messages.Message("EccentricCore_AutomatedLaunchRejected".Translate(acceptanceReport.Reason), MessageTypeDefOf.RejectInput, historical: false);
		}
		else
		{
			GravshipUtility.PreLaunchConfirmation(connectedGravship, DoAutomatedLaunch);
		}
	}

	private void DoAutomatedLaunch()
	{
		Building_GravEngine connectedGravship = this.GetConnectedGravship();
		if (connectedGravship != null)
		{
			CompPilotConsole compPilotConsole = connectedGravship.FindPilotConsole();
			if (compPilotConsole != null)
			{
				connectedGravship.launchInfo = new LaunchInfo
				{
					quality = 1f,
					doNegativeOutcome = false,
					pilot = CoreUtility.Aurora
				};
				compPilotConsole.StartChoosingDestination();
			}
		}
	}
}
