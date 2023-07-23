namespace RegionKit;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Common Hooks")]
internal static class _CommonHooks
{

	private static Action<Room>[] __emptyinv = new Action<Room>[0];
	internal static void Enable()
	{
		On.Room.Loaded += RoomLoadedPatch;
		On.RoomSettings.Save_string_bool += RoomSettings_Save_string_bool;
	}

	internal static void Disable()
	{
		On.Room.Loaded -= RoomLoadedPatch;
		On.RoomSettings.Save_string_bool -= RoomSettings_Save_string_bool;
	}


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
			__logger.LogError((post ? "Error in PostRoomLoad : " : "Error in PreRoomLoad : ") + err);
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

		foreach (Exception err in errors) __logger.LogError(("Error in RoomSettings_Save : ") + err);
	}

	//not a func for the sake of auto-generating arg names
	internal static event RSLines? RoomSettingsSave;

	internal delegate List<string> RSLines(RoomSettings self, bool saveAsTemplate);
}
