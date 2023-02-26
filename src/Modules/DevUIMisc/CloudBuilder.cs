using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AboveCloudsView;
using System.Text.RegularExpressions;
using Menu.Remix.MixedUI;

namespace RegionKit.Modules.DevUIMisc
{
	internal static class CloudBuilder
	{
		public static void Apply()
		{
			On.AboveCloudsView.Update += AboveCloudsView_Update;
		}

		private static void AboveCloudsView_Update(On.AboveCloudsView.orig_Update orig, AboveCloudsView self, bool eu)
		{
			orig(self, eu);

			if (Input.GetKeyDown("z"))
			{
				foreach (BackgroundScene.BackgroundSceneElement element in self.elements.ToList())
				{
					if (element is DistantBuilding or DistantLightning)
					{
						element.Destroy();
						self.elements.Remove(element);
					}
				}
			}

			if(Input.GetKeyDown("x")) 
			{
				self.elementsAddedToRoom = false;
				foreach (string str in System.IO.File.ReadAllLines(filePath()))
				{
					string[] parts = Regex.Split(str, " : ");

					if (parts.Length != 2) continue;

					string[] args = Regex.Split(parts[1], ", ");

					switch (parts[0])
					{
					case "DistantBuilding":
						if (args.Length != 5) break;

						string name = args[0];
						if (!float.TryParse(args[1], out float x)) break;
						if (!float.TryParse(args[2], out float y)) break;
						if (!float.TryParse(args[3], out float z)) break;
						if (!float.TryParse(args[2], out float z2)) break;

						self.AddElement(new DistantBuilding(self, name, self.PosFromDrawPosAtNeutralCamPos(new Vector2(x, y), z), z, z2));
						break;

					case "DistantLightning":
						if (args.Length != 5) break;

						string nameL = args[0];
						if (!float.TryParse(args[1], out float xL)) break;
						if (!float.TryParse(args[2], out float yL)) break;
						if (!float.TryParse(args[3], out float zL)) break;
						if (!float.TryParse(args[2], out float z2L)) break;

						self.AddElement(new DistantLightning(self, nameL, self.PosFromDrawPosAtNeutralCamPos(new Vector2(xL, yL), zL), zL, z2L));
						break;
					}
				}
			}

			if (self.room.game.devToolsActive || ModManager.DevTools)
			{
				if (!Input.GetMouseButtonDown(0) && Input.GetMouseButton(0))
				{
					int num5 = 0;
					for (int i = 0; i < self.elements.Count; i++)
					{
						if (self.elements[i] is DistantBuilding)
						{
							if (num5 == self.editObject)
							{
								Vector2 vector = new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
								Vector2 movement = self.PosFromDrawPosAtNeutralCamPos(vector, self.elements[i].depth) - self.PosFromDrawPosAtNeutralCamPos(OldMousePos, self.elements[i].depth);
								self.elements[i].pos += movement;
								OldMousePos = vector;
								break;
							}
							num5++;
						}
					}
				}

				if (Input.GetMouseButtonDown(0))
				{
					OldMousePos = new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
					self.editObject = -1;
					int index = -1;
					foreach (BackgroundScene.BackgroundSceneElement element in self.elements)
					{
						if (element is DistantBuilding building)
						{
							index++;
							Vector2 mousePos = new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
							Vector2 drawPos = building.DrawPos(self.room.game.cameras[0].pos + new Vector2(0, building.AboveCloudsScene.yShift), self.room.game.cameras[0].hDisplace);
							FSprite sprite = new FSprite(building.assetName, true);

							if (sprite._atlas.texture is not Texture2D tex) continue;

							Vector2 mouseOnSpritePos = mousePos - drawPos + new Vector2(tex.width / 2, 0);

							if (mouseOnSpritePos.x < 0 || mouseOnSpritePos.x > tex.width || mouseOnSpritePos.y < 0 || mouseOnSpritePos.y > tex.height) continue;

							if (tex.GetPixel((int)mouseOnSpritePos.x, (int)mouseOnSpritePos.y).a <= 0.5f) continue;
							

							self.editObject = index;
							Debug.Log($"Editing {index}");
							break;
						}
					}
				}
			}
		}

		static Vector2 OldMousePos;

		static string filePath() => System.IO.Path.Combine(Custom.RootFolderDirectory(), "background.txt");
	}
}
