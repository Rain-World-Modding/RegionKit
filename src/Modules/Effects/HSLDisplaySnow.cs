using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EffExt;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.CoralBrain;
using On.MoreSlugcats;
using RegionKit.Modules.Objects;

namespace RegionKit.Modules.Effects
{
	// By ASlightlyOvergrownCactus
	// Called before MossWaterRGB's Load Resources
	internal static class HSLDisplaySnowBuilder
	{
		internal static void __RegisterBuilder()
		{
			try
			{
				EffectDefinitionBuilder builder = new EffectDefinitionBuilder("HSLDisplaySnow");
				builder
					.AddFloatField("Luminosity", 0, 100, 1, 20)
					.AddFloatField("Saturation", 0, 100, 1, 0)
					.AddFloatField("Hue", 0, 360, 1, 180)
					.AddBoolField("AffectSnowfall", true)
					.SetUADFactory((room, data, firstTimeRealized) => new HSLDisplaySnowUAD(data))
					.SetCategory("RegionKit")
					.Register();
			}
			catch (Exception ex)
			{
				LogWarning($"Error on eff HSLDisplaySnow init {ex}");
			}
		}
	}
	
	internal class HSLDisplaySnowUAD : UpdatableAndDeletable
	{
		public EffectExtraData EffectData { get; }
		public HSLColor color;
		public HSLDisplaySnow DisplaySnowHSL;
		public bool affectSnowfall;
		

		public HSLDisplaySnowUAD(EffectExtraData effectData)
		{
			EffectData = effectData;
			Vector3 temp = RGB2HSL(Color.white);
			color = new HSLColor(temp.x, temp.y, temp.z);
			affectSnowfall = true;
			DisplaySnowHSL = new HSLDisplaySnow();
		}

		public override void Update(bool eu)
		{
			color.hue = EffectData.GetFloat("Hue") / 360f;
			color.saturation = EffectData.GetFloat("Saturation") / 100f;
			color.lightness = EffectData.GetFloat("Luminosity") / 100f;
			affectSnowfall = EffectData.GetBool("AffectSnowfall");

			if (DisplaySnowHSL != null && room.BeingViewed)
			{
				Shader.SetGlobalColor("_InputColorDispSnow", HSL2RGB(color.hue, color.saturation, color.lightness));
				Shader.SetGlobalFloat("_InputRGBSnowAmount", room.roomSettings.GetEffectAmount(_Enums.HSLDisplaySnow));
			}
		}
	}

	public class HSLDisplaySnow
	{
		static bool loaded = false;
		
		public static void RDSLoadResources(RainWorld rw)
		{
			if (!loaded)
			{
				loaded = true;
				if (MossWaterUnlit.mossBundle != null)
				{
					rw.Shaders["RGBDisplaySnow"] = FShader.CreateShader("RGBDisplaySnow", MossWaterUnlit.mossBundle.LoadAsset<Shader>("Assets/shaders 1.9.03/RGBSnow.shader"));
					rw.Shaders["RGBSnowfall"] = FShader.CreateShader("RGBSnowfall", MossWaterUnlit.mossBundle.LoadAsset<Shader>("Assets/shaders 1.9.03/RGBSnowfall.shader"));
					rw.Shaders["RGBBlizzard"] = FShader.CreateShader("RGBBlizzard", MossWaterUnlit.mossBundle.LoadAsset<Shader>("Assets/shaders 1.9.03/RGBBlizzard.shader"));
				}
				else
				{
					LogMessage("HSLDisplaySnow must be loaded after MossWaterUnlit!");
				}
			}
		}

		internal static void Apply()
		{
			if (ModManager.MSC)
			{
				On.MoreSlugcats.Snow.InitiateSprites += SnowOnInitiateSprites;
				On.MoreSlugcats.BlizzardGraphics.DrawSprites += BlizzardGraphicsOnDrawSprites;
			}
		}

		internal static void Undo()
		{
			if (ModManager.MSC)
			{
				On.MoreSlugcats.Snow.InitiateSprites -= SnowOnInitiateSprites;
				On.MoreSlugcats.BlizzardGraphics.DrawSprites -= BlizzardGraphicsOnDrawSprites;
			}
		}
		
		private static void BlizzardGraphicsOnDrawSprites(BlizzardGraphics.orig_DrawSprites orig, MoreSlugcats.BlizzardGraphics self, RoomCamera.SpriteLeaser sleaser, RoomCamera rcam, float timestacker, Vector2 campos)
		{
			orig(self, sleaser, rcam, timestacker, campos);
			
				if (self.room != null && self.room.roomSettings.GetEffect(_Enums.HSLDisplaySnow) != null && (self.room.roomSettings.DangerType == MoreSlugcats.MoreSlugcatsEnums.RoomRainDangerType.Blizzard || self.room.roomSettings.DangerType == RoomRain.DangerType.AerieBlizzard))
				{
					if (self.room.updateList.OfType<HSLDisplaySnowUAD>().FirstOrDefault()?.affectSnowfall == true)
					{
						sleaser.sprites[0].shader = rcam.room.game.rainWorld.Shaders["RGBSnowfall"];
						sleaser.sprites[1].shader = rcam.room.game.rainWorld.Shaders["RGBBlizzard"];
					}
					else
					{
						sleaser.sprites[0].shader = rcam.room.game.rainWorld.Shaders["SnowFall"];
						sleaser.sprites[1].shader = rcam.room.game.rainWorld.Shaders["Blizzard"];
					}
				}
		}
		
		private static void SnowOnInitiateSprites(Snow.orig_InitiateSprites orig, MoreSlugcats.Snow self, RoomCamera.SpriteLeaser sleaser, RoomCamera rcam)
		{
			orig(self, sleaser, rcam);
			if (self.room.roomSettings.GetEffect(_Enums.HSLDisplaySnow) != null)
			{
				sleaser.sprites[0].shader = rcam.room.game.rainWorld.Shaders["RGBDisplaySnow"];
			}
		}
	}
}
