namespace RegionKit.Modules.RoomZones;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Room zones")]
public static class _Module
{
	public const string ZONES_POM_CATEGORY = RK_POM_CATEGORY + "-ZONES";
	public static UnityEngine.GameObject colliderHolder = null!;
	public static void Setup()
	{
		colliderHolder = new GameObject("rk_roomzones_colliderholder");
		RegisterManagedObject<RectZone, RectZoneData, ManagedRepresentation>("RectZone", ZONES_POM_CATEGORY);
	}
	public static void Enable()
	{
		On.Player.Update += (orig, self, eu) =>
		{
			orig(self, eu);
            if (self.room is null || self.room.updateList is null) return;
			foreach (UpdatableAndDeletable uad in self.room.updateList)
			{
				if (uad is IRoomZone zone && zone.PointInZone(self.mainBodyChunk.pos))
				{
					__logger.LogDebug("im in!!!");
				}

			}
		};
	}
	public static void Disable()
	{

	}
}