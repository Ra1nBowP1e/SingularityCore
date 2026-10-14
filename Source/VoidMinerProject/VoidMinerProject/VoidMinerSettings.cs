using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidMinerProject;

public class VoidMinerSettings : ModSettings
{
	private const int MAX_SEARCH_RESULTS = 50;

	private static List<ThingDef> _cachedSingleGeneItems = null;

	private static bool _singleGeneCacheValid = false;

	private static Dictionary<string, List<ThingDef>> _searchCache = new Dictionary<string, List<ThingDef>>();

	private static string _lastSearchText = null;

	private static List<ThingDef> _lastSearchResult = null;

	public float globalOutputMultiplier = 1f;

	public float globalTimeMultiplier = 1f;

	public float outputMin = 1f;

	public bool enableUnfinishedWork = true;

	public float unfinishedWorkMass = 1f;

	public int unfinishedWorkHitPoints = 50;

	public bool enableAutoProduction = false;

	public int autoProductionCooldown = 60000;

	public List<string> allowedProducts = new List<string>();

	private static readonly string[] DefaultAllowedProducts = new string[9] { "Silver", "Gold", "Plasteel", "ComponentIndustrial", "ComponentSpacer", "Uranium", "Jade", "WoodLog", "Steel" };

	private static readonly List<ValueTierConfig> ValueTiers = new List<ValueTierConfig>
	{
		new ValueTierConfig(0f, 5f, 25, 4f),
		new ValueTierConfig(5f, 15f, 20, 6f),
		new ValueTierConfig(15f, 50f, 10, 8f),
		new ValueTierConfig(50f, 150f, 5, 16f),
		new ValueTierConfig(150f, 500f, 1, 24f),
		new ValueTierConfig(500f, 1500f, 1, 48f),
		new ValueTierConfig(1500f, 5000f, 1, 96f),
		new ValueTierConfig(5000f, 15000f, 1, 168f),
		new ValueTierConfig(15000f, 50000f, 1, 216f),
		new ValueTierConfig(50000f, 150000f, 1, 264f),
		new ValueTierConfig(150000f, 500000f, 1, 336f),
		new ValueTierConfig(500000f, float.MaxValue, 1, 336f)
	};

	public static bool IsBiotechEnabled => ModsConfig.BiotechActive;

	public static VoidMinerSettings Instance { get; private set; }

	public float OutputMin => outputMin;

	public float GlobalOutputMultiplier => globalOutputMultiplier;

	public float GlobalTimeMultiplier => globalTimeMultiplier;

	public event Action OnSettingsChanged;

	public VoidMinerSettings()
	{
		Instance = this;
		if (allowedProducts == null)
		{
			allowedProducts = new List<string>();
		}
		InitializeDefaultSettings();
	}

	private void InitializeDefaultSettings()
	{
		if (allowedProducts == null)
		{
			allowedProducts = new List<string>();
		}
		if (allowedProducts.Count == 0)
		{
			allowedProducts.AddRange(DefaultAllowedProducts);
			if (!IsBiotechEnabled)
			{
				return;
			}
			List<GeneDef> allDefsListForReading = DefDatabase<GeneDef>.AllDefsListForReading;
			if (allDefsListForReading.NullOrEmpty())
			{
				return;
			}
			{
				foreach (GeneDef item3 in allDefsListForReading)
				{
					string item = "genepack_" + item3.defName;
					if (!allowedProducts.Contains(item))
					{
						allowedProducts.Add(item);
					}
				}
				return;
			}
		}
		if (!IsBiotechEnabled)
		{
			return;
		}
		List<GeneDef> allDefsListForReading2 = DefDatabase<GeneDef>.AllDefsListForReading;
		if (allDefsListForReading2.NullOrEmpty())
		{
			return;
		}
		foreach (GeneDef item4 in allDefsListForReading2)
		{
			string item2 = "genepack_" + item4.defName;
			if (!allowedProducts.Contains(item2))
			{
				allowedProducts.Add(item2);
			}
		}
	}

	public bool IsProductAllowed(ThingDef product)
	{
		return allowedProducts.Contains(product.defName);
	}

	public void AddAllowedProduct(string productDefName)
	{
		if (!allowedProducts.Contains(productDefName))
		{
			allowedProducts.Add(productDefName);
		}
	}

