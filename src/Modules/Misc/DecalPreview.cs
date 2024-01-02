using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;

namespace RegionKit.Modules.Misc;

internal static class DecalPreview
{
	public static void Enable()
	{
		On.DevInterface.Panel.Update += Panel_Update;
		On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.PopulateDecals += SelectDecalPanel_PopulateDecals;
	}

	public static void Disable()
	{
		On.DevInterface.Panel.Update -= Panel_Update;
		On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.PopulateDecals -= SelectDecalPanel_PopulateDecals;
	}

	private static void Panel_Update(On.DevInterface.Panel.orig_Update orig, Panel self)
	{
		orig(self);

		if (self is not CustomDecalRepresentation.SelectDecalPanel) return;

		foreach (var subNode in self.subNodes)
		{
			if (subNode is Button && (subNode as Button).MouseOver)
			{
				if (subNode.IDstring == "BackPage99289..?/~") continue;
				if (subNode.IDstring == "NextPage99289..?/~") continue;

				(self.parentNode.parentNode as CustomDecalRepresentation).CD.LoadFile(subNode.IDstring);

				if (Futile.atlasManager.GetAtlasWithName(subNode.IDstring) != null)
				{
					self.fSprites[7].SetElementByName(subNode.IDstring);
					self.fSprites[7].isVisible = true;

					return;
				}
			}
		}

		self.fSprites[7].isVisible = false;
	}

	private static void SelectDecalPanel_PopulateDecals(On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.orig_PopulateDecals orig, CustomDecalRepresentation.SelectDecalPanel self, int offset)
	{
		orig(self, offset);

		//string[] array = AssetManager.ListDirectory("decals", false, false);

		//for (int i = 0; i < array.Length; i++)
		//{
		//	Debug.Log(array[i]);
		//}

		if (self.fSprites.Count != 8) self.fSprites.Add(new FSprite("pixel"));
		self.fSprites[7].anchorX = 0f;
		self.fSprites[7].anchorY = 0f;
		Futile.stage.AddChild(self.fSprites[7]);
	}
}
