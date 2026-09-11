using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DevInterface;
using EffExt;
using MonoMod.RuntimeDetour;
using RegionKit.Modules.Misc;
using UnityEngine;
using FadePalette = RoomSettings.FadePalette;

namespace RegionKit.Modules.Effects
{
	internal static class PaletteEffectColor
	{
		public static RoomSettings.RoomEffect.Type PaletteEffectColorAorB = new("PaletteEffectColor", true);

		internal static void __RegisterBuilder()
		{
			try
			{
				var builder = new EffectDefinitionBuilder("PaletteEffectColor");
				builder
					.SetCategory(_Enums.RegionKit_Decoration.value)
					.Register();
			}
			catch (Exception ex)
			{
				LogWarning($"Error on eff PaletteEffectColorA init {ex}");
			}
		}

		private static Dictionary<int, PresentPalEffects> PaletteEffectDict { get; } = [];
		private struct PresentPalEffects(bool a, bool b)
		{
			public bool EffA = a;
			public bool EffB = b;
			public readonly bool AnyEff { get => EffA || EffB; }
		}

		private static bool AnyPaletteHasEffectColors(RoomCamera rS)
		{
			FadePalette[]? moreFades = rS.room?.roomSettings?.GetAllFades();
			return rS != null && (PaletteHasEffectColor(rS.paletteA)
				|| rS.paletteB != -1 && PaletteHasEffectColor(rS.paletteB)
				|| moreFades?.Length > 0 && moreFades.Any(x => PaletteHasEffectColor(x.palette)));
		}

		// Check if the Dictionary has the palette, or if it needs to be added
		private static bool PaletteHasEffectColor(int pal)
		{
			return PaletteEffectDict.TryGetValue(pal, out PresentPalEffects hasEffects) && (hasEffects.EffA || hasEffects.EffB) // Return value if it already exists in dictionary
				|| ReadPaletteImageForEffectColors(pal); // Else, read the texture
		}

		private static bool ReadPaletteImageForEffectColors(int pal)
		{
			// Copied from RoomCamera.LoadPalette just to be able to read the raw files instead of assuming the level texture is correct
			Texture2D texture = null!;
			ReloadPaletteTexture(pal, ref texture);

			PaletteEffectDict[pal] = new(AnyPaletteTexturePixelsNotWhite(GetEffectColorPixels(texture, true)), AnyPaletteTexturePixelsNotWhite(GetEffectColorPixels(texture, false)));
			if (PaletteEffectDict[pal].AnyEff)
				UnityEngine.Debug.Log($"{pal} has written effect colors!");

			// Dispose of the temporary texture after we're done
			UnityEngine.Object.Destroy(texture);

			return PaletteEffectDict[pal].AnyEff;
		}

		// The LoadPalette method without applying Effect Colors
		private static void ReloadPaletteTexture(int pal, ref Texture2D texture)
		{
			if (texture != null)
			{
				UnityEngine.Object.Destroy(texture);
			}
			texture = new Texture2D(32, 16, TextureFormat.ARGB32, mipChain: false);
			string text = AssetManager.ResolveFilePath("palettes" + System.IO.Path.DirectorySeparatorChar + "palette" + pal.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png");
			try
			{
				AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + text, clampWrapMode: false, crispPixels: true);
			}
			catch (System.IO.FileLoadException)
			{
				text = AssetManager.ResolveFilePath("palettes" + System.IO.Path.DirectorySeparatorChar + "palette-1.png");
				AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + text, clampWrapMode: false, crispPixels: true);
			}
			texture.Apply(updateMipmaps: false);
		}

		private static bool AnyPaletteTexturePixelsNotWhite(Color[]? effectColors)
		{
			return !effectColors?.All(col => col == effectColors.FirstOrDefault()) ?? false;
		}

		private static Color[]? GetEffectColorPixels(Texture2D texture, bool effA)
		{
			if (texture == null) return null;

			// Read the effect color pixels
			var finalColors = new Color[8];
			int index = 0;
			for (int sun = 0; sun < 2; sun++)
			{
				for (int x = 0; x < 2; x++)
				{
					for (int y = 0; y < 2; y++)
					{
						int sunOffset = sun == 0 ? 2 : 10;
						int effectTypeOffset = effA ? 0 : 2;
						finalColors[index++] = texture.GetPixel(30 + x, sunOffset + effectTypeOffset + y);
					}
				}
			}

			return finalColors;
		}

