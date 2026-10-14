using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidMinerProject;

public class VoidMinerMod : Mod
{
	private class ItemCacheData
	{
		public int outputAmount;

		public float workAmount;

		public Color itemColor;

		public bool isFromOtherMod;

		public string modName;
	}

	private Vector2 scrollPosition = Vector2.zero;

	private string searchText = "";

	private List<ThingDef> cachedProducibleItems = new List<ThingDef>();

	private Dictionary<string, ItemCacheData> itemCache = new Dictionary<string, ItemCacheData>();

	private const int VIEWPORT_SIZE = 10;

	private bool cacheBuilt = false;

	private int currentViewportIndex = 0;

	private int preloadViewportIndex = 0;

	private bool isPreloading = false;

	private const int PRELOAD_TRIGGER_THRESHOLD = 7;

	private int lastScrollPosition = 0;

	private string selectedModFilter = "AllMods";

	private List<string> availableMods = new List<string>();

	private List<ThingDef> displayItems = new List<ThingDef>();

	private string lastSearchText = "";

	private string lastModFilter = "";

	private bool filterCacheValid = false;

	private Dictionary<string, List<ThingDef>> searchResultCache = new Dictionary<string, List<ThingDef>>();

	private bool settingsWindowOpen = false;

	public VoidMinerMod(ModContentPack content)
		: base(content)
	{
		VoidMinerSettings settings = GetSettings<VoidMinerSettings>();
		LongEventHandler.ExecuteWhenFinished(settings.InitializeAfterDefsLoaded);
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		VoidMinerSettings settings = GetSettings<VoidMinerSettings>();
		if (!settingsWindowOpen)
		{
			settingsWindowOpen = true;
			BuildItemCache();
			filterCacheValid = false;
		}
		Rect rect = new Rect(0f, 0f, inRect.width, 40f);
		Text.Font = GameFont.Medium;
		Widgets.Label(rect, "");
		Text.Font = GameFont.Small;
		Rect rect2 = new Rect(0f, 50f, inRect.width, inRect.height - 50f);
		DrawSliderSettings(rect2, settings);
		Rect rect3 = new Rect(0f, 200f, rect2.width, (rect2.height - 290f) * 1.7f);
		DrawItemSelection(rect3, settings);
	}

	public override void WriteSettings()
	{
		base.WriteSettings();
		if (settingsWindowOpen)
		{
			settingsWindowOpen = false;
			ClearItemCache();
			displayItems.Clear();
			filterCacheValid = false;
		}
	}

