namespace RegionKit.Modules.MultiColorSnow;

using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using static RegionKit.Modules.Objects._Module;

[RegionKitModule(nameof(onEnable), nameof(onDisable), nameof(onInit), moduleName: "Multi-Color Snow")]
public class _Module
{
	static bool loaded = false;

	public static AssetBundle ColoredSnowShadersBundle = null!;

	public static Shader RKLevelSnowShader = null!;
	public static Shader RKDisplaySnowShader = null!;

	public static Material RKLevelSnowMaterial = null!;

	public static bool sbCameraScroll = false;

	public static FieldInfo _AbstractRoomMod_texture_offset = null!;
	public static MethodInfo _AbstractRoomMod_Get_Attached_Fields = null!;
	public static MethodInfo _RoomCameraMod_Is_Type_Camera_Not_Used = null!;

	public static readonly int ColoredSnowTex = Shader.PropertyToID("_RKColoredSnowTex");
	public static readonly int ColoredSnowSources = Shader.PropertyToID("_RKColoredSnowSources");
	public static readonly int ColoredSnowSources2 = Shader.PropertyToID("_RKColoredSnowSources2");
	public static readonly int ColoredSnowPalette = Shader.PropertyToID("_RKColoredSnowPalette");

	internal static void onEnable()
	{
		On.Room.Update += Room_Update;
		On.Room.NowViewed += Room_NowViewed;
		On.RoomCamera.ctor += RoomCamera_ctor;
		On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
		On.RoomCamera.ChangeRoom += RoomCamera_ChangeRoom;
		On.RoomCamera.ApplyFade += RoomCamera_ApplyFade;

		IL.RoomCamera.Update += RoomCamera_Update;
	}

	internal static void onDisable()
	{
		On.Room.Update -= Room_Update;
		On.Room.NowViewed -= Room_NowViewed;
		On.RoomCamera.ctor -= RoomCamera_ctor;
		On.RoomCamera.DrawUpdate -= RoomCamera_DrawUpdate;
		On.RoomCamera.ChangeRoom -= RoomCamera_ChangeRoom;
		On.RoomCamera.ApplyFade -= RoomCamera_ApplyFade;

		IL.RoomCamera.Update -= RoomCamera_Update;
	}