		private static int NumOfEffectColors { get => (int)Math.Floor((RoomCamera.allEffectColorsTexture?.width ?? 40) / 2.0) - 1; }

		public static void Apply()
		{
			On.RoomCamera.LoadPalette += RoomCamera_LoadPalette;
			On.RoomCamera.ApplyEffectColorsToAllPaletteTextures += RoomCamera_ApplyEffectColorsToAllPaletteTextures;
			On.RoomCamera.ApplyEffectColorsToPaletteTexture += ApplyEffectColorsHook;
			On.RoomCamera.ApplyFade += RoomCamera_ApplyFade;
		}

		internal static void Undo()
		{
			On.RoomCamera.LoadPalette -= RoomCamera_LoadPalette;
			On.RoomCamera.ApplyEffectColorsToAllPaletteTextures -= RoomCamera_ApplyEffectColorsToAllPaletteTextures;
			On.RoomCamera.ApplyEffectColorsToPaletteTexture -= ApplyEffectColorsHook;
			On.RoomCamera.ApplyFade -= RoomCamera_ApplyFade;
		}

		// Reload our image data for effect colors if necessary
		private static void RoomCamera_LoadPalette(On.RoomCamera.orig_LoadPalette orig, RoomCamera self, int pal, ref Texture2D texture)
		{
			orig(self, pal, ref texture);

			if (PaletteEffectDict.ContainsKey(pal))
			{
				// For reloading image data if needed
				ReadPaletteImageForEffectColors(pal);
			}
		}

		private static void RoomCamera_ApplyEffectColorsToAllPaletteTextures(On.RoomCamera.orig_ApplyEffectColorsToAllPaletteTextures orig, RoomCamera self, int color1, int color2)
		{
			// Reload the room palette if the effect color effect is present so we can reinitialize our palette effects
			if (self.room?.roomSettings != null)
			{
				if (self.room.roomSettings.GetEffect(PaletteEffectColorAorB) != null)
				{
					ReloadPaletteTexture(self.room.roomSettings.Palette, ref self.fadeTexA);
					if (self.room?.roomSettings.fadePalette != null)
					{
						ReloadPaletteTexture(self.room.roomSettings.fadePalette.palette, ref self.fadeTexB);
					}

					// Reload more fade textures
					self.ClearMoreFadeTextures();
					foreach (FadePalette fade in self.MoreFadeTextures().Keys)
					{
						Texture2D moreTex = null!;
						ReloadPaletteTexture(fade.palette, ref moreTex);
						self.MoreFadeTextures()[fade] = moreTex;
					}
				}
			}

			orig(self, color1, color2);
		}

		private static void ApplyEffectColorsHook(On.RoomCamera.orig_ApplyEffectColorsToPaletteTexture orig, RoomCamera self, ref Texture2D texture, int color1, int color2)
		{
			// Fix for effect color crash
			int colorCount = NumOfEffectColors;
			if (color1 > colorCount)
			{
				color1 = -1;
			}
			if (color2 > colorCount)
			{
				color2 = -1;
			}

			if (self.room?.roomSettings?.GetEffect(PaletteEffectColorAorB) != null && AnyPaletteHasEffectColors(self))
			{
				return;
			}

			orig(self, ref texture, color1, color2);
		}

