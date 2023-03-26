using System;

namespace RegionKit.Modules.ConcealedGarden;

internal class CGCameraEffects
{

	internal class CGCameraEffectsObj : UpdatableAndDeletable
	{
		public CGCameraEffectsObj(Room room, PlacedObject pObj)
		{ }
	}
	internal static void Apply()
	{
		On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
	}

	internal static void Undo()
	{
		On.RoomCamera.ApplyPalette -= RoomCamera_ApplyPalette;
	}

	private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera self)
	{
		orig(self);
		if (self.room != null && self.fullScreenEffect == null)
		{
			foreach(PlacedObject pObj in self.room.roomSettings.placedObjects)
			{
				if (pObj != null && pObj.type.ToString() == "CGFullScreenShader")
				{
					ManagedData data = pObj.data as ManagedData;

					string shader = data.GetValue<string>("shader");
					if (!self.game.rainWorld.Shaders.ContainsKey(shader))
					{ continue; }

					self.SetUpFullScreenEffect(data.GetValue<Enum>("container")!.ToString());
					self.fullScreenEffect.shader = self.game.rainWorld.Shaders[shader];
					self.lightBloomAlphaEffect = RoomSettings.RoomEffect.Type.None;
					self.lightBloomAlpha = data.GetValue<float>("alpha");
				}
			}
			/*
			if (self.room.roomSettings.GetEffectAmount(_Enums.CGWaterFallEffect) > 0f)
			{
				self.SetUpFullScreenEffect("Foreground");
				self.fullScreenEffect.shader = self.game.rainWorld.Shaders["WaterFall"];
				self.lightBloomAlphaEffect = _Enums.CGWaterFallEffect;
				self.lightBloomAlpha = self.room.roomSettings.GetEffectAmount(_Enums.CGWaterFallEffect);
				self.fullScreenEffect.color = new UnityEngine.Color(self.lightBloomAlpha, 1f - self.lightBloomAlpha, 1f - self.lightBloomAlpha);
			}
			else if (self.room.roomSettings.GetEffectAmount(_Enums.CGSteamEffect) > 0f)
			{
				self.SetUpFullScreenEffect("Water");
				self.fullScreenEffect.shader = self.game.rainWorld.Shaders["Steam"];
				self.lightBloomAlphaEffect = _Enums.CGSteamEffect;
				self.lightBloomAlpha = self.room.roomSettings.GetEffectAmount(_Enums.CGSteamEffect);
			}
			else if (self.room.roomSettings.GetEffectAmount(_Enums.CGHeatEffect) > 0f)
			{
				self.SetUpFullScreenEffect("Bloom");
				self.fullScreenEffect.shader = self.game.rainWorld.Shaders["HeatDistortion"];
				self.lightBloomAlphaEffect = _Enums.CGHeatEffect;
				self.lightBloomAlpha = self.room.roomSettings.GetEffectAmount(_Enums.CGHeatEffect);
			}*/
		}
	}
}
