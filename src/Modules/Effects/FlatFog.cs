namespace RegionKit.Modules.Effects
{
	internal static class FlatFog
	{
		internal static void Apply()
		{
			On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
			On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
		}

		internal static void Undo()
		{
			On.RoomCamera.ApplyPalette -= RoomCamera_ApplyPalette;
			On.RoomCamera.DrawUpdate -= RoomCamera_DrawUpdate;
		}

		private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera self)
		{
			orig(self);
			if (self.room != null && self.ghostMode <= 0f)
			{
				RoomSettings.RoomEffect? flatFogEffect = self.room.roomSettings.GetEffect(_Enums.FlatFog);
				if (flatFogEffect != null && flatFogEffect.GetAmount(3) > 0f)
				{
					self.SetUpFullScreenEffect("Bloom");
					self.fullScreenEffect.shader = self.game.rainWorld.Shaders["RKFlatFog"];
					self.lightBloomAlphaEffect = _Enums.FlatFog;
					self.lightBloomAlpha = flatFogEffect.GetAmount(3);
				}
			}
		}

		private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
		{
			orig(self, timeStacker, timeSpeed);
			if (self.room != null && self.lightBloomAlphaEffect == _Enums.FlatFog)
			{
				RoomSettings.RoomEffect? flatFogEffect = self.room.roomSettings.GetEffect(_Enums.FlatFog);
				if (flatFogEffect != null)
				{
					self.fullScreenEffect.color = new Color(flatFogEffect.GetAmount(0), flatFogEffect.GetAmount(1), flatFogEffect.GetAmount(2));
					self.fullScreenEffect.alpha = flatFogEffect.GetAmount(3);
					self.lightBloomAlpha = flatFogEffect.GetAmount(3);
				}
			}
		}
	}
}
