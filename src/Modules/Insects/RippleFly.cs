
namespace RegionKit.Modules.Insects
{
	public class RippleFly : FireFly
	{
		public RippleFly(Room room, Vector2 pos) : base(room, pos)
		{
			rippleLayer = 1;
			col = Color.Lerp(RainWorld.RippleColor, RainWorld.RippleGold, UnityEngine.Random.value);
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			if (light != null && !slatedForDeletetion && !light.slatedForDeletetion && light is not RippleLight)
			{
				light.Destroy();
				light = new RippleLight(light.pos, true, light.color, light.tiedToObject)
				{
					setPos = light.pos,
					setAlpha = light.alpha,
					setRad = light.rad,
					noGameplayImpact = true
				};
				room.AddObject(light);
			}
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			base.InitiateSprites(sLeaser, rCam);
			sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["RippleBasicRippleSideAlt"];
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			sLeaser.sprites[0].isVisible = rCam.rippleData != null && rCam.rippleData.isPassAdded;
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		}

		public class RippleLight : LightSource
		{
			internal static void Apply()
			{
				On.LightSource.InitiateSprites += OverrideInitiateSprites;
				On.LightSource.DrawSprites += LightSource_DrawSprites;
			}

			internal static void Undo()
			{
				On.LightSource.InitiateSprites -= OverrideInitiateSprites;
			}

			public RippleLight(Vector2 initPos, bool environmentalLight, Color color, UpdatableAndDeletable tiedToObject) : base(initPos, environmentalLight, color, tiedToObject)
			{
			}

			public RippleLight(Vector2 initPos, bool environmentalLight, Color color, UpdatableAndDeletable tiedToObject, bool submersible) : base(initPos, environmentalLight, color, tiedToObject, submersible)
			{
			}

			private static void OverrideInitiateSprites(On.LightSource.orig_InitiateSprites orig, LightSource self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				orig(self, sLeaser, rCam);
				if (self is RippleLight) self.shaderDirty = true;
			}

			private static void LightSource_DrawSprites(On.LightSource.orig_DrawSprites orig, LightSource self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
			{
				//bool shaderDirty = self.shaderDirty;
				orig(self, sLeaser, rCam, timeStacker, camPos);
				if (self is RippleLight)
				{
					if (sLeaser.sprites.Length > 1)
					{
						sLeaser.sprites[1].isVisible = false;
					}
					sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders[self.flat ? "FlatLightRippleSideAlt" : "LightSourceRippleSideAlt"];
					sLeaser.sprites[0].isVisible = rCam.rippleData != null && rCam.rippleData.isPassAdded;
				}

			}
		}
	}
}