	public void UpdateAllSingleGeneGenepacks()
	{
		if (!IsBiotechEnabled)
		{
			return;
		}
		List<GeneDef> allDefsListForReading = DefDatabase<GeneDef>.AllDefsListForReading;
		if (allDefsListForReading.NullOrEmpty())
		{
			return;
		}
		foreach (GeneDef item2 in allDefsListForReading)
		{
			string item = "genepack_" + item2.defName;
			if (!allowedProducts.Contains(item))
			{
				allowedProducts.Add(item);
			}
		}
	}

	public void RemoveAllowedProduct(string productDefName)
	{
		allowedProducts.Remove(productDefName);
	}

	public List<ThingDef> GetAllowedProductDefs()
	{
		List<ThingDef> list = new List<ThingDef>();
		List<ThingDef> list2 = CreateSingleGeneGenepackItems();
		foreach (string defName in allowedProducts)
		{
			ThingDef named = DefDatabase<ThingDef>.GetNamed(defName, errorOnFail: false);
			if (named != null)
			{
				list.Add(named);
				continue;
			}
			ThingDef thingDef = list2.FirstOrDefault((ThingDef def) => def.defName == defName);
			if (thingDef != null)
			{
				list.Add(thingDef);
			}
		}
		return list;
	}

	public List<ThingDef> GetProducibleItems()
	{
		List<ThingDef> list = (from td in DefDatabase<ThingDef>.AllDefs
			where td.category == ThingCategory.Item && !td.IsPlant && !td.IsCorpse && !td.IsFilth && td.BaseMarketValue > 0f && !string.IsNullOrEmpty(td.label) && !string.IsNullOrEmpty(td.description)
			orderby td.BaseMarketValue
			select td).ToList();
		list.AddRange(CreateSingleGeneGenepackItems());
		return list;
	}

	private List<ThingDef> CreateSingleGeneGenepackItems()
	{
		if (!IsBiotechEnabled)
		{
			_cachedSingleGeneItems = new List<ThingDef>();
			_singleGeneCacheValid = true;
			return _cachedSingleGeneItems;
		}
		if (_singleGeneCacheValid && _cachedSingleGeneItems != null)
		{
			return _cachedSingleGeneItems;
		}
		List<ThingDef> list = new List<ThingDef>();
		List<GeneDef> allDefsListForReading = DefDatabase<GeneDef>.AllDefsListForReading;
		if (allDefsListForReading.NullOrEmpty())
		{
			_cachedSingleGeneItems = list;
			_singleGeneCacheValid = true;
			return list;
		}
		foreach (GeneDef item2 in allDefsListForReading)
		{
			ThingDef thingDef = new ThingDef();
			thingDef.defName = "genepack_" + item2.defName;
			thingDef.label = "SingleGeneGenepackLabel".Translate(item2.label);
			thingDef.description = "SingleGeneGenepackDescription".Translate(item2.label, item2.description);
			thingDef.category = ThingCategory.Item;
			thingDef.BaseMarketValue = Math.Max((float)item2.biostatCpx * 100f, 100f);
			thingDef.stackLimit = 1;
			thingDef.techLevel = TechLevel.Archotech;
			thingDef.isUnfinishedThing = false;
			thingDef.ingestible = null;
			thingDef.stuffProps = null;
			thingDef.graphicData = new GraphicData
			{
				texPath = "Things/Item/Special/Genepack",
				graphicClass = typeof(Graphic_Genepack),
				shaderType = ShaderTypeDefOf.Cutout,
				color = Color.white,
				drawSize = new Vector2(0.75f, 0.75f)
			};
			thingDef.uiIconPath = "Things/Item/Special/Genepack";
			thingDef.drawGUIOverlay = true;
			thingDef.generated = true;
			thingDef.selectable = true;
			thingDef.alwaysHaulable = true;
			thingDef.rotatable = false;
			thingDef.thingCategories = new List<ThingCategoryDef> { DefDatabase<ThingCategoryDef>.GetNamed("Items", errorOnFail: false) }.Where((ThingCategoryDef t) => t != null).ToList();
			thingDef.resourceReadoutPriority = ResourceCountPriority.Middle;
			thingDef.tradeTags = new List<string> { "Exotic" };
			ThingDef item = thingDef;
			list.Add(item);
		}
		_cachedSingleGeneItems = list.OrderBy((ThingDef td) => td.BaseMarketValue).ToList();
		_singleGeneCacheValid = true;
		return _cachedSingleGeneItems;
	}