	private void DrawSliderSettings(Rect rect, VoidMinerSettings settings)
	{
		float y = rect.y;
		float num = 168f;
		float num2 = 392f;
		float num3 = 80f;
		float num4 = 40f;
		float num5 = num + 20f + num2 + 25f + num3;
		float num6 = (rect.width - num5) / 2f;
		Rect rect2 = new Rect(rect.x + num6, y, num, 30f);
		Text.Font = GameFont.Small;
		Widgets.Label(rect2, "SC_GlobalOutputMultiplierLabel".Translate());
		Rect rect3 = new Rect(rect.x + num6 + num + 20f, y, num2, 30f);
		float globalOutputMultiplier = settings.globalOutputMultiplier;
		float globalOutputMultiplier2 = settings.globalOutputMultiplier;
		globalOutputMultiplier2 = Widgets.HorizontalSlider(rect3, globalOutputMultiplier2, 0.1f, 10f, middleAlignment: true);
		Rect rect4 = new Rect(rect.x + num6 + num + num2 + 25f, y, num3, 30f);
		string text = globalOutputMultiplier2.ToString("F1");
		text = Widgets.TextField(rect4, text);
		if (float.TryParse(text, out var result))
		{
			result = Mathf.Clamp(result, 0.1f, 10f);
			globalOutputMultiplier2 = result;
		}
		if (Math.Abs(globalOutputMultiplier2 - settings.globalOutputMultiplier) > 0.01f)
		{
			settings.globalOutputMultiplier = globalOutputMultiplier2;
		}
		if (Math.Abs(globalOutputMultiplier - settings.globalOutputMultiplier) > 0.01f)
		{
			settings.NotifySettingsChanged();
		}
		y += num4;
		rect2 = new Rect(rect.x + num6, y, num, 30f);
		Widgets.Label(rect2, "SC_GlobalTimeMultiplierLabel".Translate());
		rect3 = new Rect(rect.x + num6 + num + 20f, y, num2, 30f);
		float globalTimeMultiplier = settings.globalTimeMultiplier;
		float globalTimeMultiplier2 = settings.globalTimeMultiplier;
		globalTimeMultiplier2 = Widgets.HorizontalSlider(rect3, globalTimeMultiplier2, 0.01f, 10f, middleAlignment: true);
		rect4 = new Rect(rect.x + num6 + num + num2 + 25f, y, num3, 30f);
		string text2 = globalTimeMultiplier2.ToString("F1");
		text2 = Widgets.TextField(rect4, text2);
		if (float.TryParse(text2, out var result2))
		{
			result2 = Mathf.Clamp(result2, 0.01f, 10f);
			globalTimeMultiplier2 = result2;
		}
		if (Math.Abs(globalTimeMultiplier2 - settings.globalTimeMultiplier) > 0.01f)
		{
			settings.globalTimeMultiplier = globalTimeMultiplier2;
		}
		if (Math.Abs(globalTimeMultiplier - settings.globalTimeMultiplier) > 0.01f)
		{
			settings.NotifySettingsChanged();
		}
		y += num4;
		rect2 = new Rect(rect.x + num6, y, num, 30f);
		Widgets.Label(rect2, "SC_OutputMinLabel".Translate());
		rect3 = new Rect(rect.x + num6 + num + 20f, y, num2, 30f);
		float outputMin = settings.outputMin;
		float outputMin2 = settings.outputMin;
		outputMin2 = Widgets.HorizontalSlider(rect3, outputMin2, 1f, 100f, middleAlignment: true);
		rect4 = new Rect(rect.x + num6 + num + num2 + 25f, y, num3, 30f);
		string text3 = outputMin2.ToString("F0");
		text3 = Widgets.TextField(rect4, text3);
		if (float.TryParse(text3, out var result3))
		{
			result3 = Mathf.Clamp(result3, 1f, 100f);
			outputMin2 = result3;
		}
		if (Math.Abs(outputMin2 - settings.outputMin) > 0.1f)
		{
			settings.outputMin = outputMin2;
		}
		if (Math.Abs(outputMin - settings.outputMin) > 0.1f)
		{
			settings.NotifySettingsChanged();
		}
		y += num4;
		Rect rect5 = new Rect((rect.width - 200f) / 2f, y, 200f, 28f);
		if (Widgets.ButtonText(rect5, "SC_ResetToDefaultsButton".Translate()))
		{
			settings.ResetToDefaults();
			ClearItemCache();
		}
		Rect rect6 = new Rect(rect5.x + rect5.width + 20f, y, 200f, 28f);
		if (availableMods.Count == 0)
		{
			availableMods = GetAvailableMods();
		}
		if (selectedModFilter == "AllMods")
		{
			selectedModFilter = "SC_AllModsLabel".Translate();
		}
		if (!Widgets.ButtonText(rect6, selectedModFilter))
		{
			return;
		}
		if (availableMods.Count > 10)
		{
			Find.WindowStack.Add(new ModSelectionWindow(availableMods, delegate(string selectedMod)
			{
				selectedModFilter = selectedMod;
			}));
			return;
		}
		List<FloatMenuOption> options = availableMods.Select((string mod) => new FloatMenuOption(mod, delegate
		{
			selectedModFilter = mod;
		})).ToList();
		Find.WindowStack.Add(new FloatMenu(options));
	}

