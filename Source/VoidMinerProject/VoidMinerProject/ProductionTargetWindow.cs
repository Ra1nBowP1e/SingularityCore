using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidMinerProject;

public class ProductionTargetWindow : Window
{
	private Building_AutoVoidMiner autoMiner;

	private Vector2 scrollPosition = Vector2.zero;

	private string searchText = "";

	public override Vector2 InitialSize => new Vector2(800f, 700f);

	public ProductionTargetWindow(Building_AutoVoidMiner miner)
	{
		autoMiner = miner;
		if (VoidMinerSettings.Instance != null)
		{
			VoidMinerSettings.Instance.OnSettingsChanged += OnSettingsChanged;
		}
		forcePause = false;
		draggable = true;
		resizeable = true;
		closeOnClickedOutside = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Widgets.DrawWindowBackground(inRect);
		Rect rect = new Rect(0f, 0f, inRect.width, 40f);
		Widgets.DrawHighlight(rect);
		Rect rect2 = new Rect(15f, 8f, inRect.width - 30f, 24f);
		Widgets.Label(rect2, "ProductionTargetWindowTitle".Translate());
		Rect rect3 = new Rect(inRect.width - 35f, 5f, 30f, 30f);
		if (Widgets.ButtonText(rect3, "X"))
		{
			Close();
		}
		Rect rect4 = new Rect(10f, 50f, inRect.width - 20f, 140f);
		DrawStatusInfo(rect4);
		Rect rect5 = new Rect(10f, 200f, inRect.width - 20f, 40f);
		DrawControlButtons(rect5);
		Rect rect6 = new Rect(10f, 250f, inRect.width - 20f, 35f);
		DrawSearchArea(rect6);
		List<ThingDef> filteredProducts = GetFilteredProducts();
		Rect rect7 = new Rect(10f, 295f, inRect.width - 20f, 25f);
		Widgets.DrawHighlight(rect7);
		VoidMinerSettings settings = VoidMinerSettings.Instance;
		int num = ((settings != null) ? filteredProducts.Count((ThingDef p) => settings.IsProductAllowed(p)) : 0);
		Rect rect8 = new Rect(15f, 298f, inRect.width - 30f, 20f);
		string label;
		if (string.IsNullOrEmpty(searchText))
		{
			label = "AllProducibleItemsCount".Translate(filteredProducts.Count, num);
		}
		else
		{
			string searchLower = searchText.ToLower();
			int num2 = filteredProducts.Count((ThingDef p) => !string.IsNullOrEmpty(p.label) && p.label.ToLower() == searchLower);
			int num3 = filteredProducts.Count((ThingDef p) => !string.IsNullOrEmpty(p.label) && p.label.ToLower().StartsWith(searchLower));
			label = "SearchResultsCount".Translate(filteredProducts.Count, num2, num3, num);
		}
		Widgets.Label(rect8, label);
		Rect outRect = new Rect(10f, 330f, inRect.width - 20f, inRect.height - 380f);
		Widgets.BeginScrollView(outRect, ref scrollPosition, new Rect(0f, 0f, outRect.width - 16f, (float)filteredProducts.Count * 105f));
		float num4 = 0f;
		foreach (ThingDef item in filteredProducts)
		{
			Rect rect9 = new Rect(0f, num4, outRect.width - 16f, 100f);
			Widgets.DrawHighlightIfMouseover(rect9);
			Rect rect10 = new Rect(8f, num4 + 8f, 70f, 70f);
			Widgets.DrawBox(rect10, 2);
			Texture2D texture2D;
			if (ModsConfig.BiotechActive && item.defName.StartsWith("genepack_"))
			{
				texture2D = ThingDefOf.Genepack.uiIcon;
				if (texture2D == null)
				{
					texture2D = ContentFinder<Texture2D>.Get("Things/Item/Special/Genepack", reportFailure: false);
					if (texture2D == null)
					{
						texture2D = item.uiIcon;
					}
				}
			}
			else
			{
				texture2D = item.uiIcon;
			}
			Widgets.DrawTextureFitted(rect10, texture2D, 0.9f);
			bool flag = VoidMinerSettings.Instance?.IsProductAllowed(item) ?? false;
			bool flag2 = !string.IsNullOrEmpty(searchText);
			if (flag)
			{
				Widgets.DrawLightHighlight(rect9);
			}
			else
			{
				if (!flag2)
				{
					continue;
				}
				Widgets.DrawHighlight(rect9);
			}
			Rect rect11 = new Rect(85f, num4 + 8f, outRect.width - 280f, 70f);
			Rect rect12 = new Rect(85f, num4 + 8f, flag2 ? (outRect.width - 320f) : (outRect.width - 100f), 25f);
			if (flag2)
			{
				DrawHighlightedLabel(rect12, item.label, searchText);
			}
			else
			{
				Widgets.Label(rect12, item.label);
			}
			if (flag2)
			{
				Rect rect13 = new Rect(rect12.x + rect12.width + 5f, num4 + 8f, 100f, 20f);
				string label2 = (flag ? "ItemAllowed".Translate() : "ItemCanAdd".Translate());
				GUI.color = (flag ? Color.green : new Color(1f, 0.5f, 0f));
				Widgets.Label(rect13, label2);
				GUI.color = Color.white;
			}
			Rect rect14 = new Rect(85f, num4 + 38f, outRect.width - 280f, 22f);
			float f = CalculateWorkAmount(item);
			int ticks = Mathf.RoundToInt(f);
			string label3 = "ProductionTimeLabel".Translate(Building_AutoVoidMiner.FormatTimeFromTicks(ticks));
			Widgets.Label(rect14, label3);
			Rect rect15 = new Rect(85f, num4 + 65f, outRect.width - 280f, 22f);
			int num5 = CalculateOutputAmount(item);
			Widgets.Label(rect15, "ExpectedOutput".Translate(num5));
			Rect butRect = new Rect(outRect.width - 196f, num4 + 30f, 25f, 25f);
			if (Widgets.ButtonImage(butRect, ContentFinder<Texture2D>.Get("InfoButton", reportFailure: false)))
			{
				Find.WindowStack.Add(new Dialog_InfoCard(item));
			}
			float width = 144f;
			Rect rect16 = new Rect(outRect.width - 166f, num4 + 30f, width, 35f);
			string label4 = "StartProduction".Translate();
			if (!string.IsNullOrEmpty(searchText) && !flag)
			{
				label4 = "AddAndStartProduction".Translate();
			}
			if (Widgets.ButtonText(rect16, label4, drawBackground: true, doMouseoverSound: false))
			{
				if (!flag && !string.IsNullOrEmpty(searchText))
				{
					settings?.AddAllowedProduct(item.defName);
					settings.Write();
					Messages.Message("ProductAddedToAllowed".Translate(item.label), MessageTypeDefOf.PositiveEvent);
				}
				autoMiner.SetProductionTarget(item);
				Messages.Message("ProductionTargetSet".Translate(item.label), MessageTypeDefOf.PositiveEvent);
			}
			num4 += 105f;
		}
		Widgets.EndScrollView();
		Rect rect17 = new Rect((inRect.width - 90f) / 2f, inRect.height - 35f, 90f, 30f);
		if (Widgets.ButtonText(rect17, "CloseButton".Translate(), drawBackground: true, doMouseoverSound: false))
		{
			Close();
		}
	}