	public static void ClearSingleGeneCache()
	{
		_cachedSingleGeneItems = null;
		_singleGeneCacheValid = false;
		_searchCache.Clear();
		_lastSearchText = null;
		_lastSearchResult = null;
	}

	public List<ThingDef> GetFilteredItemsForAutoMiner(string searchText = null)
	{
		VoidMinerSettings instance = Instance;
		if (instance == null)
		{
			return new List<ThingDef>();
		}
		if (string.IsNullOrEmpty(searchText))
		{
			if (_lastSearchText == null && _lastSearchResult != null)
			{
				return _lastSearchResult;
			}
			List<ThingDef> list = (from p in GetAllowedProductDefs()
				where !string.IsNullOrEmpty(p.label) && p.BaseMarketValue > 0f
				orderby p.BaseMarketValue descending
				select p).ToList();
			_lastSearchText = null;
			_lastSearchResult = list;
			return list;
		}
		if (_searchCache.TryGetValue(searchText, out var value))
		{
			return value;
		}
		List<ThingDef> producibleItems = GetProducibleItems();
		string searchLower = searchText.ToLower();
		List<ThingDef> list2 = (from p in producibleItems
			where IsItemMatch(p, searchLower)
			orderby CalculateRelevanceScore(p, searchLower) descending
			select p).Take(50).ToList();
		if (_searchCache.Count > 50)
		{
			_searchCache.Clear();
		}
		_searchCache[searchText] = list2;
		_lastSearchText = searchText;
		_lastSearchResult = list2;
		return list2;
	}

	private bool IsItemMatch(ThingDef item, string searchLower)
	{
		return (!string.IsNullOrEmpty(item.label) && item.label.ToLower().Contains(searchLower)) || item.defName.ToLower().Contains(searchLower) || (!string.IsNullOrEmpty(item.description) && item.description.ToLower().Contains(searchLower));
	}

	private double CalculateRelevanceScore(ThingDef item, string searchLower)
	{
		double num = 0.0;
		if (!string.IsNullOrEmpty(item.label) && item.label.ToLower() == searchLower)
		{
			num += 1000.0;
		}
		if (!string.IsNullOrEmpty(item.label))
		{
			string text = item.label.ToLower();
			if (text.StartsWith(searchLower))
			{
				num += 800.0;
			}
			else if (text.Contains(searchLower))
			{
				num += 600.0;
				int num2 = text.IndexOf(searchLower);
				num += (double)(100 - num2 * 2);
			}
		}
		if (item.defName.ToLower().Contains(searchLower))
		{
			num += 400.0;
			if (item.defName.ToLower().StartsWith(searchLower))
			{
				num += 200.0;
			}
		}
		if (!string.IsNullOrEmpty(item.description))
		{
			string text2 = item.description.ToLower();
			if (text2.Contains(searchLower))
			{
				num += 100.0;
				int num3 = text2.IndexOf(searchLower);
				num += (double)(50 - num3);
			}
		}
		return num + Math.Log10(Math.Max(item.BaseMarketValue, 1f)) * 10.0;
	}

	private ValueTierConfig GetValueTierConfig(ThingDef product)
	{
		if (product == null)
		{
			return ValueTiers[0];
		}
		float baseMarketValue = product.BaseMarketValue;
		for (int i = 0; i < ValueTiers.Count; i++)
		{
			ValueTierConfig valueTierConfig = ValueTiers[i];
			if (baseMarketValue >= valueTierConfig.minValue && baseMarketValue < valueTierConfig.maxValue)
			{
				return valueTierConfig;
			}
		}
		return ValueTiers[ValueTiers.Count - 1];
	}

