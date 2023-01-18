using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast
{
	/// <summary>
	/// Allow deer to enter specified modded rooms
	/// </summary>
	internal static class DeerFix
	{
		public static readonly string[] rooms = new string[] {
			"TM_E01"
		};
		public static readonly int[][] nodes = new int[][] {
			new[] { 6 } // TM_E01
        };

		public static void Apply()
		{
			// Add new rooms
			int len = DeerAbstractAI.UGLYHARDCODEDALLOWEDROOMS.Length;
			Array.Resize(ref DeerAbstractAI.UGLYHARDCODEDALLOWEDROOMS, len + rooms.Length);
			rooms.CopyTo(DeerAbstractAI.UGLYHARDCODEDALLOWEDROOMS, len);

			// Add new nodes
			len = DeerAbstractAI.UGLYHARDCODEDALLOWEDNODES.Length;
			Array.Resize(ref DeerAbstractAI.UGLYHARDCODEDALLOWEDNODES, len + nodes.Length);
			nodes.CopyTo(DeerAbstractAI.UGLYHARDCODEDALLOWEDNODES, len);

			On.DeerAbstractAI.ctor += DeerAbstractAI_ctor;
		}

		// Temporarily change hardcoded rooms based on the current region
		private static List<int[]> __nodes = new List<int[]>();
		private static List<string> __rooms = new List<string>();
		private static void DeerAbstractAI_ctor(On.DeerAbstractAI.orig_ctor orig, DeerAbstractAI self, World world, AbstractCreature parent)
		{
			if (world.region == null)
			{
				orig(self, world, parent);
				return;
			}
			string region = world.name;
			string[] rooms = DeerAbstractAI.UGLYHARDCODEDALLOWEDROOMS;
			int[][] nodes = DeerAbstractAI.UGLYHARDCODEDALLOWEDNODES;
			for (int i = 0; i < rooms.Length; i++)
			{
				if (rooms[i].Substring(0, region.Length) != region) continue;
				__rooms.Add(rooms[i]);
				__nodes.Add(nodes[i]);
			}
			DeerAbstractAI.UGLYHARDCODEDALLOWEDROOMS = __rooms.ToArray();
			DeerAbstractAI.UGLYHARDCODEDALLOWEDNODES = __nodes.ToArray();
			__rooms.Clear();
			__nodes.Clear();
			orig(self, world, parent);
			DeerAbstractAI.UGLYHARDCODEDALLOWEDROOMS = rooms;
			DeerAbstractAI.UGLYHARDCODEDALLOWEDNODES = nodes;
		}
	}
}
