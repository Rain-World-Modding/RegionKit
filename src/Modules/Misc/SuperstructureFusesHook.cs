namespace RegionKit.Modules.Misc;

/// <summary>
/// By Woodensponge and Slime_Cubed
/// </summary>
internal static class SuperstructureFusesHook
{
	public static void Apply() => On.SuperStructureFuses.ctor += SuperStructureFusesCtor;

	public static void Undo() => On.SuperStructureFuses.ctor -= SuperStructureFusesCtor;

	private static void SuperStructureFusesCtor(On.SuperStructureFuses.orig_ctor orig, SuperStructureFuses self, PlacedObject placedObject, IntRect rect, Room room)
	{
		orig(self, placedObject, rect, room);
		if (room.world.region?.name is "ED" or "CM")
			self.broken = 0f;
	}
}
