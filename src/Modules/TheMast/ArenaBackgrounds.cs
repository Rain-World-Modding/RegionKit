using System.Collections.Generic;
using UnityEngine;

//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast
{
	/// <summary>
	/// Allow arenas to use custom background effects
	/// </summary>
	internal static class ArenaBackgrounds
	{
		// Offsets of background scenes for any given arena, measured in level tiles
		// All levels that use a background scene must be in this list
		public static Dictionary<string, Vector2> sceneOffsets = new Dictionary<string, Vector2>()
		{
			{ "Spire", new Vector2(-1900f, 500f) },
			{ "SI_Array", new Vector2(-1900f, 200f) }
		};

		public static void Apply()
		{
			On.BackgroundScene.RoomToWorldPos += BackgroundScene_RoomToWorldPos;
		}

		// Force arena levels to be positioned at the center of the background scene
		private static Vector2 BackgroundScene_RoomToWorldPos(On.BackgroundScene.orig_RoomToWorldPos orig, BackgroundScene self, Vector2 inRoomPos)
		{
			AbstractRoom room = self.room.world.GetAbstractRoom(self.room.abstractRoom.index);
			if (sceneOffsets.TryGetValue(room.name, out Vector2 v))
			{
				Vector2 mapPos = self.sceneOrigo / 20f;
				mapPos += v;
				return mapPos * 20f + inRoomPos - new Vector2(room.size.x * 20f, room.size.y * 20f) / 2f;
			}
			else return orig(self, inRoomPos);
		}
	}
}
