namespace RegionKit.Modules.MultiColorSnow;

using System.Reflection;
using System.Runtime.CompilerServices;

public class ColoredSnowRoomCamera
{
	private static readonly ConditionalWeakTable<RoomCamera, ColoredSnowRoomCamera> weakData = new ConditionalWeakTable<RoomCamera, ColoredSnowRoomCamera>();

	public static Color[] empty;
	public static Color[] empty2;
	public static Color[] empty3;

	public bool snowChange;
	public RenderTexture coloredSnowTexture;
	public Texture2D coloredSnowSources;
	public Texture2D coloredSnowSources2;
	public Texture2D coloredSnowPalette;
	public Color[] palette;

	static ColoredSnowRoomCamera()
	{
		empty2 = new Color[16];
		Color color = new Color(0f, 0f, 0f, 0f);
		Color color2 = new Color(1f, 1f, 1f, 1f);
		for (int j = 0; j < empty2.Length; j++)
		{
			empty2[j] = color;
		}
		empty = new Color[49];
		for (int j = 0; j < empty.Length; j++)
		{
			empty[j] = color;
		}
		empty3 = new Color[256];
		for (int j = 0; j < empty3.Length; j++)
		{
			empty3[j] = color2;
		}
	}

	private static PropertyInfo _RoomCamera_fadeCoord = typeof(RoomCamera).GetProperty("fadeCoord", BindingFlags.NonPublic | BindingFlags.Instance);
	private static PropertyInfo _RoomCamera_levelTexture = typeof(RoomCamera).GetProperty("levelTexture", BindingFlags.NonPublic | BindingFlags.Instance);

	public static void UpdateSnowLight(RoomCamera camera)
	{
		ColoredSnowRoomCamera cameraData = ColoredSnowRoomCamera.GetData(camera);
		ColoredSnowWeakRoomData roomData = ColoredSnowWeakRoomData.GetData(camera.room);

		if (cameraData.palette == null)
		{
			cameraData.palette = (Color[])empty3.Clone();
		}

		int source = 0;

		float[] shapeBuffer = new float[4];
		Vector2[] depthBuffer = new Vector2[2];
		float[] paletteBuffer = new float[4];

		Color[] packedSources = (Color[])empty.Clone();
		Color[] packedSources2 = (Color[])empty2.Clone();

		for (int i = 0; i < roomData.snowSources.Count && source < 20; i++)
		{
			ColoredSnowSourceUAD snowSource = roomData.snowSources[i];

			if (snowSource.visibility == 1)
			{
				Vector4[] packedData = snowSource.PackSnowData();
				packedSources[source] = new Color(packedData[0].x, packedData[0].y, packedData[0].z, packedData[0].w);
				packedSources[source + 20] = new Color(packedData[1].x, packedData[1].y, packedData[1].z, packedData[1].w);
				shapeBuffer[source % 4] = packedData[2].w;
				paletteBuffer[source % 4] = (float)snowSource.data.palette / 255.0F;

				if ((source + 1) % 4 == 0)
				{
					packedSources[source / 4 + 40] = new Color(shapeBuffer[0], shapeBuffer[1], shapeBuffer[2], shapeBuffer[3]);
					packedSources2[source / 4 + 10] = new Color(paletteBuffer[0], paletteBuffer[1], paletteBuffer[2], paletteBuffer[3]);
				}

				if (roomData.snowPalettes.ContainsKey(snowSource.data.palette))
				{
					cameraData.palette[snowSource.data.palette] = roomData.snowPalettes[snowSource.data.palette].getBlendedRGBA(((Vector4)_RoomCamera_fadeCoord.GetValue(camera)).y);
					depthBuffer[source % 2] = new Vector2((float)roomData.snowPalettes[snowSource.data.palette].data.from / 30f, (float)roomData.snowPalettes[snowSource.data.palette].data.to / 30f);
				}
				else
				{
					cameraData.palette[snowSource.data.palette] = Color.white;
					depthBuffer[source % 2] = new Vector2(0, 1);
				}

				if ((source + 1) % 2 == 0)
				{
					packedSources2[source / 2] = new Color(depthBuffer[0].x, depthBuffer[0].y, depthBuffer[1].x, depthBuffer[1].y);
				}

				source++;
			}

		}

		if (source > 0)
		{
			source--;

			if ((source + 1) % 4 != 0)
			{
				int s = source / 4 + 40;
				int s2 = source / 4 + 10;
				switch ((source + 1) % 4)
				{
				case 1:
					packedSources[s] = new Color(shapeBuffer[0], 0, 0, 0);
					packedSources2[s2] = new Color(paletteBuffer[0], 0, 0, 0);
					break;
				case 2:
					packedSources[s] = new Color(shapeBuffer[0], shapeBuffer[1], 0, 0);
					packedSources2[s2] = new Color(paletteBuffer[0], paletteBuffer[1], 0, 0);
					break;
				case 3:
					packedSources[s] = new Color(shapeBuffer[0], shapeBuffer[1], shapeBuffer[2], 0);
					packedSources2[s2] = new Color(paletteBuffer[0], paletteBuffer[1], paletteBuffer[2], 0);
					break;
				}
			}

			if ((source + 1) % 2 != 0)
			{
				packedSources2[source / 2] = new Color(depthBuffer[0].x, depthBuffer[0].y, 0, 0);
			}

			packedSources2[15] = new Color((source / 20.0F), 0, 0, 0);
			source++;
			Shader.EnableKeyword("SNOW_ON");
		}
		else
		{
			Shader.DisableKeyword("SNOW_ON");
		}

		roomData.snowObject.visibleSnow = source;
		cameraData.coloredSnowSources.SetPixels(packedSources);
		cameraData.coloredSnowSources.Apply();
		cameraData.coloredSnowSources2.SetPixels(packedSources2);
		cameraData.coloredSnowSources2.Apply();
		cameraData.coloredSnowPalette.SetPixels(cameraData.palette);
		cameraData.coloredSnowPalette.Apply();
		Graphics.Blit((Texture2D)_RoomCamera_levelTexture.GetValue(camera), cameraData.coloredSnowTexture, _Module.RKLevelSnowMaterial);
		cameraData.snowChange = false;
	}

	public static ColoredSnowRoomCamera GetData(RoomCamera obj)
	{
		return weakData.GetOrCreateValue(obj);
	}
}

