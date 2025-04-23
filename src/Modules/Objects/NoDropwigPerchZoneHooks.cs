namespace RegionKit.Modules.Objects
{
	internal static class NoDropwigPerchZoneHooks
	{
		public static void Apply()
		{
			On.DropBugAI.ValidCeilingSpot += DropBugAI_ValidCeilingSpot;
			On.DropBugAI.ValidCeilingSpotControlled += DropBugAI_ValidCeilingSpotControlled;
		}

		public static void Undo()
		{
			On.DropBugAI.ValidCeilingSpot -= DropBugAI_ValidCeilingSpot;
			On.DropBugAI.ValidCeilingSpotControlled -= DropBugAI_ValidCeilingSpotControlled;
		}

		private static bool DropBugAI_ValidCeilingSpot(On.DropBugAI.orig_ValidCeilingSpot orig, Room room, IntVector2 test)
		{
			if (room.roomSettings.placedObjects.Any(x => x.type == _Enums.NoDropwigPerchZone && Vector2.Distance(x.pos, room.MiddleOfTile(test)) < (x.data as PlacedObject.ResizableObjectData)!.Rad))
			{
				return false;
			}
			return orig(room, test);
		}

		private static bool DropBugAI_ValidCeilingSpotControlled(On.DropBugAI.orig_ValidCeilingSpotControlled orig, Room room, IntVector2 test)
		{
			if (room.roomSettings.placedObjects.Any(x => x.type == _Enums.NoDropwigPerchZone && Vector2.Distance(x.pos, room.MiddleOfTile(test)) < (x.data as PlacedObject.ResizableObjectData)!.Rad))
			{
				return false;
			}
			return orig(room, test);
		}
	}
}
