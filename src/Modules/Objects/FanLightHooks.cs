using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
			if (!Directory.Exists(directory))
				continue;
			string[] files = Directory.GetFiles(directory);
			for (int i = 0; i < files.Length; ++i)
			{
				string fn = Path.GetFileNameWithoutExtension(files[i]);
				if (!Futile.atlasManager.DoesContainAtlas(fn))
				{
					FAtlas atlas = Futile.atlasManager.LoadFanImage(Path.Combine("fanmasks", fn));
					atlas._texture = blur.Blur((atlas._texture as Texture2D)!, 5, 1);
				}
			}
		}
	}

	internal static void Dispose()
	{
		blur = null!;
		data = null!;
	}

	public static FAtlas LoadFanImage(this FAtlasManager self, string imagePath) => self.ActuallyLoadAtlasOrImage(imagePath.Split('\\')[1], imagePath + Futile.resourceSuffix, "");

	public class LinearBlur
	{
		Texture2D? _sourceImage;
		int _sourceWidth;
		int _sourceHeight;
		int _sourceLength;
		int _sourceLastYOffset;
		int _windowSize;
		Color[] _pixelBuffer;

		public Texture2D Blur(Texture2D image, int radius, int iterations)
		{
			_windowSize = radius * 2 + 1;
			_sourceWidth = image.width;
			_sourceHeight = image.height;
			Texture2D tex = image;
			for (var i = 0; i < iterations; i++)
				tex = OneDimensialBlurY(OneDimensialBlurX(tex, radius), radius);
			return tex;
		}

		Texture2D OneDimensialBlurX(Texture2D image, int radius)
		{
			_sourceImage = image;
			_pixelBuffer = _sourceImage.GetPixels();
			float _windowSizeR = 1.0f / _windowSize;
			Texture2D blurred = new(image.width, image.height, image.format, false);
			Color[] pixels = new Color[_sourceWidth * _sourceHeight];
			Parallel.For(0, _sourceHeight, imgY =>
			{
				int y = imgY * _sourceWidth;
				float rSum = 0f, gSum = 0f, bSum = 0f;
				for (var x = radius * -1; x <= radius; ++x)
				{
					ref Color pixel = ref GetPixelWithXCheck(x, y);
					rSum += pixel.r;
					gSum += pixel.g;
					bSum += pixel.b;
				}
				ref Color pixel0 = ref pixels[y];
				pixel0.r = rSum * _windowSizeR;
				pixel0.g = gSum * _windowSizeR;
				pixel0.b = bSum * _windowSizeR;
				pixel0.a = 1f;
				int imgX = 1;
				for (; imgX < radius + 2; ++imgX)
				{
					ref Color toExclude = ref _pixelBuffer[y];
					ref Color toInclude = ref _pixelBuffer[y + imgX + radius];
					ref Color pixelX = ref pixels[y + imgX];
					pixelX.r = (rSum += toInclude.r - toExclude.r) * _windowSizeR;
					pixelX.g = (gSum += toInclude.g - toExclude.g) * _windowSizeR;
					pixelX.b = (bSum += toInclude.b - toExclude.b) * _windowSizeR;
					pixelX.a = 1f;
				}
				for (; imgX < _sourceWidth - radius; ++imgX)
				{
					ref Color toExclude = ref _pixelBuffer[y + imgX - radius - 1];
					ref Color toInclude = ref _pixelBuffer[y + imgX + radius];
					ref Color pixelX = ref pixels[y + imgX];
					pixelX.r = (rSum += toInclude.r - toExclude.r) * _windowSizeR;
					pixelX.g = (gSum += toInclude.g - toExclude.g) * _windowSizeR;
					pixelX.b = (bSum += toInclude.b - toExclude.b) * _windowSizeR;
					pixelX.a = 1f;
				}
				for (; imgX < _sourceWidth; ++imgX)
				{
					ref Color toExclude = ref _pixelBuffer[y + imgX - radius - 1];
					ref Color toInclude = ref _pixelBuffer[y + _sourceWidth - 1];
					ref Color pixelX = ref pixels[y + imgX];
					pixelX.r = (rSum += toInclude.r - toExclude.r) * _windowSizeR;
					pixelX.g = (gSum += toInclude.g - toExclude.g) * _windowSizeR;
					pixelX.b = (bSum += toInclude.b - toExclude.b) * _windowSizeR;
					pixelX.a = 1f;
				}
			});
			blurred.SetPixels(pixels);
			blurred.Apply();
			return blurred;
		}

		Texture2D OneDimensialBlurY(Texture2D image, int radius)
		{
			_sourceImage = image;
			_pixelBuffer = _sourceImage.GetPixels();
			_sourceLength = _sourceWidth * _sourceHeight;
			_sourceLastYOffset = _sourceLength - _sourceWidth;
			float _windowSizeR = 1.0f / _windowSize;
			Texture2D blurred = new(image.width, image.height, image.format, false);
			Color[] pixels = new Color[_sourceWidth * _sourceHeight];
			int radiusOffsetLeft = (-radius - 1) * _sourceWidth,
				radiusOffsetRight = radius * _sourceWidth;
			Parallel.For(0, _sourceWidth, imgX =>
			{
				float rSum = 0f, gSum = 0f, bSum = 0f;
				for (int y = radius * -1; y <= radius; ++y)
				{
					ref Color pixel = ref GetPixelWithYCheck(imgX, y);
					rSum += pixel.r;
					gSum += pixel.g;
					bSum += pixel.b;
				}
				ref Color pixel0 = ref pixels[imgX];
				pixel0.r = rSum * _windowSizeR;
				pixel0.g = gSum * _windowSizeR;
				pixel0.b = bSum * _windowSizeR;
				pixel0.a = 1f;
				int imgY = 1;
				for (; imgY < radius + 2; ++imgY)
				{
					int y = imgY * _sourceWidth;
					ref Color toExclude = ref _pixelBuffer[imgX];
					ref Color toInclude = ref _pixelBuffer[imgX + y + radiusOffsetRight];
					ref Color pixelX = ref pixels[y + imgX];
					pixelX.r = (rSum += toInclude.r - toExclude.r) * _windowSizeR;
					pixelX.g = (gSum += toInclude.g - toExclude.g) * _windowSizeR;
					pixelX.b = (bSum += toInclude.b - toExclude.b) * _windowSizeR;
					pixelX.a = 1f;
				}
				for (; imgY < _sourceHeight - radius; ++imgY)
				{
					int y = imgY * _sourceWidth;
					ref Color toExclude = ref _pixelBuffer[imgX + y + radiusOffsetLeft];
					ref Color toInclude = ref _pixelBuffer[imgX + y + radiusOffsetRight];
					ref Color pixelX = ref pixels[y + imgX];
					pixelX.r = (rSum += toInclude.r - toExclude.r) * _windowSizeR;
					pixelX.g = (gSum += toInclude.g - toExclude.g) * _windowSizeR;
					pixelX.b = (bSum += toInclude.b - toExclude.b) * _windowSizeR;
					pixelX.a = 1f;
				}
				for (; imgY < _sourceHeight; ++imgY)
				{
					int y = imgY * _sourceWidth;
					ref Color toExclude = ref _pixelBuffer[imgX + y + radiusOffsetLeft];
					ref Color toInclude = ref _pixelBuffer[imgX + _sourceLastYOffset];
					ref Color pixelX = ref pixels[y + imgX];
					pixelX.r = (rSum += toInclude.r - toExclude.r) * _windowSizeR;
					pixelX.g = (gSum += toInclude.g - toExclude.g) * _windowSizeR;
					pixelX.b = (bSum += toInclude.b - toExclude.b) * _windowSizeR;
					pixelX.a = 1f;
				}
			});
			blurred.SetPixels(pixels);
			blurred.Apply();
			return blurred;
		}

		ref Color GetPixelWithXCheck(int x, int realY)
		{
			if (x <= 0)
				return ref _pixelBuffer[realY];
			if (x >= _sourceWidth)
				return ref _pixelBuffer[realY + _sourceWidth - 1];
			return ref _pixelBuffer[realY + x];
		}

		ref Color GetPixelWithYCheck(int x, int realY)
		{
			if (realY <= 0)
				return ref _pixelBuffer[x];
			if (realY >= _sourceLength)
				return ref _pixelBuffer[x + _sourceLastYOffset];
			return ref _pixelBuffer[realY + x];
		}
	}
}
