namespace RegionKit;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Common Hooks")]
internal static class _CommonHooks
{

	private static Action<Room>[] __emptyinv = new Action<Room>[0];
	internal static void Enable()
	{
		On.Room.Loaded += RoomLoadedPatch;
		On.RoomSettings.Save_string_bool += RoomSettings_Save_string_bool;
		On.Region.ctor_string_int_int_RainWorldGame_Timeline += Region_ctor_string_int_int_RainWorldGame_Timeline;
	}

	internal static void Disable()
	{
		On.Room.Loaded -= RoomLoadedPatch;
		On.RoomSettings.Save_string_bool -= RoomSettings_Save_string_bool;
		On.Region.ctor_string_int_int_RainWorldGame_Timeline -= Region_ctor_string_int_int_RainWorldGame_Timeline;
	}

	private static void Region_ctor_string_int_int_RainWorldGame_Timeline(On.Region.orig_ctor_string_int_int_RainWorldGame_Timeline orig, Region self, string name, int firstRoomIndex, int regionNumber, RainWorldGame game, SlugcatStats.Timeline timelineIndex)
	{
		orig(self, name, firstRoomIndex, regionNumber, game, timelineIndex);

		foreach ((string key, string value) in self.regionParams.unrecognizedParams)
		{
			if (SpecificUnrecognizedRegionParamProcessor.TryGetValue(key, out var action))
			{
				try
				{
					action(self, key, value);
				}
				catch (Exception ex)
				{
					LogError(ex);
				}
			}

			try
			{
				GeneralUnrecognizedRegionParamProcessor?.Invoke(self, key, value);
			}
			catch (Exception ex)
			{
				LogError(ex);
			}
		}
	}

	internal static readonly Dictionary<string, Action<Region, string, string>> SpecificUnrecognizedRegionParamProcessor = new();
	internal static event Action<Region, string, string>? GeneralUnrecognizedRegionParamProcessor;

	internal static void RoomLoadedPatch(On.Room.orig_Loaded orig, Room self)
	{
		List<(Exception, bool)> errors = new();
		foreach (Action<Room> pre in PreRoomLoad?.GetInvocationList() ?? __emptyinv)
		{
			try
			{
				pre?.Invoke(self);
			}
			catch (Exception ex)
			{
				errors.Add((ex, false));
			}
		}
		orig(self);
		foreach (Action<Room> post in PostRoomLoad?.GetInvocationList() ?? __emptyinv)
		{
			try
			{
				post?.Invoke(self);
			}
			catch (Exception ex)
			{
				errors.Add((ex, true));
			}
		}
		foreach ((Exception err, bool post) in errors)
		{
			LogError((post ? "Error in PostRoomLoad : " : "Error in PreRoomLoad : ") + err);
		}
	}


	internal static event Action<Room>? PreRoomLoad;

	internal static event Action<Room>? PostRoomLoad;


	internal static void RoomSettings_Save_string_bool(On.RoomSettings.orig_Save_string_bool orig, RoomSettings self, string path, bool saveAsTemplate)
	{
		orig(self, path, saveAsTemplate);
		if (!System.IO.File.Exists(path)) return;

		List<Exception> errors = new();
		List<string> lines = new();
		foreach (RSLines del in RoomSettingsSave?.GetInvocationList() ?? new RSLines[0])
		{
			try { lines.AddRange(del(self, saveAsTemplate)); }
			catch (Exception ex) { errors.Add(ex); }
		}
		if (lines.Count > 0) System.IO.File.AppendAllLines(path, lines);

		foreach (Exception err in errors) LogError(("Error in RoomSettings_Save : ") + err);
	}

	//not a func for the sake of auto-generating arg names
	internal static event RSLines? RoomSettingsSave;

	internal delegate List<string> RSLines(RoomSettings self, bool saveAsTemplate);
}
