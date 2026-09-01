using System.Runtime.CompilerServices;
using EffExt;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RegionKit.Extras.FutileExtras;

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
					.AddBoolField("AffectBKGLightning", true)
					.AddBoolField("AffectGreenSparks", true)
					.AddBoolField("AffectElectricDeath", true)
					.SetUADFactory((room, data, firstTimeRealized) => new RGBElectricDeathUAD(data))
					.SetCategory(_Enums.RegionKit_Decoration.value)
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
		private static readonly ConditionalWeakTable<Room, RGBElectricDeathUAD> uadCWT = new();
		public static RGBElectricDeathUAD? Instance(Room room) => room != null && uadCWT.TryGetValue(room, out RGBElectricDeathUAD result) ? result : null;

		private bool addedToRoom = false;
		public EffectExtraData EffectData { get; }
		public Color color;
		public bool affectBKGLightning;
		public bool affectGreenSparks;
		public bool affectElectricDeath;
		

		public RGBElectricDeathUAD(EffectExtraData effectData)
		{
			EffectData = effectData;
			color = Color.green;
			affectBKGLightning = true;
			affectGreenSparks = true;
			affectElectricDeath = true;
		}

		public override void Update(bool eu)
		{
			if (!addedToRoom && room != null)
			{
				addedToRoom = true;
				if (Instance(room) == null)
				{
					uadCWT.Add(room, this);
				}
			}

			color.r = EffectData.GetFloat("Red") / 255f;
			color.g = EffectData.GetFloat("Green") / 255f;
			color.b = EffectData.GetFloat("Blue") / 255f;

			affectBKGLightning = EffectData.GetBool("AffectBKGLightning");
			affectGreenSparks = EffectData.GetBool("AffectGreenSparks");
			affectElectricDeath = EffectData.GetBool("AffectElectricDeath");
		}

		public override void Destroy()
		{
			if (room != null)
			{
				uadCWT.Remove(room);
			}
			base.Destroy();
		}
	}
	
	public static class RGBElectricDeath
	{
		internal static void Apply()
		{
			On.ElectricDeath.InitiateSprites += ElectricDeathOnInitiateSprites;
			On.ElectricDeath.DrawSprites += ElectricDeath_DrawSprites;
			On.ElectricDeath.SparkFlash.InitiateSprites += SparkFlashOnInitiateSprites;
			IL.ElectricDeath.SparkFlash.Update += SparkFlashOnUpdate;
			On.GreenSparks.GreenSpark.InitiateSprites += GreenSparkOnInitiateSprites;
			On.Lightning.Update += LightningOnUpdate;
		}

		internal static void Undo()
		{
			On.ElectricDeath.InitiateSprites -= ElectricDeathOnInitiateSprites;
			On.ElectricDeath.SparkFlash.InitiateSprites -= SparkFlashOnInitiateSprites;
			IL.ElectricDeath.SparkFlash.Update -= SparkFlashOnUpdate;
			On.GreenSparks.GreenSpark.InitiateSprites -= GreenSparkOnInitiateSprites;
			On.Lightning.Update -= LightningOnUpdate;
		}

		private static void GreenSparkOnInitiateSprites(On.GreenSparks.GreenSpark.orig_InitiateSprites orig, GreenSparks.GreenSpark self, RoomCamera.SpriteLeaser sleaser, RoomCamera rcam)
		{
			if (self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.affectGreenSparks == true)
				self.col = self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ?? self.col;
			orig(self, sleaser, rcam);
		}

		private static void LightningOnUpdate(On.Lightning.orig_Update orig, Lightning self, bool eu)
		{
			orig(self, eu);
			if (self.room.roomSettings.GetEffect(_Enums.RGBElectricDeath) != null && self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.affectBKGLightning == true)
			{
				self.bkgGradient[0] =
					self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ??
					self.bkgGradient[0];
				self.bkgGradient[1] =
					self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ??
					self.bkgGradient[1];
			}
		}

		private static void SparkFlashOnInitiateSprites(On.ElectricDeath.SparkFlash.orig_InitiateSprites orig, ElectricDeath.SparkFlash self, RoomCamera.SpriteLeaser sleaser, RoomCamera rcam)
		{
			orig(self, sleaser, rcam);
				if (self.room.roomSettings.GetEffect(_Enums.RGBElectricDeath) != null && self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.affectElectricDeath == true)
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
				cursor.EmitDelegate((Color origColor, ElectricDeath.SparkFlash self) =>
				{
					return (self.room.roomSettings.GetEffect(_Enums.RGBElectricDeath) != null && self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.affectElectricDeath == true) 
						? self.room.updateList.OfType<RGBElectricDeathUAD>().FirstOrDefault()?.color ?? origColor
						: origColor;
				}); 
			}
		}
		
		private static void ElectricDeathOnInitiateSprites(On.ElectricDeath.orig_InitiateSprites orig, ElectricDeath self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			if (self.room.roomSettings.GetEffect(_Enums.RGBElectricDeath) != null && RGBElectricDeathUAD.Instance(self.room) is { } uad && uad.affectElectricDeath == true)
			{
				FSprite oldSprite = sLeaser.sprites[0];
				oldSprite.RemoveFromContainer();
				sLeaser.sprites[0] = new FSpriteUVs("Futile_White")
				{
					scaleX = oldSprite.scaleX,
					scaleY = oldSprite.scaleY,
					shader = self.room.game.rainWorld.Shaders["RGBElectricDeath"],
				};
				for (int i = 1; i < 10; i++)
				{
					sLeaser.sprites[i].color = uad.color;
				}
			}
			self.AddToContainer(sLeaser, rCam, null);
		}

		private static void ElectricDeath_DrawSprites(On.ElectricDeath.orig_DrawSprites orig, ElectricDeath self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (sLeaser.sprites.Length > 0 && RGBElectricDeathUAD.Instance(self.room) is { } uad)
			{
				if (sLeaser.sprites[0] is not FSpriteUVs sprite)
				{
					FSprite oldSprite = sLeaser.sprites[0];
					sLeaser.sprites[0] = sprite = new FSpriteUVs("Futile_White")
					{
						scaleX = oldSprite.scaleX,
						scaleY = oldSprite.scaleY,
						shader = self.room.game.rainWorld.Shaders["RGBElectricDeath"],
					};
					self.AddToContainer(sLeaser, rCam, null!);
				}
				Color color = uad.affectElectricDeath ? uad.color : Color.green;
				sprite.SetUVs(new Vector2(color.r, color.g), 1);
				sprite.SetUVs(new Vector2(color.b, 1f), 2);
			}
		}
	}
}
