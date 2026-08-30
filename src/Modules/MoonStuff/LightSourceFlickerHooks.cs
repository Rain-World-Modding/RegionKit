namespace RegionKit.Modules.MoonStuff
{
	internal static class LightSourceFlickerHooks
	{
		internal static void Apply()
		{
			On.SpotLight.ctor += SpotLightTurnOn;
			On.SpotLight.DrawSprites += SpotLightOnOff;

			On.LightSource.ctor_Vector2_bool_Color_UpdatableAndDeletable += LightSourceTurnOn;
			On.LightSource.DrawSprites += LightSourceOnOff;

			On.LightBeam.ctor += LightBeamTurnOn;
			On.LightBeam.DrawSprites += LightBeamOnOff;
		}

		internal static void Undo()
		{
			On.SpotLight.ctor -= SpotLightTurnOn;
			On.SpotLight.DrawSprites -= SpotLightOnOff;

			On.LightSource.ctor_Vector2_bool_Color_UpdatableAndDeletable -= LightSourceTurnOn;
			On.LightSource.DrawSprites -= LightSourceOnOff;

			On.LightBeam.ctor -= LightBeamTurnOn;
			On.LightBeam.DrawSprites -= LightBeamOnOff;
		}


		private static void LightBeamOnOff(On.LightBeam.orig_DrawSprites orig, LightBeam self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			sLeaser.sprites[0].isVisible = _CWTs.MoonLightBeamData(self).On;
			orig(self, sLeaser, rCam, timeStacker, camPos);
		}

		private static void LightBeamTurnOn(On.LightBeam.orig_ctor orig, LightBeam self, PlacedObject placedObject)
		{
			orig(self, placedObject);
			_CWTs.MoonLightBeamData(self).On = true;
		}

		private static void LightSourceOnOff(On.LightSource.orig_DrawSprites orig, LightSource self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (self.tiedToObject == null)
			{
				sLeaser.sprites[0].isVisible = _CWTs.MoonLightSourceData(self).On;
			}

			orig(self, sLeaser, rCam, timeStacker, camPos);
		}

		private static void LightSourceTurnOn(On.LightSource.orig_ctor_Vector2_bool_Color_UpdatableAndDeletable orig, LightSource self, Vector2 initPos, bool environmentalLight, Color color, UpdatableAndDeletable tiedToObject)
		{
			orig(self, initPos, environmentalLight, color, tiedToObject);
			if (self.tiedToObject == null)
			{
				_CWTs.MoonLightSourceData(self).On = true;
			}
		}

		private static void SpotLightOnOff(On.SpotLight.orig_DrawSprites orig, SpotLight self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			sLeaser.sprites[0].isVisible = _CWTs.MoonSpotLightData(self).On;
			orig(self, sLeaser, rCam, timeStacker, camPos);
		}

		private static void SpotLightTurnOn(On.SpotLight.orig_ctor orig, SpotLight self, PlacedObject placedObject)
		{
			orig(self, placedObject);
			_CWTs.MoonSpotLightData(self).On = true;
		}
	}
}
