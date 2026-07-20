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
		On.BackgroundScene.Update += BackgroundScene_Update;
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
		On.BackgroundScene.Update -= BackgroundScene_Update;
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
			self.CData().DepthUpdate = false;
			FNode? justBehindNode = null;
			FNode? frontmostNode = null;
			float frontmostDepth = float.MaxValue;
			float backDepth = float.MinValue;
			foreach (RoomCamera.SpriteLeaser i in rCam.spriteLeasers)
			{
				if (i.drawableObject is BackgroundScene.BackgroundSceneElement element && element != self && !element.CData().ReInitiateSprites && !element.CData().DepthUpdate)
				{
					if (element.depth < self.depth && element.depth > backDepth)
					{
						justBehindNode = i.sprites[^1];
						backDepth = element.depth;
					}

					if (element.depth < frontmostDepth)
					{
					frontmostNode = i.sprites[^1];
						frontmostDepth = element.depth;
					}

					if (self.room.roomSettings.BackgroundData().sceneData is Data.DayNightSceneData dayNight)
					{
						if (self == dayNight.SceneDaySky && i.drawableObject == dayNight.SceneDuskSky)
						{
							justBehindNode = null;
							frontmostNode = i.sprites[^1];
							break;
						}
						if (self == dayNight.SceneDuskSky && i.drawableObject == dayNight.SceneDaySky)
						{
							justBehindNode = i.sprites[^1];
							frontmostNode = null;
							break;
						}
						if (self == dayNight.SceneNightSky && i.drawableObject == dayNight.SceneDuskSky)
						{
							justBehindNode = i.sprites[^1];
							frontmostNode = null;
							break;
						}
					}

				}
			}

			if (justBehindNode != null)
			{
				foreach (FSprite sprite in sLeaser.sprites)
				{
					sprite.MoveBehindOtherNode(justBehindNode);
				}
			}
			else if (frontmostNode != null)
			{

				foreach (FSprite sprite in sLeaser.sprites)
				{
					sprite.MoveInFrontOfOtherNode(frontmostNode);
				}
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
	}
}
