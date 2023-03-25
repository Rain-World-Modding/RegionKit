using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AboveCloudsView;
using System.Text.RegularExpressions;
using Menu.Remix.MixedUI;
using UnityEngine;
using DevInterface;

namespace RegionKit.Modules.BackgroundBuilder
{
	internal static class CloudBuilder
	{
		public static void Apply()
		{
			//On.AboveCloudsView.Update += AboveCloudsView_Update;
			//On.AboveCloudsView.DistantBuilding.DrawSprites += DistantBuilding_DrawSprites;
		}

		private static void DistantBuilding_DrawSprites(On.AboveCloudsView.DistantBuilding.orig_DrawSprites orig, DistantBuilding self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			bool move = false;
			int num5 = 0;
			if ((self.room.game.devToolsActive || ModManager.DevTools) && Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
			{
				for (int i = 0; i < self.AboveCloudsScene.elements.Count; i++)
				{
					if (self.AboveCloudsScene.elements[i] is DistantBuilding building)
					{
						if (num5 == self.AboveCloudsScene.editObject && building == self)
						{
							move = true;

							break;
						}
						num5++;
					}
				}
			}

			if (move)
			{
				FNode? otherNode = null;
				bool otherNodeFound = false;
				foreach (RoomCamera.SpriteLeaser i in rCam.spriteLeasers)
				{
					if (i.drawableObject is BackgroundScene.BackgroundSceneElement element)
					{
						if (element.depth < self.depth)
						{
							otherNode = i.sprites[0];
							otherNodeFound = true;
							break;
						}
					}
				}

				if (otherNodeFound && otherNode != null)
				{ sLeaser.sprites[0].MoveInFrontOfOtherNode(otherNode); }
			}

			orig(self, sLeaser, rCam, timeStacker, camPos);
		}

		public static bool checkForBackgroundPage(DevUI devUI)
		{
			if (devUI == null) return false;
			if (devUI.activePage == null) return false;
			if (devUI.activePage is not BuilderPage.BackgroundPage) return false;
			return true;
		}
		private static void AboveCloudsView_Update(On.AboveCloudsView.orig_Update orig, AboveCloudsView self, bool eu)
		{
			orig(self, eu);

			if (!self.room.game.devToolsActive || !checkForBackgroundPage(self.room.game.devUI) || self.room.game.devUI.draggedNode != null) return;

		}

		public static void BackgroundElementsToString(List<BackgroundScene.BackgroundSceneElement> elements)
		{
			Debug.Log("background stringer");
			var lines = new List<string>();
			foreach (BackgroundScene.BackgroundSceneElement element in elements)
			{
				if (element is DistantBuilding building)
				{ Debug.Log(DistantBuildingToString(building)); }
			}

		}

		public static Vector2 PosToDrawPos(BackgroundScene self, Vector2 input, float depth)
		{
			return input / depth;
		}

		public static string DistantBuildingToString(DistantBuilding building)
		{
			Vector2 realPos = PosToDrawPos(building.scene, building.pos, building.depth);
			return $"DistantBuilding : {building.assetName}, {realPos.x}, {realPos.y}, {building.depth}, {building.atmosphericalDepthAdd}";
		}

		static Vector2 OldMousePos;

		static bool ActiveDrag;

		static string filePath() => IO.Path.Combine(RootFolderDirectory(), "background.txt");
	}
}
