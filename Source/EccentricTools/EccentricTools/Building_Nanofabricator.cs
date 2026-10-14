using UnityEngine;
using Verse;

namespace EccentricTools;

public class Building_Nanofabricator : Building_NanoBase
{
	private static readonly Vector3 ArmDrawSize = new Vector3(1f, 1f, 2f);

	private static readonly Vector3 HorizontalArmDrawSize = new Vector3(2f, 1f, 1f);

	private static readonly Vector3 HeadDrawSize = Vector3.one;

	protected override void DrawArm(Vector3 drawLoc)
	{
		Vector3 vector = drawLoc;
		Vector3 pos = vector;
		Vector3 s = ArmDrawSize;
		vector.y = AltitudeLayer.BuildingOnTop.AltitudeFor(1f);
		pos.y = AltitudeLayer.BuildingOnTop.AltitudeFor(2f);
		Material material;
		Material material2;
		switch (base.Rotation.AsInt)
		{
		default:
			vector.x += directionalProgress - 0.5f;
			pos.x += directionalProgress - 0.5f;
			pos.z += armProgress;
			material = UIAssets.VerticalArmMaterial;
			material2 = UIAssets.VerticalHeadMaterial;
			break;
		case 1:
			vector.z += -0.2f + 0.8f * directionalProgress;
			pos.z += -0.2f + 0.8f * directionalProgress;
			pos.x -= armProgress;
			s = HorizontalArmDrawSize;
			material = UIAssets.HorizontalArmMaterial;
			material2 = UIAssets.HorizontalHeadMaterial;
			break;
		case 2:
			vector.x -= directionalProgress - 0.5f;
			pos.x -= directionalProgress - 0.5f;
			pos.z -= armProgress;
			material = UIAssets.VerticalArmMaterial;
			material2 = UIAssets.VerticalHeadMaterial;
			break;
		case 3:
			vector.z += -0.2f + 0.8f * (1f - directionalProgress);
			pos.z += -0.2f + 0.8f * (1f - directionalProgress);
			pos.x += armProgress;
			s = HorizontalArmDrawSize;
			material = UIAssets.HorizontalArmMaterial;
			material2 = UIAssets.HorizontalHeadMaterial;
			break;
		}
		Matrix4x4 matrix = Matrix4x4.TRS(vector, Quaternion.identity, s);
		Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
		matrix = Matrix4x4.TRS(pos, Quaternion.identity, HeadDrawSize);
		Graphics.DrawMesh(MeshPool.plane10, matrix, material2, 0);
	}
}
