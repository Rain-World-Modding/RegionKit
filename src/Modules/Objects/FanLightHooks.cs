using System.IO;
using System.Runtime.CompilerServices;

namespace RegionKit.Modules.Objects;

public static class FanLightHooks
{
	public static ConditionalWeakTable<LightSource, float[]> data = new();
	public static LinearBlur blur = new();

	internal static void Apply()
	{
		//please don't delete or change the shape of the additional alpha/speed light data
		On.RainWorld.PostModsInit += RainWorld_PostModsInit;
		On.RainWorld.UnloadResources += RainWorld_UnloadResources;
		On.LightSource.ctor_Vector2_bool_Color_UpdatableAndDeletable += LightSource_ctor;
		On.LightSource.Update += LightSource_Update;
		On.LightSource.DrawSprites += LightSource_DrawSprites;
	}

	internal static void Undo()
	{
		On.RainWorld.PostModsInit -= RainWorld_PostModsInit;
		On.RainWorld.UnloadResources -= RainWorld_UnloadResources;
		On.LightSource.ctor_Vector2_bool_Color_UpdatableAndDeletable -= LightSource_ctor;
		On.LightSource.Update -= LightSource_Update;
		On.LightSource.DrawSprites -= LightSource_DrawSprites;
	}

	private static void LightSource_DrawSprites(On.LightSource.orig_DrawSprites orig, LightSource self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		orig(self, sLeaser, rCam, timeStacker, camPos);
		if (self is FanLightObject.FanLightLight && data.TryGetValue(self, out var r))
		{
			FSprite[] sprs = sLeaser.sprites;
			for (var i = 0; i < sprs.Length; i++)
				sprs[i].rotation = Mathf.Lerp(r[1], r[2], timeStacker);
		}
	}

	private static void LightSource_Update(On.LightSource.orig_Update orig, LightSource self, bool eu)
	{
		orig(self, eu);
		if (self.room is not null && !self.slatedForDeletetion && data.TryGetValue(self, out var d))
		{
			if (self is FanLightObject.FanLightLight fanLight)
			{
				d[1] = d[2];
				d[2] += fanLight.speed / 10f - fanLight.inverseSpeed / 10f;
			}
		}
	}

	private static void LightSource_ctor(On.LightSource.orig_ctor_Vector2_bool_Color_UpdatableAndDeletable orig, LightSource self, Vector2 initPos, bool environmentalLight, Color color, UpdatableAndDeletable tiedToObject)
	{
		orig(self, initPos, environmentalLight, color, tiedToObject);
		if (self.tiedToObject is FanLightObject)
			data.Add(self, new[] { 1f, 0f, 0f });
	}

	private static void RainWorld_UnloadResources(On.RainWorld.orig_UnloadResources orig, RainWorld self)
	{
		orig(self);
		for (var i = 0; i < Futile.atlasManager._atlases.Count; i++)
		{
			var atl = Futile.atlasManager._atlases[i].name;
			if (atl.StartsWith("FanLightMask"))
				Futile.atlasManager.UnloadAtlas(atl);
		}
	}

	private static void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
	{
		orig(self);
		List<ModManager.Mod> enabledMods = ModManager.ActiveMods;
		for (int k = 0; k < enabledMods.Count; k++)
		{
			string directory = Path.Combine(enabledMods[k].path, "fanmasks");
			if (Directory.Exists(directory))
			{
				var files = Directory.GetFiles(directory);
				for (var i = 0; i < files.Length; i++)
				{
					var fn = Path.GetFileNameWithoutExtension(files[i]);
					if (!Futile.atlasManager.DoesContainAtlas(fn))
					{
						FAtlas atlas = Futile.atlasManager.LoadFanImage(Path.Combine("fanmasks", fn));
						atlas._texture = blur.Blur((atlas._texture as Texture2D)!, 5, 1);
					}
				}
			}
		}
	}

	internal static void Dispose()
	{
		blur = null!;
		data = null!;
	}

