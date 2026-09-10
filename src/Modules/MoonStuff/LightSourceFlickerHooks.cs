namespace RegionKit.Modules.MoonStuff
{
	internal static class LightSourceFlickerHooks
	{
		internal static void Apply()
		{
			On.LightSource.DrawSprites += LightSourceOnOff;
			On.LightBeam.DrawSprites += LightBeamOnOff;
		}

		internal static void Undo()
		{
			On.LightSource.DrawSprites -= LightSourceOnOff;
			On.LightBeam.DrawSprites -= LightBeamOnOff;
		}

		private static void LightSourceOnOff(On.LightSource.orig_DrawSprites orig, LightSource self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (_CWTs.TryGetMoonLightSourceData(self, out _CWTs.FlickerData data))
			{
				foreach (FSprite sprite in sLeaser.sprites)
				{
					sprite.alpha *= data.Alpha(timeStacker);
				}
			}
		}

		private static void LightBeamOnOff(On.LightBeam.orig_DrawSprites orig, LightBeam self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (_CWTs.TryGetMoonLightBeamData(self, out _CWTs.FlickerData data))
			{
				var vertColors = (sLeaser.sprites[0] as TriangleMesh)!.verticeColors;
				for (int i = 0; i < vertColors.Length; i++)
				{
					vertColors[i].a *= data.Alpha(timeStacker);
				}
			}
		}
	}
}
