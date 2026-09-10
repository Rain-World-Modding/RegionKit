namespace RegionKit.Modules.RoomZones;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Room zones")]
public static class _Module
{
	public static List<GameObject> colliderHolders = new();
	public static void Setup()
	{
		var category = Objects._Enums.MiscObjectsCategory;
		// colliderHolder = new GameObject("rk_roomzones_colliderholder");
		RegisterManagedObject<RectZone, RectZoneData, ManagedRepresentation>(_Enums.RectZone, category);
		RegisterManagedObject<CircleZone, CircleZoneData, ManagedRepresentation>(_Enums.CircleZone, category);
	}
	public static void Enable()
	{
		// On.Player.Update += (orig, self, eu) =>
		// {
		// 	orig(self, eu);
        //     if (self.room is null || self.room.updateList is null) return;
		// 	foreach (UpdatableAndDeletable uad in self.room.updateList)
		// 	{
		// 		if (uad is IRoomZone zone && zone.PointInZone(self.mainBodyChunk.pos))
		// 		{
		// 			LogDebug($"im in!!! {zone.Tag}");
		// 		}

		// 	}
		// };
	}
	public static void Disable()
	{

	}
}
