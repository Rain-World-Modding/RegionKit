using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.RuntimeDetour;
using static AboveCloudsView;
using static RoofTopView;
using static VoidSea.VoidSeaScene;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class ExceptionFixes
{

	public static void Apply()
	{
		//AboveCloudsView
		DrawSpriteHooks.Add(new Hook(typeof(AboveCloudsView.DistantBuilding).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(DistantCloud).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(CloseCloud).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(FlyingCloud).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(DistantLightning).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(Fog).GetMethod("DrawSprites"), DrawSpritesHK));

		//RoofTopView
		DrawSpriteHooks.Add(new Hook(typeof(RoofTopView.DistantBuilding).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(Building).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(DistantGhost).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(DustWave).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(Floor).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(Rubble).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(RoofTopView.Smoke).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(DistantCloud).GetMethod("DrawSprites"), DrawSpritesHK));

		//VoidSeaScene
		DrawSpriteHooks.Add(new Hook(typeof(VoidSea.DistantWormLight).GetMethod("DrawSprites"), DrawSpritesHK));
		//DrawSpriteHooks.Add(new Hook(typeof(TheEgg).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(VoidCeiling).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(VoidSeaBkg).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(VoidSeaFade).GetMethod("DrawSprites"), DrawSpritesHK));
		//DrawSpriteHooks.Add(new Hook(typeof(VoidSeaSceneElement).GetMethod("DrawSprites"), DrawSpritesHK));
		//DrawSpriteHooks.Add(new Hook(typeof(VoidSprite).GetMethod("DrawSprites"), DrawSpritesHK));
		//DrawSpriteHooks.Add(new Hook(typeof(WormLightFade).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(VoidSeaFade).GetMethod("DrawSprites"), DrawSpritesHK));
		DrawSpriteHooks.Add(new Hook(typeof(VoidSea.VoidWorm).GetMethod("DrawSprites"), DrawSpritesHK));
	}

	public static void Undo()
	{
		foreach (Hook hook in DrawSpriteHooks)
		{
			hook.Undo();
		}
	}

	private static List<Hook> DrawSpriteHooks = new List<Hook>();
	private static void DrawSpritesHK(Action<UpdatableAndDeletable, RoomCamera.SpriteLeaser, RoomCamera, float, Vector2> orig, UpdatableAndDeletable self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (self.slatedForDeletetion)
		{
			sLeaser.CleanSpritesAndRemove();
			return;
		}
		orig(self, sLeaser, rCam, timeStacker, camPos);
	}
}
