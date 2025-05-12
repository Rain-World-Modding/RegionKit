using Mono.Cecil.Cil;
using MonoMod.Cil;
using static AboveCloudsView;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class Init
{
	public static void Apply()
	{
		On.RoofTopView.ctor += RoofTopView_ctor;
		On.AboveCloudsView.ctor += AboveCloudsView_ctor;
		On.BackgroundScene.RoomToWorldPos += BackgroundScene_RoomToWorldPos;
		On.AboveCloudsView.CloseCloud.DrawSprites += CloseCloud_DrawSprites;
		On.AboveCloudsView.DistantCloud.DrawSprites += DistantCloud_DrawSprites;
		On.AboveCloudsView.Update += AboveCloudsView_Update;
		On.RoofTopView.Update += RoofTopView_Update;
		On.Watcher.AncientUrbanView.ctor += AncientUrbanView_ctor;
		On.Watcher.AncientUrbanView.Update += AncientUrbanView_Update;
		On.RotWormScene.ctor += RotWormScene_ctor;
		_CommonHooks.PostRoomLoad += PostRoomLoad;
	}

	public static void Undo()
	{
		On.RoofTopView.ctor -= RoofTopView_ctor;
		On.AboveCloudsView.ctor -= AboveCloudsView_ctor;
		On.BackgroundScene.RoomToWorldPos -= BackgroundScene_RoomToWorldPos;
		On.AboveCloudsView.CloseCloud.DrawSprites -= CloseCloud_DrawSprites;
		On.AboveCloudsView.DistantCloud.DrawSprites -= DistantCloud_DrawSprites;
		On.AboveCloudsView.Update -= AboveCloudsView_Update;
		On.RoofTopView.Update -= RoofTopView_Update;
		On.Watcher.AncientUrbanView.ctor -= AncientUrbanView_ctor;
		On.Watcher.AncientUrbanView.Update -= AncientUrbanView_Update;
		On.RotWormScene.ctor -= RotWormScene_ctor;
		_CommonHooks.PostRoomLoad -= PostRoomLoad;
	}


	private static void PostRoomLoad(Room room)
	{
		if (room.game == null) return;
		Data.RoomBGData data = room.roomSettings.BackgroundData();

		if (!data.sceneData.sceneInitialized && data.type != BackgroundTemplateType.None)
			BackgroundPage.SwitchRoomBackground(room, data.type);
	}

	private static void RotWormScene_ctor(On.RotWormScene.orig_ctor orig, RotWormScene self, Room room)
	{
		orig(self, room);
		Data.RoomBGData data = self.room.roomSettings.BackgroundData();

		if (data.type != BackgroundTemplateType.RotWormScene || data.sceneData is not Data.RotWormScene_SceneData)
		{ data.backgroundName = ""; data.SetBGTypeAndData(BackgroundTemplateType.RotWormScene); }
		data.LoadSceneData(self);
		data.sceneData.MakeScene(self);
	}

	private static void AncientUrbanView_Update(On.Watcher.AncientUrbanView.orig_Update orig, Watcher.AncientUrbanView self, bool eu)
	{
		orig(self, eu);

		RainCycle rainCycle = self.room.world.rainCycle;
		if ((self.room.game.cameras[0].effect_dayNight > 0f && rainCycle.timer >= rainCycle.cycleLength)
			|| (ModManager.Expedition && self.room.game.rainWorld.ExpeditionMode))
		{
			if (self.room.roomSettings.BackgroundData().sceneData is Data.DayNightSceneData dayNightScene)
			{
				dayNightScene.ColorUpdate();
			}
		}
	}

	private static void AncientUrbanView_ctor(On.Watcher.AncientUrbanView.orig_ctor orig, Watcher.AncientUrbanView self, Room room, RoomSettings.RoomEffect effect)
	{
		//ye I know this is a little hacky but it works and it's less annoying than using a bool or smthn
		Vector2 roomOffset = room.roomSettings.BackgroundData().roomOffset;
		Vector2 backgroundOffset = room.roomSettings.BackgroundData().backgroundOffset;

		room.roomSettings.BackgroundData().roomOffset = new();
		room.roomSettings.BackgroundData().backgroundOffset = new();

		orig(self, room, effect);

		room.roomSettings.BackgroundData().roomOffset = roomOffset;
		room.roomSettings.BackgroundData().backgroundOffset = backgroundOffset;

		Data.RoomBGData data = self.room.roomSettings.BackgroundData();

		if (data.type != BackgroundTemplateType.AncientUrbanView || data.sceneData is not Data.AncientUrbanView_SceneData)
		{ data.backgroundName = ""; data.SetBGTypeAndData(BackgroundTemplateType.AncientUrbanView); }
		data.LoadSceneData(self);
		data.sceneData.MakeScene(self);
	}

	private static void RoofTopView_Update(On.RoofTopView.orig_Update orig, RoofTopView self, bool eu)
	{
		orig(self, eu);

		RainCycle rainCycle = self.room.world.rainCycle;
		if ((self.room.game.cameras[0].effect_dayNight > 0f && rainCycle.timer >= rainCycle.cycleLength)
			|| (ModManager.Expedition && self.room.game.rainWorld.ExpeditionMode))
		{
			if (self.room.roomSettings.BackgroundData().sceneData is Data.DayNightSceneData dayNightScene)
			{
				dayNightScene.ColorUpdate();
			}
		}
	}

	private static void AboveCloudsView_Update(On.AboveCloudsView.orig_Update orig, AboveCloudsView self, bool eu)
	{
		orig(self, eu);

		RainCycle rainCycle = self.room.world.rainCycle;
		if ((self.room.game.cameras[0].effect_dayNight > 0f && rainCycle.timer >= rainCycle.cycleLength)
			|| (ModManager.Expedition && self.room.game.rainWorld.ExpeditionMode))
		{
			if (self.room.roomSettings.BackgroundData().sceneData is Data.DayNightSceneData dayNightScene)
			{
				dayNightScene.ColorUpdate();
			}
		}
	}

	private static void DistantCloud_DrawSprites(On.AboveCloudsView.DistantCloud.orig_DrawSprites orig, DistantCloud self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		//pixel fix
		orig(self, sLeaser, rCam, timeStacker, camPos);
		sLeaser.sprites[0].scaleY += 2f;
	}

	private static void CloseCloud_DrawSprites(On.AboveCloudsView.CloseCloud.orig_DrawSprites orig, CloseCloud self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		//pixel fix
		orig(self, sLeaser, rCam, timeStacker, camPos);
		sLeaser.sprites[0].scaleY += 2f;
	}

	private static Vector2 BackgroundScene_RoomToWorldPos(On.BackgroundScene.orig_RoomToWorldPos orig, BackgroundScene self, Vector2 inRoomPos)
	{
		//offset
		if (self.slatedForDeletetion) return Vector2.zero;
		return orig(self, inRoomPos) + (self.room.roomSettings.BackgroundData().roomOffset * 20) + (self.room.roomSettings.BackgroundData().backgroundOffset * 20);
	}

	private static void AboveCloudsView_ctor(On.AboveCloudsView.orig_ctor orig, AboveCloudsView self, Room room, RoomSettings.RoomEffect effect)
	{
		orig(self, room, effect);
		Shader.SetGlobalFloat("_windDir", ModManager.MSC ? -1f : 1f);
		Data.RoomBGData data = self.room.roomSettings.BackgroundData();

		if (data.type != BackgroundTemplateType.AboveCloudsView || data.sceneData is not Data.AboveCloudsView_SceneData)
		{ data.backgroundName = ""; data.SetBGTypeAndData(BackgroundTemplateType.AboveCloudsView); }
		data.LoadSceneData(self);
		data.sceneData.MakeScene(self);
	}

	private static void RoofTopView_ctor(On.RoofTopView.orig_ctor orig, RoofTopView self, Room room, RoomSettings.RoomEffect effect)
	{
		//ye I know this is a little hacky but it works and it's less annoying than using a bool or smthn
		Vector2 roomOffset = room.roomSettings.BackgroundData().roomOffset;
		Vector2 backgroundOffset = room.roomSettings.BackgroundData().backgroundOffset;

		room.roomSettings.BackgroundData().roomOffset = new();
		room.roomSettings.BackgroundData().backgroundOffset = new();

		orig(self, room, effect);

		room.roomSettings.BackgroundData().roomOffset = roomOffset;
		room.roomSettings.BackgroundData().backgroundOffset = backgroundOffset;

		Data.RoomBGData data = self.room.roomSettings.BackgroundData();

		if (data.type != BackgroundTemplateType.RoofTopView || data.sceneData is not Data.RoofTopView_SceneData)
		{ data.backgroundName = ""; data.SetBGTypeAndData(BackgroundTemplateType.RoofTopView); }
		data.LoadSceneData(self);
		data.sceneData.MakeScene(self);
	}
}
