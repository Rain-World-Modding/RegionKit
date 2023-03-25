using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static AboveCloudsView;
using static RegionKit.Modules.BackgroundBuilder.Data;
using static RoofTopView;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class BackgroundUpdates
{
	public static void Apply()
	{
		On.BackgroundScene.Update += BackgroundScene_Update;
		On.BackgroundScene.BackgroundSceneElement.DrawSprites += BackgroundSceneElement_DrawSprites;
		On.AboveCloudsView.Update += AboveCloudsView_Update;
		On.BackgroundScene.BackgroundSceneElement.DrawPos += BackgroundSceneElement_DrawPos;
	}

	private static Vector2 BackgroundSceneElement_DrawPos(On.BackgroundScene.BackgroundSceneElement.orig_DrawPos orig, BackgroundScene.BackgroundSceneElement self, Vector2 camPos, float hDisplace)
	{
		if (Input.GetKeyDown("v"))
		{ Debug.Log($"name is [{GetSpriteOfElement(self)?._atlas.name}] and pos is {orig(self, camPos, hDisplace)} and depth is {self.depth}"); }
		Vector2 offset = new Vector2();
		return orig(self, camPos, hDisplace) - offset;
	}

	private static void AboveCloudsView_Update(On.AboveCloudsView.orig_Update orig, AboveCloudsView self, bool eu)
	{
		orig(self, eu);
		if (!BuilderPage.checkForBackgroundPage(self.room.game.devUI)) return;

		if (self.room.roomSettings.BackgroundData().backgroundData is not CloudsBackgroundData data) return;

		//Debug.Log($"data {data.startAltitude} undata {self.startAltitude}");

		if ((data.startAltitude != null && data.startAltitude != self.startAltitude) ||
			(data.endAltitude != null && data.endAltitude != self.endAltitude))
		{
			self.startAltitude = data.startAltitude ?? self.startAltitude;
			self.endAltitude = data.endAltitude ?? self.endAltitude;
			self.sceneOrigo = new Vector2(2514f, (self.startAltitude + self.endAltitude) / 2f);
		}
	}

	private static void BackgroundSceneElement_DrawSprites(On.BackgroundScene.BackgroundSceneElement.orig_DrawSprites orig, BackgroundScene.BackgroundSceneElement self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (self.CData().ReInitiateSprites)
		{
			self.CData().ReInitiateSprites = false;
			sLeaser.RemoveAllSpritesFromContainer();
			self.InitiateSprites(sLeaser, rCam);
			self.CData().DepthUpdate = true;
		}

		if (self.CData().DepthUpdate)
		{
			Debug.Log("updating depth");
			self.CData().DepthUpdate = false;
			FNode? otherNode = null;
			bool otherNodeFound = false;
			foreach (RoomCamera.SpriteLeaser i in rCam.spriteLeasers)
			{
				if (i.drawableObject is BackgroundScene.BackgroundSceneElement element)
				{
					if (element.depth < self.depth)
					{
						otherNode = i.sprites[^1];
						otherNodeFound = true;
						break;
					}
				}
			}

			if (otherNodeFound && otherNode != null)
			{
				foreach (FSprite sprite in sLeaser.sprites)
				{ sprite.MoveInFrontOfOtherNode(otherNode); }
			}
		}
		orig(self, sLeaser, rCam, timeStacker, camPos);
	}

	public static void CloudDepthAdjust(AboveCloudsView self)
	{
		Debug.Log("CloudDepthAdjust");
		if (self.room.roomSettings.BackgroundData().backgroundData is not CloudsBackgroundData cloudsData) return;

		self.cloudsStartDepth = cloudsData.cloudsStartDepth ?? self.cloudsStartDepth;
		self.cloudsEndDepth = cloudsData.cloudsEndDepth ?? self.cloudsEndDepth;
		self.distantCloudsEndDepth = cloudsData.distantCloudsEndDepth ?? self.distantCloudsEndDepth;

		foreach (BackgroundScene.BackgroundSceneElement element in self.elements.ToList())
		{
			if (element is DistantCloud dcloud)
			{
				element.depth = self.DistantCloudDepth(dcloud.distantCloudDepth);
				element.CData().DepthUpdate = true;
			}

			else if(element is CloseCloud ccloud)
			{
				element.depth = self.CloudDepth(ccloud.cloudDepth);
				element.CData().DepthUpdate = true;
			}
		}
	}

	public static void CloudAmountAdjust(AboveCloudsView self)
	{
		if (self.room.roomSettings.BackgroundData().backgroundData is not CloudsBackgroundData cloudsData) return;
		foreach (BackgroundScene.BackgroundSceneElement element in self.elements.ToList())
		{
			if (element is DistantCloud or CloseCloud)
			{
				element.Destroy();
				self.elements.Remove(element);
			}
		}

		int num = (int)(cloudsData.cloudsCount ?? 7f);
		for (int i = 0; i < num; i++)
		{
			float cloudDepth = i / (float)(num - 1);
			CloseCloud cloud = new CloseCloud(self, new Vector2(0f, 0f), cloudDepth, i);
			cloud.CData().needsAddToRoom = true;
			self.AddElement(cloud);
		}

		num = (int)(cloudsData.distantCloudsCount ?? 11f);
		for (int j = 0; j < num; j++)
		{
			float num15 = j / (float)(num - 1);
			DistantCloud dcloud = new DistantCloud(self, new Vector2(0f, -40f * self.cloudsEndDepth * (1f - num15)), num15, j);
			dcloud.CData().needsAddToRoom = true;
			self.AddElement(dcloud);
		}
	}


	private static void BackgroundScene_Update(On.BackgroundScene.orig_Update orig, BackgroundScene self, bool eu)
	{
		orig(self, eu);

		if (!BuilderPage.checkForBackgroundPage(self.room.game.devUI)) return;

		foreach (BackgroundScene.BackgroundSceneElement element in self.elements)
		{
			if (element.CData().needsAddToRoom)
			{
				if (self.room.updateList.Contains(element))
				{
					element.CData().needsAddToRoom = false;
					continue;
				}

				self.room.AddObject(element);
				element.CData().needsAddToRoom = false;
				element.CData().DepthUpdate = true;
			}
		}


		bool controlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

		if (!Input.GetMouseButton(0) && ActiveDrag)
		{ ActiveDrag = false; }

		if (ActiveDrag && editObject != null)
		{
			if (controlPressed)
			{
				CData(editObject).DepthUpdate = true;
				editObject.depth += Futile.mousePosition.y - OldMousePos.y;
				if (editObject is AboveCloudsView.DistantBuilding dbuilding)
				{

					dbuilding.atmosphericalDepthAdd += Futile.mousePosition.x - OldMousePos.x;
				}

				else if (editObject is RoofTopView.DistantBuilding rfdbuilding)
				{
					rfdbuilding.atmosphericalDepthAdd += Futile.mousePosition.x - OldMousePos.x;
				}

				else if (editObject is Building building)
				{
				}

			}
			else
			{
				var vector = new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
				Vector2 movement = self.PosFromDrawPosAtNeutralCamPos(vector, editObject.depth) - self.PosFromDrawPosAtNeutralCamPos(OldMousePos, editObject.depth);
				editObject.pos += movement;
				OldMousePos = vector;
			}

			OldMousePos = Futile.mousePosition;
		}

		if (Input.GetMouseButton(0) && !ActiveDrag)
		{
			OldMousePos = new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
			editObject = null;
			foreach (BackgroundScene.BackgroundSceneElement element in self.elements)
			{
				if (ElementClicked(element, self.room.game.cameras[0]))
				{
					editObject = element;
					ActiveDrag = true;
					break;
				}
			}
		}

	}

	public static bool ElementClicked(BackgroundScene.BackgroundSceneElement element, RoomCamera cam)
	{
		if (!ElementIsDraggable(element)) return false;

		FSprite? sprite = GetSpriteOfElement(element);
		if (sprite == null || (sprite._atlas.texture is not Texture2D tex)) return false;

		Vector2 offset = new();
		if (element.scene is AboveCloudsView acv)
		{ offset = new Vector2(0, acv.yShift); }

		Vector2 mouseOnSpritePos = MouseOnElementPos(element, cam, tex.width, offset);

		if (mouseOnSpritePos.x < 0 || mouseOnSpritePos.x > tex.width || mouseOnSpritePos.y < 0 || mouseOnSpritePos.y > tex.height) return false;
		if (tex.GetPixel((int)mouseOnSpritePos.x, (int)mouseOnSpritePos.y).a <= 0.5f) return false;

		return true;
	}

	public static Vector2 MouseOnElementPos(BackgroundScene.BackgroundSceneElement element, RoomCamera cam, float texWidth, Vector2 shift = new())
	{
		var mousePos = new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
		Vector2 drawPos = element.DrawPos(cam.pos + shift, cam.hDisplace);
		return mousePos - drawPos + new Vector2(texWidth / 2, 0);
	}

	public static FSprite? GetSpriteOfElement(BackgroundScene.BackgroundSceneElement element)
	{
		if (element is AboveCloudsView.DistantBuilding dbuilding)
		{ return new FSprite(dbuilding.assetName, true); }

		else if (element is RoofTopView.DistantBuilding rfdbuilding)
		{ return new FSprite(rfdbuilding.assetName, true); }

		else if (element is Floor floor)
		{ return new FSprite(floor.assetName, true); }

		else if (element is Rubble rubble)
		{ return new FSprite(rubble.assetName, true); }

		else if (element is Building Building)
		{ return new FSprite(Building.assetName, true); }

		else if (element is CloseCloud cloud)
		{ return new FSprite("clouds" + (cloud.index % 3 + 1).ToString(), true); }

		else if (element is DistantCloud dcloud)
		{ return new FSprite("clouds" + (dcloud.index % 3 + 1).ToString(), true); }

		else if (element is FlyingCloud)
		{ return new FSprite("flyingClouds1", true); }

		else return null;
	}

	public static bool ElementIsDraggable(BackgroundScene.BackgroundSceneElement element)
	{
		return (element is AboveCloudsView.DistantBuilding or RoofTopView.DistantBuilding or DistantLightning or Building or Rubble or Floor);
	}

	static Vector2 OldMousePos;

	static BackgroundScene.BackgroundSceneElement? editObject = null;

	static bool ActiveDrag;


	private static readonly ConditionalWeakTable<BackgroundScene.BackgroundSceneElement, BoolClass> table = new();

	public static BoolClass CData(this BackgroundScene.BackgroundSceneElement p) => table.GetValue(p, _ => new BoolClass());

	public class BoolClass
	{
		public bool DepthUpdate = false;
		public bool needsAddToRoom = false;
		public bool ReInitiateSprites = false;
	}

}
