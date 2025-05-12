using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EffExt;
using RegionKit.Extras;
using static AboveCloudsView;
using static RoofTopView;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class BackgroundUpdates
{
	public static void Apply()
	{
		//On.BackgroundScene.Update += BackgroundScene_Update;
		On.BackgroundScene.BackgroundSceneElement.DrawSprites += BackgroundSceneElement_DrawSprites;
		On.BackgroundScene.BackgroundSceneElement.InitiateSprites += BackgroundSceneElement_InitiateSprites;
		On.BackgroundScene.BackgroundSceneElement.AddToContainer += BackgroundSceneElement_AddToContainer;
		//On.BackgroundScene.BackgroundSceneElement.DrawPos += BackgroundSceneElement_DrawPos;
		On.RoofTopView.Building.InitiateSprites += Building_InitiateSprites;
		On.RoofTopView.DistantBuilding.InitiateSprites += Building_InitiateSprites;
		On.AboveCloudsView.DistantBuilding.InitiateSprites += Building_InitiateSprites;
		On.AboveCloudsView.DistantLightning.InitiateSprites += Building_InitiateSprites;
		On.RoofTopView.Smoke.InitiateSprites += Building_InitiateSprites;
		On.BackgroundScene.FullScreenSingleColor.DrawSprites += FullScreenSingleColor_DrawSprites;
		On.BackgroundScene.LoadGraphic += BackgroundScene_LoadGraphic;
	}

	public static void Undo()
	{
		//On.BackgroundScene.Update += BackgroundScene_Update;
		On.BackgroundScene.BackgroundSceneElement.DrawSprites -= BackgroundSceneElement_DrawSprites;
		On.BackgroundScene.BackgroundSceneElement.InitiateSprites -= BackgroundSceneElement_InitiateSprites;
		On.BackgroundScene.BackgroundSceneElement.AddToContainer -= BackgroundSceneElement_AddToContainer;
		//On.BackgroundScene.BackgroundSceneElement.DrawPos += BackgroundSceneElement_DrawPos;
		On.RoofTopView.Building.InitiateSprites -= Building_InitiateSprites;
		On.RoofTopView.DistantBuilding.InitiateSprites -= Building_InitiateSprites;
		On.AboveCloudsView.DistantBuilding.InitiateSprites -= Building_InitiateSprites;
		On.AboveCloudsView.DistantLightning.InitiateSprites -= Building_InitiateSprites;
		On.RoofTopView.Smoke.InitiateSprites -= Building_InitiateSprites;
		On.BackgroundScene.FullScreenSingleColor.DrawSprites -= FullScreenSingleColor_DrawSprites;
		On.BackgroundScene.LoadGraphic -= BackgroundScene_LoadGraphic;
	}

	private static void BackgroundScene_LoadGraphic(On.BackgroundScene.orig_LoadGraphic orig, BackgroundScene self, string elementName, bool crispPixels, bool clampWrapMode)
	{
		if (Futile.atlasManager.DoesContainElementWithName(elementName)) return;
		orig(self, elementName, crispPixels, clampWrapMode);
	}

	private static void FullScreenSingleColor_DrawSprites(On.BackgroundScene.FullScreenSingleColor.orig_DrawSprites orig, BackgroundScene.FullScreenSingleColor self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (self is Fog && self.room.roomSettings.BackgroundData().sceneData is Data.AboveCloudsView_SceneData data && (data._startFogAltitude != null || data._endFogAltitude != null))
		{
			float num = self.scene.RoomToWorldPos(camPos).y + (data.Scene == null ? 0f : data.Scene.yShift);
			self.alpha = Mathf.InverseLerp(data.endFogAltitude, data.startFogAltitude, num) * 0.6f;
		}
		orig(self, sLeaser, rCam, timeStacker, camPos);
	}

	private static void BackgroundSceneElement_AddToContainer(On.BackgroundScene.BackgroundSceneElement.orig_AddToContainer orig, BackgroundScene.BackgroundSceneElement self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		if (self.room.roomSettings.BackgroundData().sceneData.defaultContainer is ContainerCodes container)
		{
			try { newContatiner = rCam.ReturnFContainer(container); }
			catch (Exception e) { LogError("[BackgroundBuilder]: failed to set background element default container\n" + e); }

		}

		if (self.CData().dataElement?.container is ContainerCodes container2)
		{
			try { newContatiner = rCam.ReturnFContainer(container2); }
			catch (Exception e) { LogError("[BackgroundBuilder]: failed to set background element specific container\n" + e); }
		}

		orig(self, sLeaser, rCam, newContatiner);
	}

	private static void Building_InitiateSprites<T, S>(T orig, S self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		where T : Delegate
		where S : BackgroundScene.BackgroundSceneElement
	{
		orig.DynamicInvoke(self, sLeaser, rCam);
		if (self.CData().dataElement?.anchorPos is Vector2 anchor)
		{
			foreach (FSprite sprite in sLeaser.sprites)
			{ sprite.SetAnchor(anchor); }
		}
		if (self is RoofTopView.Smoke && self.CData().dataElement is BackgroundElementData.RTV_Smoke dsmoke && dsmoke.spriteName != null)
		{
			self.scene.LoadGraphic(dsmoke.spriteName, false, false);
			sLeaser.sprites[0].SetElementByName(dsmoke.spriteName);
		}
	}

	private static void BackgroundSceneElement_InitiateSprites(On.BackgroundScene.BackgroundSceneElement.orig_InitiateSprites orig, BackgroundScene.BackgroundSceneElement self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		orig(self, sLeaser, rCam);
	}

	private static Vector2 BackgroundSceneElement_DrawPos(On.BackgroundScene.BackgroundSceneElement.orig_DrawPos orig, BackgroundScene.BackgroundSceneElement self, Vector2 camPos, float hDisplace)
	{
		//if (Input.GetKeyDown("v"))
		//{ LogMessage($"name is [{GetSpriteOfElement(self)?._atlas.name}] and pos is {orig(self, camPos, hDisplace)} and depth is {self.depth}"); }
		Vector2 offset = new Vector2();
		return orig(self, camPos, hDisplace) - offset;
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
			LogMessage("updating depth");
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

		if (self.CData().dataElement is BackgroundElementData.CustomBgElement element2)
		{
			element2.UpdateElementSprites(self, sLeaser);
		}
	}


	private static void BackgroundScene_Update(On.BackgroundScene.orig_Update orig, BackgroundScene self, bool eu)
	{
		orig(self, eu);

		//if (!BuilderPage.checkForBackgroundPage(self.room.game.devUI)) return;

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


	private static readonly ConditionalWeakTable<BackgroundScene.BackgroundSceneElement, InstanceData> table = new();

	public static InstanceData CData(this BackgroundScene.BackgroundSceneElement p) => table.GetValue(p, _ => new InstanceData());

	public class InstanceData
	{
		public bool DepthUpdate = false;
		public bool needsAddToRoom = false;
		public bool ReInitiateSprites = false;
		public BackgroundElementData.CustomBgElement? dataElement = null;
	}

}
