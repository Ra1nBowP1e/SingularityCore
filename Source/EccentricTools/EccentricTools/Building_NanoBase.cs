using RimWorld;
using UnityEngine;

namespace EccentricTools;

public abstract class Building_NanoBase : Building_WorkTable
{
	protected bool usedLastTick;

	protected int animationFrame;

	protected float directionalProgress;

	protected float armProgress;

	protected override void Tick()
	{
		base.Tick();
		if (usedLastTick)
		{
			UpdateAnimationState();
			usedLastTick = false;
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		DrawArm(drawLoc);
	}

	public override void UsedThisTick()
	{
		base.UsedThisTick();
		usedLastTick = true;
	}

	protected void UpdateAnimationState()
	{
		animationFrame++;
		if (animationFrame > 239)
		{
			animationFrame -= 240;
		}
		directionalProgress = AnimationUtility.EaseInOutCubic((float)(animationFrame % 120) / 120f);
		if (animationFrame > 119)
		{
			directionalProgress = 1f - directionalProgress;
		}
		int num = animationFrame % 80;
		armProgress = AnimationUtility.EaseInOutCubic((float)(num % 40) / 40f, -0.5f, 0.5f);
		if (num > 39)
		{
			armProgress *= -1f;
		}
	}

	protected abstract void DrawArm(Vector3 drawLoc);
}