	public static FAtlas LoadFanImage(this FAtlasManager self, string imagePath) => self.DoesContainAtlas(imagePath) ? self.GetAtlasWithName(imagePath) : self.ActuallyLoadAtlasOrImage(imagePath.Split('\\')[1], imagePath + Futile.resourceSuffix, "");

	public class LinearBlur
	{
		float _rSum;
		float _gSum;
		float _bSum;
		Texture2D? _sourceImage;
		int _sourceWidth;
		int _sourceHeight;
		int _windowSize;

		public Texture2D Blur(Texture2D image, int radius, int iterations)
		{
			_windowSize = radius * 2 + 1;
			_sourceWidth = image.width;
			_sourceHeight = image.height;
			Texture2D tex = image;
			for (var i = 0; i < iterations; i++)
			{
				tex = OneDimensialBlur(tex, radius, true);
				tex = OneDimensialBlur(tex, radius, false);
			}
			return tex;
		}

		Texture2D OneDimensialBlur(Texture2D image, int radius, bool horizontal)
		{
			_sourceImage = image;
			var blurred = new Texture2D(image.width, image.height, image.format, false);
			if (horizontal)
			{
				for (var imgY = 0; imgY < _sourceHeight; ++imgY)
				{
					ResetSum();
					for (var imgX = 0; imgX < _sourceWidth; imgX++)
					{
						if (imgX == 0)
						{
							for (var x = radius * -1; x <= radius; ++x)
								AddPixel(GetPixelWithXCheck(x, imgY));
						}
						else
						{
							Color toExclude = GetPixelWithXCheck(imgX - radius - 1, imgY);
							Color toInclude = GetPixelWithXCheck(imgX + radius, imgY);
							SubstractPixel(toExclude);
							AddPixel(toInclude);
						}
						blurred.SetPixel(imgX, imgY, CalcPixelFromSum());
					}
				}
			}
			else
			{
				for (var imgX = 0; imgX < _sourceWidth; imgX++)
				{
					ResetSum();
					for (var imgY = 0; imgY < _sourceHeight; ++imgY)
					{
						if (imgY == 0)
						{
							for (var y = radius * -1; y <= radius; ++y)
								AddPixel(GetPixelWithYCheck(imgX, y));
						}
						else
						{
							Color toExclude = GetPixelWithYCheck(imgX, imgY - radius - 1);
							Color toInclude = GetPixelWithYCheck(imgX, imgY + radius);
							SubstractPixel(toExclude);
							AddPixel(toInclude);
						}
						blurred.SetPixel(imgX, imgY, CalcPixelFromSum());
					}
				}
			}
			blurred.Apply();
			return blurred;
		}

		Color GetPixelWithXCheck(int x, int y)
		{
			if (_sourceImage is not Texture2D text)
				return default;
			if (x <= 0)
				return text.GetPixel(0, y);
			if (x >= _sourceWidth)
				return text.GetPixel(_sourceWidth - 1, y);
			return text.GetPixel(x, y);
		}

		Color GetPixelWithYCheck(int x, int y)
		{
			if (_sourceImage is not Texture2D text)
				return default;
			if (y <= 0)
				return text.GetPixel(x, 0);
			if (y >= _sourceHeight)
				return text.GetPixel(x, _sourceHeight - 1);
			return text.GetPixel(x, y);
		}

		void AddPixel(Color pixel)
		{
			_rSum += pixel.r;
			_gSum += pixel.g;
			_bSum += pixel.b;
		}

		void SubstractPixel(Color pixel)
		{
			_rSum -= pixel.r;
			_gSum -= pixel.g;
			_bSum -= pixel.b;
		}

		void ResetSum()
		{
			_rSum = 0f;
			_gSum = 0f;
			_bSum = 0f;
		}

		Color CalcPixelFromSum() => new(_rSum / _windowSize, _gSum / _windowSize, _bSum / _windowSize);
	}
}
