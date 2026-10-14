using RimWorld;
using Verse;

namespace EccentricTools;

[DefOf]
public class CoreDefOf
{
	public static ThingDef EccentricAuroraCore;

	static CoreDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(CoreDefOf));
	}
}