	internal static void onInit()
	{
		if (!loaded)
		{
			loaded = true;
			loadShaders();

			List<ManagedField> snowSourceFields =
			[
				new IntegerField("palette", 0, 255, 0, ManagedFieldWithPanel.ControlType.arrows, "Group"),
				new IntegerField("intensity", 0, 100, 100, ManagedFieldWithPanel.ControlType.slider, "Intensity"),
				new IntegerField("irregularity", 0, 100, 0, ManagedFieldWithPanel.ControlType.slider, "Irregularity"),
				new ExtEnumField<ColoredSnowShape>("shape", ColoredSnowShape.Radial, null, ManagedFieldWithPanel.ControlType.button, "Shape"),
				new BooleanField("unsnow", false, ManagedFieldWithPanel.ControlType.button, "Unsnow"),
				new Vector2Field("range", new Vector2(100, 0), Vector2Field.VectorReprType.circle)
			];

			RegisterFullyManagedObjectType([.. snowSourceFields], typeof(ColoredSnowSourceUAD), "ColoredSnowSource", DECORATIONS_POM_CATEGORY);

			List<ManagedField> snowSourceSettingsFields =
			[
				new IntegerField("palette", 0, 255, 0, ManagedFieldWithPanel.ControlType.arrows, "Group"),
				new IntegerField("front", 0, 30, 0, ManagedFieldWithPanel.ControlType.slider, "From Depth"),
				new IntegerField("back", 0, 30, 30, ManagedFieldWithPanel.ControlType.slider, "To Depth"),
				new ColorField("color", new(1f, 1f, 1f), ManagedFieldWithPanel.ControlType.button, "Color"),
				new IntegerField("s", 0, 255, 255, ManagedFieldWithPanel.ControlType.slider, "Blend"),
				new ColorField("rainColor", new(1f, 1f, 1f), ManagedFieldWithPanel.ControlType.button, "Rain Color"),
				new IntegerField("es", 0, 255, 255, ManagedFieldWithPanel.ControlType.slider, "Rain Blend")
			];

			RegisterFullyManagedObjectType([.. snowSourceSettingsFields], typeof(ColoredSnowGroupUAD), "ColoredSnowGroup", DECORATIONS_POM_CATEGORY);

			foreach (ModManager.Mod mod in ModManager.ActiveMods)
			{
				if (mod.id == "SBCameraScroll")
				{
					sbCameraScroll = true;

					Assembly assembly = Assembly.Load("SBCameraScroll");
					Type roomMod = assembly.GetType("SBCameraScroll.AbstractRoomMod");

					Type attachedFieldsType = roomMod.GetNestedType("Attached_Fields", BindingFlags.NonPublic | BindingFlags.Public);

					_AbstractRoomMod_texture_offset = attachedFieldsType.GetField("texture_offset", BindingFlags.Public | BindingFlags.Instance);

					_AbstractRoomMod_Get_Attached_Fields = roomMod.GetMethod("Get_Attached_Fields", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

					Type roomCameraMod = assembly.GetType("SBCameraScroll.RoomCameraMod");

					_RoomCameraMod_Is_Type_Camera_Not_Used = roomCameraMod.GetMethod("Is_Type_Camera_Not_Used", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

					break;
				}
			}
		}
	}

	internal static void loadShaders()
	{
		RainWorld rw = CRW;

		ColoredSnowShadersBundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/rkcoloredsnow"));
		RKLevelSnowShader = ColoredSnowShadersBundle.LoadAsset<Shader>("Assets/RKLevelSnowShader.shader");
		rw.Shaders["RKLevelSnowShader"] = FShader.CreateShader("RKLevelSnowShader", RKLevelSnowShader);
		RKDisplaySnowShader = ColoredSnowShadersBundle.LoadAsset<Shader>("Assets/RKDisplaySnowShader.shader");
		rw.Shaders["RKDisplaySnowShader"] = FShader.CreateShader("RKDisplaySnowShader", RKDisplaySnowShader);

		RKLevelSnowMaterial = new Material(RKLevelSnowShader);
	}

	private static void RoomCamera_Update(ILContext il)
	{
		var cursor = new ILCursor(il);

		if (cursor.TryGotoNext(
			i => i.MatchLdarg(0),
			i => i.MatchCall(typeof(RoomCamera).GetProperty("room").GetGetMethod()),
			i => i.MatchLdfld(typeof(Room).GetField("snow"))
			))
		{
			cursor.Index--;
		}

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Call, typeof(_Module).GetMethod("UpdateRoomCamera", [typeof(RoomCamera)]));
	}

	public static void UpdateRoomCamera(RoomCamera self)
	{
		if (ColoredSnowWeakRoomData.GetData(self.room).snow)
		{
			ColoredSnowRoomCamera.UpdateSnowLight(self);
		}
	}

	private static void RoomCamera_ChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
	{
		orig.Invoke(self, newRoom, cameraPosition);
		if (ColoredSnowWeakRoomData.GetData(newRoom).snowObject != null)
		{
			ColoredSnowRoomCamera.GetData(self).snowChange = true;
		}
	}

	private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
	{
		orig.Invoke(self, timeStacker, timeSpeed);
		if (self.room != null)
		{
			if (ColoredSnowRoomCamera.GetData(self).snowChange || self.fullscreenSync != UnityEngine.Screen.fullScreen)
			{
				if (ColoredSnowWeakRoomData.GetData(self.room).snow)
				{
					ColoredSnowRoomCamera.UpdateSnowLight(self);
				}
			}
		}
	}
	private static void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
	{
		orig.Invoke(self, game, cameraNumber);

		ColoredSnowRoomCamera data = ColoredSnowRoomCamera.GetData(self);

		data.coloredSnowTexture = new RenderTexture(1400, 800, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
		{
			filterMode = FilterMode.Point
		};
		Shader.SetGlobalTexture(ColoredSnowTex, data.coloredSnowTexture);

		Shader.DisableKeyword("SNOW_ON");

		data.coloredSnowSources = new Texture2D(7, 7, TextureFormat.RGBA32, false)
		{
			filterMode = FilterMode.Point
		};
		Shader.SetGlobalTexture(ColoredSnowSources, data.coloredSnowSources);

		data.coloredSnowSources2 = new Texture2D(4, 4, TextureFormat.RGBA32, false)
		{
			filterMode = FilterMode.Point
		};
		Shader.SetGlobalTexture(ColoredSnowSources2, data.coloredSnowSources2);

		data.coloredSnowPalette = new Texture2D(16, 16, TextureFormat.RGBA32, false)
		{
			filterMode = FilterMode.Point
		};
		Shader.SetGlobalTexture(ColoredSnowPalette, data.coloredSnowPalette);
	}

	private static readonly PropertyInfo _RoomCamera_fadeCoord = typeof(RoomCamera).GetProperty("fadeCoord", BindingFlags.NonPublic | BindingFlags.Instance);

	private static void RoomCamera_ApplyFade(On.RoomCamera.orig_ApplyFade orig, RoomCamera self)
	{
		orig.Invoke(self);

		if (self == null)
		{
			return;
		}

		if (self.room == null)
		{
			return;
		}

		ColoredSnowRoomCamera cameraData = ColoredSnowRoomCamera.GetData(self);
		ColoredSnowWeakRoomData roomData = ColoredSnowWeakRoomData.GetData(self.room);

		cameraData.palette ??= (Color[])ColoredSnowRoomCamera.empty3.Clone();

		int source = 0;

		for (int i = 0; i < roomData.snowSources.Count && source < 20; i++)
		{
			ColoredSnowSourceUAD snowSource = roomData.snowSources[i];

			if (snowSource.visibility == 1)
			{

				if (roomData.snowPalettes.ContainsKey(snowSource.data.palette))
				{
					cameraData.palette[snowSource.data.palette] = roomData.snowPalettes[snowSource.data.palette].getBlendedRGBA(((Vector4)_RoomCamera_fadeCoord.GetValue(self)).y);
				}
				else
				{
					cameraData.palette[snowSource.data.palette] = Color.white;
				}

				source++;
			}

		}

		cameraData.coloredSnowPalette.SetPixels(cameraData.palette);
		cameraData.coloredSnowPalette.Apply();
	}

	private static void Room_NowViewed(On.Room.orig_NowViewed orig, Room self)
	{
		orig.Invoke(self);
		if (ColoredSnowWeakRoomData.GetData(self).snowObject == null)
		{
			Shader.DisableKeyword("SNOW_ON");
		}
	}

	private static void Room_Update(On.Room.orig_Update orig, Room self)
	{
		if (ColoredSnowWeakRoomData.GetData(self).snowSources.Count == 0)
		{
			ColoredSnowWeakRoomData.GetData(self).snow = false;
		}
		orig.Invoke(self);
	}
}

