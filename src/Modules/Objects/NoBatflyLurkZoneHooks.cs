using System.Runtime.CompilerServices;

namespace RegionKit.Modules.Objects
{
	internal static class NoBatflyLurkZoneHooks
	{
		private static ConditionalWeakTable<Fly, List<PlacedObject>> flyNoLurkZones = new();

		public static void Apply()
		{
			On.Fly.NewRoom += Fly_NewRoom;
			On.FlyAI.ValidSwarmPosition += FlyAI_ValidSwarmPosition;
			On.FlyAI.Update += FlyAI_Update;
		}

		public static void Undo()
		{
			On.Fly.NewRoom -= Fly_NewRoom;
			On.FlyAI.ValidSwarmPosition -= FlyAI_ValidSwarmPosition;
			On.FlyAI.Update -= FlyAI_Update;
		}

		private static void FlyAI_Update(On.FlyAI.orig_Update orig, FlyAI self)
		{
			orig(self);

			// Push away
			if (flyNoLurkZones.TryGetValue(self.fly, out List<PlacedObject> noLurkZones))
			{
				foreach (PlacedObject obj in noLurkZones)
				{
					float dist = Vector2.Distance(self.FlyPos, obj.pos);
					float rad = (obj.data as PlacedObject.ResizableObjectData)!.Rad;
					if (dist < rad)
					{
						Vector2 norm = (obj.data as PlacedObject.ResizableObjectData)!.handlePos.normalized;
						self.fly.mainBodyChunk.vel += norm * 1.4f * Mathf.InverseLerp(rad, rad / 5f, dist);
						self.localGoal += norm * 20.4f * Mathf.InverseLerp(rad, -rad / 4f, dist);
					}
				}
			}
		}

		private static void Fly_NewRoom(On.Fly.orig_NewRoom orig, Fly self, Room room)
		{
			orig(self, room);

			// Find all no batfly lurk zones and track them
			if (!flyNoLurkZones.TryGetValue(self, out List<PlacedObject> noLurkZones))
			{
				noLurkZones = new();
				flyNoLurkZones.Add(self, noLurkZones);
			}
			else
			{
				noLurkZones.Clear();
			}

			foreach (PlacedObject obj in room.roomSettings.placedObjects)
			{
				if (obj.type == _Enums.NoBatflyLurkZone)
				{
					noLurkZones.Add(obj);
				}
			}
		}

		private static bool FlyAI_ValidSwarmPosition(On.FlyAI.orig_ValidSwarmPosition orig, FlyAI self, Vector2 testPos)
		{
			bool result = orig(self, testPos);

			// If it's a valid swarm position normally, make sure it's not in a no lurk zone
			if (result && flyNoLurkZones.TryGetValue(self.fly, out List<PlacedObject> noLurkZones))
			{
				foreach (PlacedObject obj in noLurkZones)
				{
					if (Vector2.Distance(self.FlyPos, obj.pos) < (obj.data as PlacedObject.ResizableObjectData)!.Rad)
					{
						return false;
					}
				}
			}
			return result;
		}
	}
}
