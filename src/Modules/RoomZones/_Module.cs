namespace RegionKit.Modules.RoomZones;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Room zones")]
public static class _Module
{
	public const string ZONES_POM_CATEGORY = Objects._Module.GAMEPLAY_POM_CATEGORY;
	public static List<GameObject> colliderHolders = new();
	public static void Setup()
	{
		// colliderHolder = new GameObject("rk_roomzones_colliderholder");
		RegisterManagedObject<RectZone, RectZoneData, ManagedRepresentation>("RectZone", ZONES_POM_CATEGORY);
		RegisterManagedObject<CircleZone, CircleZoneData, ManagedRepresentation>("CircleZone", ZONES_POM_CATEGORY);
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
		// 			__logger.LogDebug($"im in!!! {zone.Tag}");
		// 		}

		// 	}
		// };
	}
	public static void Disable()
	{

	}
}
