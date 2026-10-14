using RimWorld;
using Verse;

namespace EccentricTools;

[DefOf]
public class CoreDefOf
{
	public static ThingDef EccentricAuroraCore;

	public static ThingDef EccentricNanofabricator;

	public static ThingDef EccentricNanoassembler;

	static CoreDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(CoreDefOf));
	}
}