	private float CalculateWorkAmount(ThingDef productDef)
	{
		if (productDef == null)
		{
			return 60000f;
		}
		return VoidMinerSettings.Instance?.GetWorkAmountForProduct(productDef) ?? 60000f;
	}

	private int CalculateBaseOutputAmount(ThingDef productDef)
	{
		if (productDef == null)
		{
			return 1;
		}
		return VoidMinerSettings.Instance?.GetOutputAmountForProduct(productDef) ?? 1;
	}

	private int CalculateOutputAmount(ThingDef productDef)
	{
		if (productDef == null)
		{
			return 0;
		}
		int val = CalculateBaseOutputAmount(productDef);
		return Math.Max(val, 1);
	}

	private void DrawStatusInfo(Rect rect)
	{
		Widgets.DrawHighlight(rect);
		CompAutoVoidMiner.ProductionState currentState = autoMiner.GetCurrentState();
		ThingDef currentProductionTarget = autoMiner.GetCurrentProductionTarget();
		CompAutoVoidMiner comp = autoMiner.GetComp<CompAutoVoidMiner>();
		Rect rect2 = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 25f);
		Rect rect3 = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 20f);
		string text = currentState switch
		{
			CompAutoVoidMiner.ProductionState.Producing => "StatusProducing".Translate(), 
			CompAutoVoidMiner.ProductionState.Paused => "StatusPaused".Translate(), 
			CompAutoVoidMiner.ProductionState.Stopped => "StatusStopped".Translate(), 
			_ => "StatusStopped".Translate(), 
		};
		string text2 = "";
		if (currentState != CompAutoVoidMiner.ProductionState.Stopped)
		{
			CompAutoVoidMiner.ProductionMode currentProductionMode = comp.CurrentProductionMode;
			int productionCount = comp.ProductionCount;
			int completedCount = comp.CompletedCount;
			switch (currentProductionMode)
			{
			case CompAutoVoidMiner.ProductionMode.Single:
				text2 = string.Format(" - {0}: {1}/{2}", "SingleProductionStatus".Translate(), completedCount, productionCount);
				break;
			case CompAutoVoidMiner.ProductionMode.Maintain:
			{
				int currentStorageCount = comp.GetCurrentStorageCount();
				text2 = string.Format(" - {0}: {1}/{2}", "MaintainQuantityStatus".Translate(), currentStorageCount, productionCount);
				break;
			}
			case CompAutoVoidMiner.ProductionMode.Continuous:
				text2 = string.Format(" - {0}", "ContinuousProductionStatus".Translate());
				break;
			}
		}
		string label = "CurrentStatus".Translate() + ": " + text + text2;
		Widgets.Label(rect3, label);
		Rect rect4 = new Rect(rect.x + 5f, rect.y + 50f, rect.width - 10f, 20f);
		if (currentProductionTarget != null)
		{
			Widgets.Label(rect4, "CurrentTarget".Translate(currentProductionTarget.label));
		}
		else
		{
			Widgets.Label(rect4, "NeedToSetProductionMaterial".Translate());
		}
		switch (currentState)
		{
		case CompAutoVoidMiner.ProductionState.Producing:
		{
			Rect rect7 = new Rect(rect.x + 5f, rect.y + 70f, rect.width - 10f, 20f);
			float productionProgress2 = autoMiner.GetProductionProgress();
			Widgets.DrawBox(rect7, 2);
			Widgets.FillableBar(rect7, productionProgress2, ContentFinder<Texture2D>.Get("UI/Widgets/DesButBG"));
			Rect rect8 = new Rect(rect.x + 5f, rect.y + 95f, rect.width - 10f, 25f);
			int remainingProductionTicks = autoMiner.GetRemainingProductionTicks();
			string text3 = Building_AutoVoidMiner.FormatTimeFromTicks(remainingProductionTicks);
			Widgets.Label(rect8, "ProgressWithRemaining".Translate(productionProgress2, text3));
			break;
		}
		case CompAutoVoidMiner.ProductionState.Paused:
		{
			Rect rect5 = new Rect(rect.x + 5f, rect.y + 70f, rect.width - 10f, 20f);
			float productionProgress = autoMiner.GetProductionProgress();
			Widgets.DrawBox(rect5, 2);
			Widgets.FillableBar(rect5, productionProgress, ContentFinder<Texture2D>.Get("UI/Widgets/DesButBG"));
			Rect rect6 = new Rect(rect.x + 5f, rect.y + 95f, rect.width - 10f, 25f);
			Widgets.Label(rect6, "PausedWithProgress".Translate(productionProgress));
			break;
		}
		}
	}

	private void DrawSearchArea(Rect rect)
	{
		Widgets.DrawHighlight(rect);
		bool flag = Mouse.IsOver(rect);
		Rect rect2 = new Rect(rect.x + 8f, rect.y + 8f, 60f, 20f);
		Widgets.Label(rect2, "Search".Translate());
		Rect rect3 = new Rect(rect.x + 70f, rect.y + 5f, rect.width - 110f, 25f);
		searchText = Widgets.TextField(rect3, searchText);
		Rect rect4 = new Rect(rect.x + rect.width - 35f, rect.y + 5f, 25f, 25f);
		if (Widgets.ButtonText(rect4, "X", drawBackground: true, doMouseoverSound: false))
		{
			searchText = "";
		}
		if (flag)
		{
			Vector2 vector = Text.CalcSize("SearchAndAddItems".Translate());
			float num = vector.x + 16f;
			float num2 = vector.y + 8f;
			Rect position = new Rect(rect.x + (rect.width - num) / 2f, rect.y - num2 - 5f, num, num2);
			GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
			GUI.DrawTexture(position, ContentFinder<Texture2D>.Get("UI/Widgets/TooltipBG", reportFailure: false));
			GUI.color = Color.white;
			Rect rect5 = new Rect(position.x + 8f, position.y + 4f, position.width - 16f, position.height - 8f);
			Widgets.Label(rect5, "SearchAndAddItems".Translate());
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
	}

	private void DrawControlButtons(Rect rect)
	{
		Widgets.DrawHighlight(rect);
		CompAutoVoidMiner.ProductionState currentState = autoMiner.GetCurrentState();
		CompAutoVoidMiner comp = autoMiner.GetComp<CompAutoVoidMiner>();
		float y = rect.y + (rect.height - 25f) / 2f;
		Rect rect2 = new Rect(rect.x + 5f, y, 80f, 25f);
		Widgets.Label(rect2, "ProductionControl".Translate());
		Rect rect3 = new Rect(rect.x + 90f, y, 120f, 25f);
		CompAutoVoidMiner.ProductionMode currentProductionMode = comp.CurrentProductionMode;
		if (Widgets.ButtonText(rect3, GetProductionModeLabel(currentProductionMode), drawBackground: true, doMouseoverSound: false))
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>
			{
				new FloatMenuOption("SingleProduction".Translate(), delegate
				{
					comp.SetProductionMode(CompAutoVoidMiner.ProductionMode.Single);
				}),
				new FloatMenuOption("MaintainQuantity".Translate(), delegate
				{
					comp.SetProductionMode(CompAutoVoidMiner.ProductionMode.Maintain);
				}),
				new FloatMenuOption("ContinuousProduction".Translate(), delegate
				{
					comp.SetProductionMode(CompAutoVoidMiner.ProductionMode.Continuous);
				})
			};
			Find.WindowStack.Add(new FloatMenu(options));
		}
		if (currentProductionMode != CompAutoVoidMiner.ProductionMode.Continuous)
		{
			Rect rect4 = new Rect(rect.x + 215f, y, 60f, 25f);
			Widgets.Label(rect4, "Count".Translate());
			Rect rect5 = new Rect(rect.x + 275f, y, 25f, 25f);
			if (Widgets.ButtonText(rect5, "-", drawBackground: true, doMouseoverSound: false))
			{
				comp.DecreaseProductionCount();
			}
			Rect rect6 = new Rect(rect.x + 305f, y, 50f, 25f);
			int val = comp.ProductionCount;
			string buffer = val.ToString();
			Widgets.TextFieldNumeric(rect6, ref val, ref buffer, 1f, 100000f);
			if (val != comp.ProductionCount)
			{
				comp.SetProductionCount(val);
			}
			Rect rect7 = new Rect(rect.x + 360f, y, 25f, 25f);
			if (Widgets.ButtonText(rect7, "+", drawBackground: true, doMouseoverSound: false))
			{
				comp.IncreaseProductionCount();
			}
		}
		float num = ((currentProductionMode == CompAutoVoidMiner.ProductionMode.Continuous) ? 215f : 390f);
		float num2 = ((currentProductionMode == CompAutoVoidMiner.ProductionMode.Continuous) ? 310f : 485f);
		if (currentState == CompAutoVoidMiner.ProductionState.Producing)
		{
			Rect rect8 = new Rect(rect.x + num, y, 90f, 25f);
			if (Widgets.ButtonText(rect8, "PauseProduction".Translate(), drawBackground: true, doMouseoverSound: false))
			{
				autoMiner.PauseProduction();
			}
		}
		if (currentState == CompAutoVoidMiner.ProductionState.Paused)
		{
			Rect rect9 = new Rect(rect.x + num, y, 90f, 25f);
			if (Widgets.ButtonText(rect9, "ResumeProduction".Translate(), drawBackground: true, doMouseoverSound: false))
			{
				autoMiner.ResumeProduction();
			}
		}
		if (currentState == CompAutoVoidMiner.ProductionState.Producing || currentState == CompAutoVoidMiner.ProductionState.Paused)
		{
			Rect rect10 = new Rect(rect.x + num2, y, 90f, 25f);
			if (Widgets.ButtonText(rect10, "StopProduction".Translate(), drawBackground: true, doMouseoverSound: false))
			{
				autoMiner.StopProduction();
			}
		}
	}

	private string GetProductionModeLabel(CompAutoVoidMiner.ProductionMode mode)
	{
		return mode switch
		{
			CompAutoVoidMiner.ProductionMode.Single => "SingleProduction".Translate(), 
			CompAutoVoidMiner.ProductionMode.Maintain => "MaintainQuantity".Translate(), 
			CompAutoVoidMiner.ProductionMode.Continuous => "ContinuousProduction".Translate(), 
			_ => "Unknown".Translate(), 
		};
	}

	private void DrawProductionStatus(Rect rect, CompAutoVoidMiner comp)
	{
		CompAutoVoidMiner.ProductionMode currentProductionMode = comp.CurrentProductionMode;
		int productionCount = comp.ProductionCount;
		int completedCount = comp.CompletedCount;
		string label = "";
		switch (currentProductionMode)
		{
		case CompAutoVoidMiner.ProductionMode.Single:
			label = string.Format("{0}: {1}/{2}", "SingleProductionStatus".Translate(), completedCount, productionCount);
			break;
		case CompAutoVoidMiner.ProductionMode.Maintain:
			label = string.Format("{0}: {1}", "MaintainQuantityStatus".Translate(), productionCount);
			break;
		case CompAutoVoidMiner.ProductionMode.Continuous:
			label = "ContinuousProductionStatus".Translate();
			break;
		}
		Widgets.Label(rect, label);
	}

	private void OnSettingsChanged()
	{
		List<ThingDef> filteredProducts = GetFilteredProducts();
	}

	public override void PostClose()
	{
		if (VoidMinerSettings.Instance != null)
		{
			VoidMinerSettings.Instance.OnSettingsChanged -= OnSettingsChanged;
		}
		base.PostClose();
	}

	private List<ThingDef> GetFilteredProducts()
	{
		return VoidMinerSettings.Instance?.GetFilteredItemsForAutoMiner(searchText) ?? new List<ThingDef>();
	}

	private string GetHighlightedText(string text, string searchText)
	{
		if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(text))
		{
			return text;
		}
		string value = searchText.ToLower();
		string text2 = text.ToLower();
		int num = text2.IndexOf(value);
		if (num == -1)
		{
			return text;
		}
		string text3 = text.Substring(0, num);
		string text4 = text.Substring(num, searchText.Length);
		string text5 = text.Substring(num + searchText.Length);
		return text3 + "<color=yellow>" + text4 + "</color>" + text5;
	}

	private void DrawHighlightedLabel(Rect rect, string text, string searchText)
	{
		if (string.IsNullOrEmpty(searchText))
		{
			Widgets.Label(rect, text);
			return;
		}
		string highlightedText = GetHighlightedText(text, searchText);
		GUI.color = Color.yellow;
		Widgets.Label(rect, highlightedText);
		GUI.color = Color.white;
	}
}