	private void DrawItemSelection(Rect rect, VoidMinerSettings settings)
	{
		Rect rect2 = new Rect(rect.x, rect.y, rect.width, 35f);
		Text.Font = GameFont.Medium;
		Widgets.Label(rect2, "SC_ProducibleItemsTitle".Translate());
		Text.Font = GameFont.Small;
		Rect rect3 = new Rect(rect.x, rect.y + 40f, rect.width, 35f);
		searchText = Widgets.TextField(rect3, searchText);
		if (searchText != lastSearchText || selectedModFilter != lastModFilter || !filterCacheValid)
		{
			UpdateDisplayList();
			lastSearchText = searchText;
			lastModFilter = selectedModFilter;
			filterCacheValid = true;
		}
		Rect rect4 = new Rect(rect.x, rect.y + 80f, rect.width, rect.height - 80f);
		DrawVirtualizedItemList(rect4, settings, displayItems);
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

	private void UpdateDisplayList()
	{
		displayItems.Clear();
		IEnumerable<ThingDef> enumerable = cachedProducibleItems.AsEnumerable();
		if (!string.IsNullOrEmpty(searchText))
		{
			string searchLower = searchText.ToLower();
			enumerable = from item in enumerable
				where IsItemMatch(item, searchLower)
				orderby CalculateRelevanceScore(item, searchLower) descending
				select item;
		}
		string text = "SC_AllModsLabel".Translate();
		if (selectedModFilter != text)
		{
			enumerable = enumerable.Where((ThingDef item) => item.modContentPack != null && item.modContentPack.Name == selectedModFilter);
		}
		foreach (ThingDef item in enumerable)
		{
			displayItems.Add(item);
		}
	}

	private (int start, int end) CalculateSimpleViewport(int totalItems)
	{
		if (totalItems == 0)
		{
			return (start: 0, end: 0);
		}
		int num = 50;
		int num2 = 15;
		int num3 = Mathf.FloorToInt(scrollPosition.y / (float)num);
		int val = Math.Min(num3 + num2 + 2, totalItems);
		num3 = Math.Max(0, num3);
		val = Math.Min(totalItems, val);
		return (start: num3, end: val);
	}

	private List<ThingDef> GetProducibleItems()
	{
		return VoidMinerSettings.Instance.GetProducibleItems();
	}

	private Color GetItemColor(ThingDef item)
	{
		if (item.graphicData != null)
		{
			return item.graphicData.color;
		}
		if (item.IsStuff && item.stuffProps != null)
		{
			return item.stuffProps.color;
		}
		return Color.white;
	}

	private List<string> GetAvailableMods()
	{
		HashSet<string> hashSet = new HashSet<string>();
		string item = "SC_AllModsLabel".Translate();
		hashSet.Add(item);
		foreach (ThingDef cachedProducibleItem in cachedProducibleItems)
		{
			if (cachedProducibleItem != null && cachedProducibleItem.modContentPack != null)
			{
				string name = cachedProducibleItem.modContentPack.Name;
				if (!string.IsNullOrEmpty(name))
				{
					hashSet.Add(name);
				}
			}
		}
		List<string> list = hashSet.ToList();
		list.Sort();
		if (list.Contains(item))
		{
			list.Remove(item);
			list.Insert(0, item);
		}
		return list;
	}

	public override string SettingsCategory()
	{
		return "SC_VoidMinerSettingsCategory".Translate();
	}

	private void BuildItemCache()
	{
		if (cacheBuilt)
		{
			return;
		}
		try
		{
			cachedProducibleItems.Clear();
			itemCache.Clear();
			List<ThingDef> producibleItems = GetProducibleItems();
			cachedProducibleItems.AddRange(producibleItems);
			VoidMinerSettings settings = GetSettings<VoidMinerSettings>();
			foreach (ThingDef item in producibleItems)
			{
				ItemCacheData value = new ItemCacheData
				{
					outputAmount = settings.GetOutputAmountForProduct(item),
					workAmount = settings.GetWorkAmountForProduct(item),
					itemColor = GetItemColor(item),
					isFromOtherMod = (item.modContentPack != null && item.modContentPack != LoadedModManager.GetMod<VoidMinerMod>().Content),
					modName = (item.modContentPack?.Name ?? "")
				};
				itemCache[item.defName] = value;
			}
			availableMods = GetAvailableMods();
			cacheBuilt = true;
		}
		catch (Exception arg)
		{
			Log.Error($"[SingularityCore] Error building the product cache: {arg}");
			ClearItemCache();
		}
	}

	private void ClearItemCache()
	{
		cachedProducibleItems.Clear();
		itemCache.Clear();
		cacheBuilt = false;
	}

	private (int startIndex, int endIndex) CalculateViewportRange()
	{
		var (item, item2, _, _) = CalculateViewportRanges();
		return (startIndex: item, endIndex: item2);
	}

	private (int currentStart, int currentEnd, int preloadStart, int preloadEnd) CalculateViewportRanges()
	{
		if (cachedProducibleItems.Count == 0)
		{
			return (currentStart: 0, currentEnd: 0, preloadStart: 0, preloadEnd: 0);
		}
		int value = Mathf.FloorToInt(scrollPosition.y / 50f);
		currentViewportIndex = Mathf.Clamp(value, 0, Math.Max(0, cachedProducibleItems.Count - 10));
		int num = currentViewportIndex;
		int num2 = Math.Min(num + 10, cachedProducibleItems.Count);
		bool flag = scrollPosition.y < (float)lastScrollPosition;
		int num3;
		int num4;
		if (flag)
		{
			num3 = Math.Max(0, num - 10);
			num4 = num;
		}
		else
		{
			num3 = num2;
			num4 = Math.Min(num3 + 10, cachedProducibleItems.Count);
		}
		if (num == 0 && flag)
		{
			num3 = num2;
			num4 = Math.Min(num3 + 10, cachedProducibleItems.Count);
		}
		else if (num2 == cachedProducibleItems.Count && !flag)
		{
			num3 = Math.Max(0, num - 10);
			num4 = num;
		}
		if (num4 == cachedProducibleItems.Count && cachedProducibleItems.Count % 10 != 0)
		{
			int num5 = cachedProducibleItems.Count % 10;
			if (num5 < 10)
			{
				num = Math.Max(0, cachedProducibleItems.Count - 10);
				num2 = cachedProducibleItems.Count;
				num3 = Math.Max(0, num - 10);
				num4 = num2;
			}
		}
		return (currentStart: num, currentEnd: num2, preloadStart: num3, preloadEnd: num4);
	}

	private bool ShouldTriggerPreload()
	{
		if (cachedProducibleItems.Count == 0 || isPreloading)
		{
			return false;
		}
		int num = Mathf.Abs((int)scrollPosition.y - lastScrollPosition);
		if (num < 50)
		{
			return false;
		}
		bool flag = scrollPosition.y < (float)lastScrollPosition;
		var (num2, num3, num4, num5) = CalculateViewportRanges();
		if (flag)
		{
			if (num4 <= 0)
			{
				return false;
			}
			int num6 = num3 - num2;
			if (num6 >= 7)
			{
				float num7 = (scrollPosition.y - (float)num2 * 50f) / ((float)num6 * 50f);
				return num7 <= 0.3f;
			}
		}
		else
		{
			if (num5 >= cachedProducibleItems.Count)
			{
				return false;
			}
			int num8 = num3 - num2;
			if (num8 >= 7)
			{
				float num9 = (scrollPosition.y - (float)num2 * 50f) / ((float)num8 * 50f);
				return num9 >= 0.7f;
			}
		}
		return false;
	}

	private void ExecutePreload()
	{
		if (isPreloading)
		{
			return;
		}
		try
		{
			isPreloading = true;
			bool flag = scrollPosition.y < (float)lastScrollPosition;
			var (num, num2, num3, num4) = CalculateViewportRanges();
			int num5;
			int num6;
			if (flag)
			{
				num5 = Math.Max(0, num3 - 10);
				num6 = num3;
			}
			else
			{
				num5 = num4;
				num6 = Math.Min(num5 + 10, cachedProducibleItems.Count);
			}
			for (int i = num5; i < num6; i++)
			{
				if (i < cachedProducibleItems.Count)
				{
					ThingDef thingDef = cachedProducibleItems[i];
					if (thingDef != null && !itemCache.ContainsKey(thingDef.defName))
					{
						VoidMinerSettings settings = GetSettings<VoidMinerSettings>();
						ItemCacheData value = new ItemCacheData
						{
							outputAmount = settings.GetOutputAmountForProduct(thingDef),
							workAmount = settings.GetWorkAmountForProduct(thingDef),
							itemColor = GetItemColor(thingDef),
							isFromOtherMod = (thingDef.modContentPack != null && thingDef.modContentPack != LoadedModManager.GetMod<VoidMinerMod>().Content),
							modName = (thingDef.modContentPack?.Name ?? "")
						};
						itemCache[thingDef.defName] = value;
					}
				}
			}
			preloadViewportIndex = num5;
		}
		catch (Exception arg)
		{
			Log.Error($"[SingularityCore] Error preloading the product cache: {arg}");
		}
		finally
		{
			isPreloading = false;
		}
	}

	private void ManageViewportData()
	{
		if (cachedProducibleItems.Count == 0)
		{
			return;
		}
		if (ShouldTriggerPreload())
		{
			ExecutePreload();
		}
		(int currentStart, int currentEnd, int preloadStart, int preloadEnd) tuple = CalculateViewportRanges();
		int item = tuple.currentStart;
		int item2 = tuple.currentEnd;
		int item3 = tuple.preloadStart;
		int item4 = tuple.preloadEnd;
		int num = Math.Max(0, Math.Min(item, item3) - 10);
		int num2 = Math.Min(cachedProducibleItems.Count, Math.Max(item2, item4) + 10);
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, ItemCacheData> kvp in itemCache)
		{
			ThingDef thingDef = cachedProducibleItems.FirstOrDefault((ThingDef i) => i.defName == kvp.Key);
			if (thingDef != null)
			{
				int num3 = cachedProducibleItems.IndexOf(thingDef);
				if (num3 < num || num3 >= num2)
				{
					list.Add(kvp.Key);
				}
			}
		}
		foreach (string item5 in list)
		{
			itemCache.Remove(item5);
		}
		lastScrollPosition = (int)scrollPosition.y;
	}

