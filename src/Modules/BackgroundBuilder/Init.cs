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
	}

	public static void Undo()
	{
		On.RoofTopView.ctor -= RoofTopView_ctor;
		On.AboveCloudsView.ctor -= AboveCloudsView_ctor;
		On.BackgroundScene.RoomToWorldPos -= BackgroundScene_RoomToWorldPos;
		On.AboveCloudsView.CloseCloud.DrawSprites -= CloseCloud_DrawSprites;
		On.AboveCloudsView.DistantCloud.DrawSprites -= DistantCloud_DrawSprites;
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

		Data.RoomBGData data = self.room.roomSettings.BackgroundData();

		if (data.type != BackgroundTemplateType.AboveCloudsView || data.realData is not Data.AboveCloudsView_BGData)
		{ data.backgroundName = ""; data.SetBGTypeAndData(BackgroundTemplateType.AboveCloudsView); }

		data.realData.MakeScene(self);
	}

	private static void RoofTopView_ctor(On.RoofTopView.orig_ctor orig, RoofTopView self, Room room, RoomSettings.RoomEffect effect)
	{
		orig(self, room, effect);

		Data.RoomBGData data = self.room.roomSettings.BackgroundData();

		if (data.type != BackgroundTemplateType.RoofTopView || data.realData is not Data.RoofTopView_BGData)
		{ data.backgroundName = ""; data.SetBGTypeAndData(BackgroundTemplateType.RoofTopView); }

		data.realData.MakeScene(self);
	}
}
