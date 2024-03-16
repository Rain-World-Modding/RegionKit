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
	internal static class RGBElectricDeathBuilder
	{
		internal static void __RegisterBuilder()
		{
			try
			{
				EffectDefinitionBuilder builder = new EffectDefinitionBuilder("RGBElectricDeath");
				builder
					.AddFloatField("Blue", 0, 255, 1, 45)
					.AddFloatField("Green", 0, 255, 1, 3)
					.AddFloatField("Red", 0, 255, 1, 153)
					.SetUADFactory((room, data, firstTimeRealized) => new RGBElectricDeathUAD(data))
					.SetCategory("RegionKit")
					.Register();
			}
			catch (Exception ex)
			{
				LogWarning($"Error on eff MossWaterRGB init {ex}");
			}
		}
	}
	
	internal class RGBElectricDeathUAD : UpdatableAndDeletable
	{
		public EffectExtraData EffectData { get; }
		public Color color;
		public RGBElectricDeath electricDeathRGB;
		

		public RGBElectricDeathUAD(EffectExtraData effectData)
		{
			EffectData = effectData;
			color = Color.green;
			electricDeathRGB = new RGBElectricDeath();
			
		}

		public override void Update(bool eu)
		{
			color.r = EffectData.GetFloat("Red") / 255f;
			color.g = EffectData.GetFloat("Green") / 255f;
			color.b = EffectData.GetFloat("Blue") / 255f;

			if (electricDeathRGB != null && room.BeingViewed)
			{
				Shader.SetGlobalColor("_InputColorElecDeath", color);
			}
		}
	}
	
	public class RGBElectricDeath
	{
		static bool loaded = false;
		
		public static void REDLoadResources(RainWorld rw)
		{
			if (!loaded)
			{
				loaded = true;
				if (MossWaterUnlit.mossBundle != null)
				{
					rw.Shaders["RGBElectricDeath"] = FShader.CreateShader("RGBElectricDeath", MossWaterUnlit.mossBundle.LoadAsset<Shader>("Assets/shaders 1.9.03/RGBElectricDeath.shader"));
				}
				else
				{
					LogMessage("RGBElectricDeath must be loaded after MossWaterUnlit!");
				}
			}
		}

		internal static void Apply()
		{
			On.ElectricDeath.InitiateSprites += ElectricDeathOnInitiateSprites;
			On.ElectricDeath.SparkFlash.InitiateSprites += SparkFlashOnInitiateSprites;
			IL.ElectricDeath.SparkFlash.Update += SparkFlashOnUpdate;
			On.GreenSparks.GreenSpark.ctor += GreenSparkOnctor;
			On.GreenSparks.GreenSpark.InitiateSprites += GreenSparkOnInitiateSprites;
			On.Lightning.Update += LightningOnUpdate;
		}

		private static void GreenSparkOnInitiateSprites(On.GreenSparks.GreenSpark.orig_InitiateSprites orig, GreenSparks.GreenSpark self, RoomCamera.SpriteLeaser sleaser, RoomCamera rcam)
		{
			self.col = self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ?? self.col;
			orig(self, sleaser, rcam);
		}

		internal static void Undo()
		{
			On.ElectricDeath.InitiateSprites -= ElectricDeathOnInitiateSprites;
			On.ElectricDeath.SparkFlash.InitiateSprites -= SparkFlashOnInitiateSprites;
			IL.ElectricDeath.SparkFlash.Update -= SparkFlashOnUpdate;
			On.GreenSparks.GreenSpark.ctor -= GreenSparkOnctor;
			On.Lightning.Update -= LightningOnUpdate;
		}

		private static void LightningOnUpdate(On.Lightning.orig_Update orig, Lightning self, bool eu)
		{
			orig(self, eu);
			if (self.room.roomSettings.GetEffect(_Enums.RGBElectricDeath) != null)
			{
				self.bkgGradient[0] =
					self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ??
					self.bkgGradient[0];
				self.bkgGradient[1] =
					self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ??
					self.bkgGradient[1];
			}
		}

		private static void GreenSparkOnctor(On.GreenSparks.GreenSpark.orig_ctor orig, GreenSparks.GreenSpark self, Vector2 pos)
		{
			orig(self, pos);
		}

		private static void SparkFlashOnInitiateSprites(On.ElectricDeath.SparkFlash.orig_InitiateSprites orig, ElectricDeath.SparkFlash self, RoomCamera.SpriteLeaser sleaser, RoomCamera rcam)
		{
			orig(self, sleaser, rcam);
				if (self.room.roomSettings.GetEffect(_Enums.RGBElectricDeath) != null)
				{
					sleaser.sprites[0].color =
						self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ??
						sleaser.sprites[0].color;
					sleaser.sprites[1].color =
						self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ??
						sleaser.sprites[1].color;
					sleaser.sprites[2].color =
						self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ??
						sleaser.sprites[2].color;
				}
		}
		
		private static void SparkFlashOnUpdate(ILContext il)
		{
			ILCursor cursor = new ILCursor(il).Goto(0);

			if (cursor.TryGotoNext(MoveType.After, i => i.MatchNewobj<Color>())) 
			{
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.EmitDelegate<Func<Color, ElectricDeath.SparkFlash, Color>>((Color origColor, ElectricDeath.SparkFlash self) => (Color)((self.room.roomSettings.GetEffect(_Enums.RGBElectricDeath) != null) ? self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color : origColor)); 
			}
		}
		
		private static void ElectricDeathOnInitiateSprites(On.ElectricDeath.orig_InitiateSprites orig, ElectricDeath self, RoomCamera.SpriteLeaser sleaser, RoomCamera rcam)
		{
			orig(self, sleaser, rcam);
			if (self.room.roomSettings.GetEffect(_Enums.RGBElectricDeath) != null)
			{
				sleaser.sprites[0].shader = self.room.game.rainWorld.Shaders["RGBElectricDeath"];
				for (int i = 1; i < 10; i++)
				{
					sleaser.sprites[i].color = self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ?? sleaser.sprites[i].color;
				}
			}
		}
	}
}