	private void DrawVirtualizedItemList(Rect rect, VoidMinerSettings settings)
	{
		DrawVirtualizedItemList(rect, settings, displayItems);
	}

	private void DrawVirtualizedItemList(Rect rect, VoidMinerSettings settings, List<ThingDef> itemsToDraw)
	{
		if (itemsToDraw.Count == 0)
		{
			Widgets.Label(rect, "SC_NoProductionTarget".Translate());
			return;
		}
		(int start, int end) tuple = CalculateSimpleViewport(itemsToDraw.Count);
		int item = tuple.start;
		int item2 = tuple.end;
		float height = (float)itemsToDraw.Count * 50f;
		Rect viewRect = new Rect(0f, 0f, rect.width - 16f, height);
		Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
		float num = (float)item * 50f;
		for (int i = item; i < item2; i++)
		{
			ThingDef thingDef = itemsToDraw[i];
			if (thingDef == null)
			{
				continue;
			}
			Rect rect2 = new Rect(0f, num, viewRect.width, 50f);
			if (!itemCache.TryGetValue(thingDef.defName, out var value))
			{
				value = new ItemCacheData
				{
					outputAmount = settings.GetOutputAmountForProduct(thingDef),
					workAmount = settings.GetWorkAmountForProduct(thingDef),
					itemColor = GetItemColor(thingDef),
					isFromOtherMod = (thingDef.modContentPack != null && thingDef.modContentPack != LoadedModManager.GetMod<VoidMinerMod>().Content),
					modName = (thingDef.modContentPack?.Name ?? "")
				};
				itemCache[thingDef.defName] = value;
			}
			Vector2 topLeft = new Vector2(10f, num + 15f);
			bool checkOn = settings.IsProductAllowed(thingDef);
			Widgets.Checkbox(topLeft, ref checkOn);
			if (checkOn != settings.IsProductAllowed(thingDef))
			{
				if (checkOn)
				{
					settings.AddAllowedProduct(thingDef.defName);
				}
				else
				{
					settings.RemoveAllowedProduct(thingDef.defName);
				}
				settings.NotifySettingsChanged();
			}
			Rect outerRect = new Rect(45f, num + 7f, 36f, 36f);
			Texture2D texture2D;
			if (ModsConfig.BiotechActive && thingDef.defName.StartsWith("genepack_"))
			{
				texture2D = ThingDefOf.Genepack.uiIcon;
				if (texture2D == null)
				{
					texture2D = ContentFinder<Texture2D>.Get("Things/Item/Special/Genepack", reportFailure: false);
					if (texture2D == null)
					{
						texture2D = thingDef.uiIcon;
					}
				}
			}
			else
			{
				texture2D = thingDef.uiIcon;
			}
			if (texture2D != null)
			{
				GUI.color = value.itemColor;
				Widgets.DrawTextureFitted(outerRect, texture2D, 1f);
				GUI.color = Color.white;
			}
			Rect rect3 = new Rect(95f, num + 15f, viewRect.width - 450f, 20f);
			Widgets.Label(rect3, thingDef.label);
			Rect rect4 = new Rect(viewRect.width - 380f, num + 15f, 90f, 20f);
			Widgets.Label(rect4, "SC_ValueLabel".Translate() + " " + thingDef.BaseMarketValue.ToString("F0"));
			Rect rect5 = new Rect(viewRect.width - 270f, num + 15f, 100f, 20f);
			Widgets.Label(rect5, string.Concat("SC_OutputLabel".Translate() + " ", value.outputAmount.ToString()));
			Rect rect6 = new Rect(viewRect.width - 150f, num + 15f, 120f, 20f);
			Widgets.Label(rect6, "SC_WorkAmountLabel".Translate() + " " + (value.workAmount / 60f).ToString("F0"));
			if (value.isFromOtherMod)
			{
				Rect rect7 = new Rect(95f, num + 35f, viewRect.width - 95f, 15f);
				Text.Font = GameFont.Tiny;
				GUI.color = Color.yellow;
				Widgets.Label(rect7, "SC_FromModLabel".Translate(value.modName));
				GUI.color = Color.white;
				Text.Font = GameFont.Small;
			}
			num += 50f;
		}
		Widgets.EndScrollView();
	}
}
