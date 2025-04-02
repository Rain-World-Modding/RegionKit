using System;
using System.Linq;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Effects
{
	internal static class RoomRainWithoutDeathRain
	{
		public static void Apply()
		{
			_CommonHooks.PostRoomLoad += _CommonHooks_PostRoomLoad;
			On.RoomRain.Update += RoomRain_Update;
			On.RoomRain.ThrowAroundObjects += RoomRain_ThrowAroundObjects;
			On.Water.DetailedWaterLevel += Water_DetailedWaterLevel;
			IL.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate1;
			On.RegionGateGraphics.AddToContainer += RegionGateGraphics_AddToContainer;
			On.VirtualMicrophone.Update += VirtualMicrophone_Update;
		}

		public static void Undo()
		{
			_CommonHooks.PostRoomLoad -= _CommonHooks_PostRoomLoad;
			On.RoomRain.Update -= RoomRain_Update;
			On.RoomRain.ThrowAroundObjects -= RoomRain_ThrowAroundObjects;
			On.Water.DetailedWaterLevel -= Water_DetailedWaterLevel;
			IL.RoomCamera.DrawUpdate -= RoomCamera_DrawUpdate1;
			On.RegionGateGraphics.AddToContainer -= RegionGateGraphics_AddToContainer;
			On.VirtualMicrophone.Update -= VirtualMicrophone_Update;
		}

		private static void VirtualMicrophone_Update(On.VirtualMicrophone.orig_Update orig, VirtualMicrophone self)
		{
			orig(self);
			if (float.IsInfinity(self.room.FloatWaterLevel(self.listenerPoint)))
			{
				self.underWater = 1f;
			}
		}

		private static void RegionGateGraphics_AddToContainer(On.RegionGateGraphics.orig_AddToContainer orig, RegionGateGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			if (self.gate.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.AboveCloudsView) != null)
			{
				if (newContatiner == null || newContatiner == rCam.ReturnFContainer("Water"))
				{
					newContatiner = rCam.ReturnFContainer("GrabShaders");
				}
			}
			orig(self, sLeaser, rCam, newContatiner);
		}

		private static void RoomCamera_DrawUpdate1(ILContext il)
		{
			var c = new ILCursor(il);
			if (c.TryGotoNext(MoveType.After,
				x => x.MatchLdarg(0),
				x => x.MatchCall<RoomCamera>("get_room"),
				x => x.MatchCallvirt<Room>("get_abstractRoom"),
				x => x.MatchCallvirt<AbstractRoom>("get_gate")
				))
			{
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate((bool orig, RoomCamera self) => { return orig && self.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.Fog) == 0; });
			}
			else { LogError("Failed to ilhook RoomCamera.DrawUpdate pt 1"); }

			if (c.TryGotoNext(MoveType.After,
				x => x.MatchLdarg(0),
				x => x.MatchCall<RoomCamera>("get_room"),
				x => x.MatchCallvirt<Room>("get_abstractRoom"),
				x => x.MatchCallvirt<AbstractRoom>("get_shelter")
				))
			{
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate((bool orig, RoomCamera self) => { return orig && self.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.Fog) == 0; });
			}
			else { LogError("Failed to ilhook RoomCamera.DrawUpdate pt 2"); }
		}

		private static float Water_DetailedWaterLevel(On.Water.orig_DetailedWaterLevel orig, Water self, Vector2 position)
		{
			float o = orig(self, position);
			if (self.room != null && self.room.PixelHeight + 500f < o - 20f)
			{ return float.PositiveInfinity; }

			return o;
		}

		private static void RoomRain_ThrowAroundObjects(On.RoomRain.orig_ThrowAroundObjects orig, RoomRain self)
		{
			if (!rainDangers.Contains(self.dangerType))
			{ return; }
			orig(self);
		}

		public static RoomRain.DangerType[] rainDangers = {
				RoomRain.DangerType.Rain,
				RoomRain.DangerType.FloodAndRain,
				RoomRain.DangerType.AerieBlizzard
			};

		private static void RoomRain_Update(On.RoomRain.orig_Update orig, RoomRain self, bool eu)
		{
			orig(self, eu);

			if (!rainDangers.Contains(self.dangerType))
			{
				self.intensity = DevtoolsIntensity(self.room.roomSettings, self.globalRain);
			}
		}

		private static float DevtoolsIntensity(RoomSettings roomSettings, GlobalRain globalRain)
		{
			float intensity = 0f;

			float lightRain = roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.LightRain);
			float heavyRain = roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.HeavyRain);
			float heavyRainFlux = roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.HeavyRainFlux);
			if (globalRain != null)
			{
				if (heavyRainFlux > 0f)
				{
					float num9 = 1200f * heavyRainFlux;
					float num10 = 60f;

					globalRain.heavyTimer = (globalRain.heavyTimer + 1) % (int)(num10 * 2f + num9 * 2f);
					if (globalRain.heavyTimer < num10)
					{ heavyRain *= globalRain.heavyTimer / num10; }

					else if (globalRain.heavyTimer >= num10 + num9 && globalRain.heavyTimer < num10 * 2f + num9)
					{ heavyRain *= 1f - (globalRain.heavyTimer - (num9 + num10)) / num10; }

					else if (globalRain.heavyTimer >= num10 * 2f + num9)
					{ heavyRain = 0f; }
				}
				if (heavyRain > 0f)
				{
					intensity = (1f + heavyRain * 4f) * 0.24f;
					globalRain.RumbleSound = heavyRain * 0.2f;
					globalRain.ScreenShake = heavyRain;
				}

				else if (lightRain > 0f)
				{ intensity = lightRain * 0.24f; }
			}

			return intensity;
		}

		private static void _CommonHooks_PostRoomLoad(Room obj)
		{
			if (obj.game == null) return;
			if (obj.roomRain == null)
			{
				RoomSettings.RoomEffect.Type[] rainTypes = {
					RoomSettings.RoomEffect.Type.LightRain,
					RoomSettings.RoomEffect.Type.HeavyRain,
					RoomSettings.RoomEffect.Type.HeavyRainFlux,
					RoomSettings.RoomEffect.Type.BulletRain,
					RoomSettings.RoomEffect.Type.BulletRainFlux
				};
				foreach (RoomSettings.RoomEffect effect in obj.roomSettings.effects)
				{
					if (rainTypes.Contains(effect.type))
					{
						obj.roomRain = new RoomRain(obj.game.globalRain, obj);
						obj.AddObject(obj.roomRain);
						break;
					}
				}
			}
		}
	}
}