	public float GetWorkAmountForProduct(ThingDef product)
	{
		if (product == null)
		{
			return 1000f;
		}
		float baseMarketValue = product.BaseMarketValue;
		float num = 0f;
		if (baseMarketValue <= 5f)
		{
			num = 4f;
		}
		else if (baseMarketValue >= 500000f)
		{
			num = 336f;
		}
		else
		{
			for (int i = 0; i < ValueTiers.Count - 1; i++)
			{
				ValueTierConfig valueTierConfig = ValueTiers[i];
				ValueTierConfig valueTierConfig2 = ValueTiers[i + 1];
				if (baseMarketValue >= valueTierConfig.minValue && baseMarketValue < valueTierConfig2.maxValue)
				{
					float workHours = valueTierConfig.workHours;
					float workHours2 = valueTierConfig2.workHours;
					if (baseMarketValue < valueTierConfig.maxValue)
					{
						float num2 = (baseMarketValue - valueTierConfig.minValue) / (valueTierConfig.maxValue - valueTierConfig.minValue);
						num = workHours + num2 * (workHours2 - workHours);
					}
					else
					{
						float num3 = (baseMarketValue - valueTierConfig2.minValue) / (valueTierConfig2.maxValue - valueTierConfig2.minValue);
						num = workHours2 + num3 * (workHours2 - workHours);
					}
					break;
				}
			}
		}
		float num4 = num * 2500f;
		return num4 * globalTimeMultiplier;
	}

	public int GetOutputAmountForProduct(ThingDef product)
	{
		if (product == null)
		{
			return 1;
		}
		ValueTierConfig valueTierConfig = GetValueTierConfig(product);
		int quantityMultiplier = valueTierConfig.quantityMultiplier;
		quantityMultiplier = Mathf.RoundToInt((float)quantityMultiplier * globalOutputMultiplier);
		quantityMultiplier = Math.Max((int)outputMin, quantityMultiplier);
		if (product.stackLimit <= 1)
		{
			return 1;
		}
		return Mathf.FloorToInt(quantityMultiplier);
	}

	public void ResetToDefaults()
	{
		globalOutputMultiplier = 1f;
		globalTimeMultiplier = 1f;
		outputMin = 1f;
		enableUnfinishedWork = true;
		unfinishedWorkMass = 1f;
		unfinishedWorkHitPoints = 50;
		allowedProducts.Clear();
		allowedProducts.AddRange(DefaultAllowedProducts);
		NotifySettingsChanged();
	}

	public void ValidateOutputLimits()
	{
		if (outputMin < 1f)
		{
			outputMin = 1f;
		}
	}

	private void TestValueTierSystem()
	{
		try
		{
			float[] array = new float[12]
			{
				1f, 10f, 25f, 75f, 200f, 750f, 2000f, 7500f, 25000f, 75000f,
				200000f, 750000f
			};
			float[] array2 = array;
			foreach (float baseMarketValue in array2)
			{
				ThingDef product = new ThingDef
				{
					BaseMarketValue = baseMarketValue
				};
				float workAmountForProduct = GetWorkAmountForProduct(product);
				float num = workAmountForProduct / 2500f;
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[VoidExtractor] 测试分段函数系统时发生错误: {arg}");
		}
	}

	public void NotifySettingsChanged()
	{
		ValidateOutputLimits();
		this.OnSettingsChanged?.Invoke();
		try
		{
			DynamicRecipeManager.UpdateAllRecipes();
		}
		catch (Exception arg)
		{
			Log.Error($"[VoidExtractor] 更新配方时发生错误: {arg}");
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref globalOutputMultiplier, "globalOutputMultiplier", 1f);
		Scribe_Values.Look(ref globalTimeMultiplier, "globalTimeMultiplier", 1f);
		Scribe_Values.Look(ref outputMin, "outputMin", 1f);
		Scribe_Values.Look(ref enableUnfinishedWork, "enableUnfinishedWork", defaultValue: true);
		Scribe_Values.Look(ref unfinishedWorkMass, "unfinishedWorkMass", 1f);
		Scribe_Values.Look(ref unfinishedWorkHitPoints, "unfinishedWorkHitPoints", 50);
		Scribe_Values.Look(ref enableAutoProduction, "enableAutoProduction", defaultValue: false);
		Scribe_Values.Look(ref autoProductionCooldown, "autoProductionCooldown", 60000);
		Scribe_Collections.Look(ref allowedProducts, "allowedProducts", LookMode.Value);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (allowedProducts == null)
			{
				allowedProducts = new List<string>();
			}
			InitializeDefaultSettings();
		}
	}
}
