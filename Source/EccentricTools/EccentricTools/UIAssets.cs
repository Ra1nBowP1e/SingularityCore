using UnityEngine;
using Verse;

namespace EccentricTools;

[StaticConstructorOnStartup]
public static class UIAssets
{
	public static readonly Material VerticalArmMaterial = MaterialPool.MatFrom("Eccentric/Things/Overlays/NanofabricatorArm_vertical", ShaderDatabase.Transparent);

	public static readonly Material VerticalHeadMaterial = MaterialPool.MatFrom("Eccentric/Things/Overlays/NanofabricatorArmHead_vertical", ShaderDatabase.Transparent);

	public static readonly Material HorizontalArmMaterial = MaterialPool.MatFrom("Eccentric/Things/Overlays/NanofabricatorArm_horizontal", ShaderDatabase.Transparent);

	public static readonly Material HorizontalHeadMaterial = MaterialPool.MatFrom("Eccentric/Things/Overlays/NanofabricatorArmHead_horizontal", ShaderDatabase.Transparent);
}
