namespace RegionKit;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Common Hooks")]
internal static class _CommonHooks
{

	private static Action<Room>[] __emptyinv = new Action<Room>[0];
	internal static void Enable()
	{
		On.Room.Loaded += RoomLoadedPatch;
	}
	internal static void Disable()
	{
		On.Room.Loaded -= RoomLoadedPatch;
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
				errors.Add((ex, false));
			}
		}
		foreach ((Exception err, bool post) in errors)
		{

		}
	}


	internal static event Action<Room>? PreRoomLoad;

	internal static event Action<Room>? PostRoomLoad;
}