		private static void RoomCamera_ApplyFade(On.RoomCamera.orig_ApplyFade orig, RoomCamera self)
		{
			orig(self);

			// Since our effect colors weren't written over, we should be able to calculate their actual values here
			if (self.paletteTexture != null && self.room?.roomSettings != null && self.room.roomSettings.GetEffect(PaletteEffectColorAorB) != null && AnyPaletteHasEffectColors(self))
			{
				List<(int pal, Texture2D tex, float fade)> allEffectsToFade = [];
				if (PaletteHasEffectColor(self.room.roomSettings.Palette))
				{
					allEffectsToFade.Add((self.room.roomSettings.Palette, self.fadeTexA, self.room.roomSettings.GetEffectAmount(PaletteEffectColorAorB)));
				}
				if (self.room.roomSettings.fadePalette != null && PaletteHasEffectColor(self.room.roomSettings.fadePalette.palette) && self.room.roomSettings.fadePalette.fades.Length > self.currentCameraPosition)
				{
					allEffectsToFade.Add((self.room.roomSettings.fadePalette.palette, self.fadeTexB, self.room.roomSettings.fadePalette.fades[self.currentCameraPosition]));
				}
				foreach (var fade in self.room.roomSettings.GetAllFades())
				{
					if (fade != null && PaletteHasEffectColor(fade.palette) && fade.fades.Length > self.currentCameraPosition)
					{
						allEffectsToFade.Add((fade.palette, self.GetMoreFadeTexture(fade), fade.fades[self.currentCameraPosition]));
					}
				}

				if (allEffectsToFade.Count > 0)
				{
					bool textureDirty = false;
					if (self.room.roomSettings.EffectColorA > -1)
					{
						(Color[] colors, float fade)[] effectColACols = [.. from eff in allEffectsToFade where PaletteEffectDict.TryGetValue(eff.pal, out var effInfo) && effInfo.EffA select (GetEffectColorPixels(eff.tex, true), eff.fade)];

						FadeEffectColors(self, effectColACols, true);
						textureDirty = true;
					}
					if (self.room.roomSettings.EffectColorB > -1)
					{
						(Color[] colors, float fade)[] effectColBCols = [.. from eff in allEffectsToFade where PaletteEffectDict.TryGetValue(eff.pal, out var effInfo) && effInfo.EffB select (GetEffectColorPixels(eff.tex, false), eff.fade)];

						FadeEffectColors(self, effectColBCols, false);
						textureDirty = true;
					}

					if (textureDirty)
						self.paletteTexture.Apply(updateMipmaps: false);
				}
			}
		}

		private static void FadeEffectColors(RoomCamera self, (Color[] colors, float fade)[] effectColACols, bool effectA)
		{
			int yOffset = effectA ? 0 : 2;
			Color[] palCols = [Color.white, Color.white, Color.white, Color.white];
			Color[] rainCols = [Color.white, Color.white, Color.white, Color.white];
			if (self.room?.roomSettings != null)
			{
				if (effectA && self.room.roomSettings.EffectColorA > -1)
				{
					palCols = self.ModifyEffectColorA(RoomCamera.allEffectColorsTexture.GetPixels(self.room.roomSettings.EffectColorA * 2, 0, 2, 2, 0));
					rainCols = self.ModifyEffectColorA(RoomCamera.allEffectColorsTexture.GetPixels(self.room.roomSettings.EffectColorA * 2, 2, 2, 2, 0));
				}
				else if (!effectA && self.room.roomSettings.EffectColorB > -1)
				{
					palCols = self.ModifyEffectColorB(RoomCamera.allEffectColorsTexture.GetPixels(self.room.roomSettings.EffectColorB * 2, 0, 2, 2, 0));
					rainCols = self.ModifyEffectColorB(RoomCamera.allEffectColorsTexture.GetPixels(self.room.roomSettings.EffectColorB * 2, 2, 2, 2, 0));
				}
			}

			foreach ((Color[] colors, float fade) in effectColACols)
			{
				if (fade <= 0f) continue;
				for (int i = 0; i < 4; i++)
				{
					palCols[i] = Color.Lerp(palCols[i], colors[i], fade);
					rainCols[i] = Color.Lerp(rainCols[i], colors[i + 4], fade);
				}
			}
			// Then lerp with the rain
			for (int i = 0; i < 4; i++)
			{
				palCols[i] = Color.Lerp(palCols[i], rainCols[i], self.fadeCoord.y);
			}
			self.paletteTexture.SetPixels(30, 2 + yOffset, 2, 2, palCols, 0);
			self.paletteTexture.SetPixels(30, 10 + yOffset, 2, 2, palCols, 0);
		}
	}
}
