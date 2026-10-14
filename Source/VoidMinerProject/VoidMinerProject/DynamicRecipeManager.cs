using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VoidMinerProject;

[StaticConstructorOnStartup]
public static class DynamicRecipeManager
{
	private static List<RecipeDef> createdRecipes;

	static DynamicRecipeManager()
	{
		createdRecipes = new List<RecipeDef>();
		try
		{
			CreateAllRecipes();
		}
		catch (Exception arg)
		{
			Log.Error($"[VoidExtractor] 创建动态配方时发生错误: {arg}");
		}
	}

	public static void CreateAllRecipes()
	{
		List<ThingDef> producibleItems = GetProducibleItems();
		int num = 0;
		foreach (ThingDef item in producibleItems)
		{
			if (!RecipeExistsForItem(item))
			{
				RecipeDef recipeDef = CreateRecipeForItem(item);
				if (recipeDef != null)
				{
					DefDatabase<RecipeDef>.Add(recipeDef);
					createdRecipes.Add(recipeDef);
					num++;
				}
			}
		}
		DefDatabase<RecipeDef>.ResolveAllReferences();
	}

	public static void UpdateAllRecipes()
	{
		try
		{
			foreach (RecipeDef createdRecipe in createdRecipes)
			{
				UpdateRecipeParameters(createdRecipe);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[VoidExtractor] 更新配方时发生错误: {arg}");
		}
	}

	private static List<ThingDef> GetProducibleItems()
	{
		VoidMinerSettings instance = VoidMinerSettings.Instance;
		if (instance != null)
		{
			List<ThingDef> allowedProductDefs = instance.GetAllowedProductDefs();
			if (ModsConfig.BiotechActive)
			{
				return allowedProductDefs.Where((ThingDef td) => !td.defName.StartsWith("genepack_")).ToList();
			}
			return allowedProductDefs.ToList();
		}
		return (from td in DefDatabase<ThingDef>.AllDefs
			where td.category == ThingCategory.Item && !td.IsPlant && !td.IsCorpse && !td.IsFilth && td.BaseMarketValue > 0f && !string.IsNullOrEmpty(td.label) && !string.IsNullOrEmpty(td.description)
			orderby td.BaseMarketValue
			select td).ToList();
	}

	private static bool RecipeExistsForItem(ThingDef item)
	{
		string recipeName = "VoidMine" + item.defName;
		return DefDatabase<RecipeDef>.AllDefs.Any((RecipeDef r) => r.defName == recipeName);
	}

	private static RecipeDef CreateRecipeForItem(ThingDef item)
	{
		ThingDef named = DefDatabase<ThingDef>.GetNamed("VoidMiner", errorOnFail: false);
		if (named == null)
		{
			Log.Error("[VoidExtractor] 无法找到VoidMiner定义，跳过配方创建");
			return null;
		}
		ThingDef named2 = DefDatabase<ThingDef>.GetNamed("UnfinishedVoidWork", errorOnFail: false);
		if (named2 == null)
		{
			Log.Warning("[VoidExtractor] 无法找到UnfinishedVoidWork定义，将使用默认值");
		}
		int num = CalculateBaseOutputAmount(item);
		VoidMinerSettings instance = VoidMinerSettings.Instance;
		float workAmountForProduct;
		if (instance != null)
		{
			workAmountForProduct = instance.GetWorkAmountForProduct(item);
			workAmountForProduct *= 0.7f;
		}
		else
		{
			workAmountForProduct = item.BaseMarketValue * (float)num * 100f * 0.7f;
		}
		if (item.MadeFromStuff)
		{
			return null;
		}
		return new RecipeDef
		{
			defName = "VoidMine" + item.defName,
			label = "VoidMineRecipeLabel".Translate(item.label),
			description = "VoidMineRecipeDescription".Translate(item.label),
			jobString = "VoidMineRecipeJobString".Translate(item.label),
			workAmount = workAmountForProduct,
			ingredients = new List<IngredientCount>(),
			products = new List<ThingDefCountClass>
			{
				new ThingDefCountClass
				{
					thingDef = item,
					count = num
				}
			},
			recipeUsers = new List<ThingDef> { named },
			skillRequirements = new List<SkillRequirement>
			{
				new SkillRequirement
				{
					skill = SkillDefOf.Mining,
					minLevel = 4
				}
			},
			workSpeedStat = StatDefOf.MiningSpeed,
			effectWorking = EffecterDefOf.ConstructMetal,
			workSkill = SkillDefOf.Mining,
			workSkillLearnFactor = 0.25f,
			unfinishedThingDef = named2,
			soundWorking = SoundDefOf.Interact_ConstructDirt,
			modContentPack = LoadedModManager.GetMod<VoidMinerMod>().Content,
			defaultIngredientFilter = new ThingFilter(),
			fromIdeoBuildingPreceptOnly = false
		};
	}

	private static int CalculateBaseOutputAmount(ThingDef item)
	{
		VoidMinerSettings instance = VoidMinerSettings.Instance;
		if (instance != null)
		{
			return instance.GetOutputAmountForProduct(item);
		}
		if (item.stackLimit <= 1)
		{
			return 1;
		}
		return 1;
	}

	private static ThingDef GetRandomCompatibleStuff(ThingDef productDef)
	{
		if (productDef == null || !productDef.MadeFromStuff)
		{
			return null;
		}
		return GenStuff.RandomStuffFor(productDef);
	}

	private static QualityCategory GetRandomQuality()
	{
		float value = Rand.Value;
		if (value < 0.1f)
		{
			return QualityCategory.Legendary;
		}
		if (value < 0.25f)
		{
			return QualityCategory.Masterwork;
		}
		if (value < 0.4f)
		{
			return QualityCategory.Excellent;
		}
		if (value < 0.7f)
		{
			return QualityCategory.Good;
		}
		return QualityCategory.Normal;
	}

	private static void UpdateRecipeParameters(RecipeDef recipeDef)
	{
		if (recipeDef.products == null || recipeDef.products.Count == 0)
		{
			return;
		}
		ThingDef thingDef = recipeDef.products[0].thingDef;
		VoidMinerSettings instance = VoidMinerSettings.Instance;
		if (instance == null)
		{
			return;
		}
		int count = recipeDef.products[0].count;
		int count2 = CalculateBaseOutputAmount(thingDef);
		recipeDef.products[0].count = count2;
		float num = instance.GetWorkAmountForProduct(thingDef);
		if (recipeDef.recipeUsers != null && recipeDef.recipeUsers.Count > 0)
		{
			ThingDef thingDef2 = recipeDef.recipeUsers[0];
			if (thingDef2.defName == "VoidMiner")
			{
				num *= 0.7f;
			}
		}
		recipeDef.workAmount = num;
	}

	public static void CleanupAllRecipes()
	{
		createdRecipes.Clear();
	}
}
