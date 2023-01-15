using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast
{
	/// <summary>
	/// Make the specified gates use ElectricGate instead of WaterGate
	/// </summary>
	internal static class ElectricGates
	{
		public static string[] electricGates = new string[] {
			"GATE_SI_TM"
		};

		public static void Apply()
		{
			On.Room.Loaded += Room_Loaded;
			On.Room.AddObject += Room_AddObject;
			On.WaterGate.ctor += WaterGate_ctor;
		}

		// Set to true to inhibit WaterGates from being initialized or added to rooms
		private static bool _forceElectricGate = false;

		private static void WaterGate_ctor(On.WaterGate.orig_ctor orig, WaterGate self, Room room)
		{
			// Leaves the object in an uninitialized state if it will be replaced with an ElectricGate
			// This way it will not add any auxiliary objects, such as karma symbols
			// Most operations on this object will fail
			if (_forceElectricGate)
			{
				room.regionGate = new ElectricGate(room);
				room.AddObject(room.regionGate);
			};
			orig(self, room);
		}

		private static void Room_AddObject(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
		{
			if (obj is WaterGate && _forceElectricGate) return;
			orig(self, obj);
		}

		private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
		{
			if (electricGates.Contains(self.abstractRoom.name))
			{
				_forceElectricGate = true;
				orig(self);
				_forceElectricGate = false;
			}
			else
			{
				orig(self);
			}
		}
	}
}
