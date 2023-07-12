using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Diagnostics;

namespace RegionKit.Modules.Effects
{
	internal static class RoomRainWithoutDeathRain
	{
		public static void Apply()
		{
			_CommonHooks.PostRoomLoad += _CommonHooks_PostRoomLoad;
			On.RoomRain.Update += RoomRain_Update;
			On.RoomRain.ThrowAroundObjects += RoomRain_ThrowAroundObjects;
		}


		public static void Undo()
		{
			_CommonHooks.PostRoomLoad -= _CommonHooks_PostRoomLoad;
			On.RoomRain.Update -= RoomRain_Update;
			On.RoomRain.ThrowAroundObjects -= RoomRain_ThrowAroundObjects;
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
			if (obj.roomRain == null)
			{
				Debug.Log("room rain is null");
				RoomSettings.RoomEffect.Type[] rainTypes = {
					RoomSettings.RoomEffect.Type.LightRain,
					RoomSettings.RoomEffect.Type.HeavyRain,
					RoomSettings.RoomEffect.Type.HeavyRainFlux,
					RoomSettings.RoomEffect.Type.BulletRain,
					RoomSettings.RoomEffect.Type.BulletRainFlux
				};
				foreach (RoomSettings.RoomEffect effect in obj.roomSettings.effects)
				{
					Debug.Log($"effect type is [{effect.type.value}]");
					if (rainTypes.Contains(effect.type))
					{
						Debug.Log($"effect [{effect.type.value}] un-nulls rain");
						obj.roomRain = new RoomRain(obj.game.globalRain, obj);
						obj.AddObject(obj.roomRain);
						break;
					}
				}
			}
		}
	}
}
