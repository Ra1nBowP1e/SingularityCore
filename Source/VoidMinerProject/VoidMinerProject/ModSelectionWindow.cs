using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VoidMinerProject;

public class ModSelectionWindow : Window
{
	private List<string> availableMods;

	private Action<string> onModSelected;

	private string searchText = "";

	private Vector2 scrollPosition = Vector2.zero;

	public override Vector2 InitialSize => new Vector2(400f, 500f);

	public ModSelectionWindow(List<string> mods, Action<string> callback)
	{
		availableMods = mods;
		onModSelected = callback;
		forcePause = false;
		draggable = true;
		resizeable = true;
		closeOnClickedOutside = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Rect rect = new Rect(0f, 0f, inRect.width, 40f);
		Widgets.DrawHighlight(rect);
		Rect rect2 = new Rect(15f, 8f, inRect.width - 30f, 24f);
		Widgets.Label(rect2, "ModSelectionTitle".Translate());
		Rect rect3 = new Rect(inRect.width - 35f, 5f, 30f, 30f);
		if (Widgets.ButtonText(rect3, "X"))
		{
			Close();
		}
		Rect rect4 = new Rect(10f, 50f, inRect.width - 20f, 35f);
		Widgets.DrawHighlight(rect4);
		Rect rect5 = new Rect(15f, 58f, 60f, 20f);
		Widgets.Label(rect5, "Search".Translate());
		Rect rect6 = new Rect(80f, 55f, inRect.width - 100f, 25f);
		searchText = Widgets.TextField(rect6, searchText);
		List<string> list = availableMods.Where((string mod) => string.IsNullOrEmpty(searchText) || mod.ToLower().Contains(searchText.ToLower())).ToList();
		Rect rect7 = new Rect(10f, 95f, inRect.width - 20f, 25f);
		Widgets.DrawHighlight(rect7);
		Rect rect8 = new Rect(15f, 98f, inRect.width - 30f, 20f);
		Widgets.Label(rect8, "AvailableModsLabel".Translate(list.Count, availableMods.Count));
		Rect outRect = new Rect(10f, 130f, inRect.width - 20f, inRect.height - 180f);
		Widgets.BeginScrollView(outRect, ref scrollPosition, new Rect(0f, 0f, outRect.width - 16f, (float)list.Count * 35f));
		float num = 0f;
		foreach (string item in list)
		{
			Rect rect9 = new Rect(0f, num, outRect.width - 16f, 30f);
			Widgets.DrawHighlightIfMouseover(rect9);
			Rect rect10 = new Rect(8f, num + 5f, outRect.width - 32f, 20f);
			Widgets.Label(rect10, item);
			if (Widgets.ButtonInvisible(rect9))
			{
				onModSelected(item);
				Close();
			}
			num += 35f;
		}
		Widgets.EndScrollView();
		Rect rect11 = new Rect((inRect.width - 90f) / 2f, inRect.height - 35f, 90f, 30f);
		if (Widgets.ButtonText(rect11, "CloseButton".Translate(), drawBackground: true, doMouseoverSound: false))
		{
			Close();
		}
	}
}
