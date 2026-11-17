using System.Runtime.CompilerServices;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.ShelterBehaviors
{
	public class ShelterDataManager
	{
		private static readonly RKConditionalWeakTable<Room, ShelterDataManager> dataCWT = new();
		public static bool TryGetManager(Room? room, out ShelterDataManager? data)
		{
			// Extra safety
			data = null;
			if (room is null) return false;

			// Try to get the data
			if (dataCWT.TryGetValue(room, out var shelterDataManager))
			{
				data = shelterDataManager;
				return true;
			}
            if (room.abstractRoom.shelter && !room.abstractRoom.isAncientShelter)
            {
                data = new ShelterDataManager(room);
                dataCWT.Add(room, data);
                return true;
            }
			return false;
		}

		public static readonly Dictionary<string, int> consumedShelters = [];

		private readonly Room room;

		private readonly List<Vector2> spawnPoints = [];
		private readonly List<IntRect> triggerZones = [];
		private readonly List<IntRect> noTriggerZones = [];

		public readonly (IntVector2 pos, IntVector2 dir)? firstShelterDoorSpot = null;
		public readonly List<CosmeticShelterDoor> cosmeticShelterDoors = [];

		public readonly (int minCycles, int maxCycles, float chance)? consumableShelterData = null;
		public readonly int consumableShelterIndex = -1;
		public readonly bool holdToTrigger = false;
		public readonly bool doorless = false;

		private ShelterDataManager(Room room)
		{
			this.room = room;
			for (int i = 0; i < room.roomSettings.placedObjects.Count; i++)
			{
				PlacedObject po = room.roomSettings.placedObjects[i];
				if (po.type == _Enums.ShelterBhvrSpawnPosition)
				{
					LogDebug($"Shelter behavior spawn point: {po.pos}");
					spawnPoints.Add(po.pos);
				}
				else if (po.type == _Enums.ShelterBhvrTriggerZone)
				{
					triggerZones.Add((po.data as PlacedObject.GridRectObjectData)!.Rect);
				}
				else if (po.type == _Enums.ShelterBhvrNoTriggerZone)
				{
					noTriggerZones.Add((po.data as PlacedObject.GridRectObjectData)!.Rect);
				}
				else if (po.type == _Enums.ShelterBhvrPlacedDoor && !doorless)
				{
					IntVector2 dir = (po.data as ManagedData)!.GetValue<IntVector2>("dir");
					LogDebug($"Shelter behavior placed door: {room.GetTilePosition(po.pos)}");

                    if (firstShelterDoorSpot == null)
					{
						firstShelterDoorSpot = (room.GetTilePosition(po.pos), dir);
					}
					else
					{
						var door = new CosmeticShelterDoor(room, room.GetTilePosition(po.pos), dir);
                        cosmeticShelterDoors.Add(door);
						room.AddObject(door);
					}
				}
				else if (po.type == _Enums.ShelterBhvrConsumableShelter && consumableShelterData is null)
				{
					int min = (po.data as ManagedData)!.GetValue<int>("min");
					int max = (po.data as ManagedData)!.GetValue<int>("max");
					float chance = (po.data as ManagedData)!.GetValue<float>("chance");
					consumableShelterData = (min, max, chance);
					consumableShelterIndex = i;
					LogDebug($"Shelter behavior consumable shelter: {consumableShelterIndex}");
				}
				else if (po.type == _Enums.ShelterBhvrDoorless)
				{
					doorless = true;
					foreach (var door in cosmeticShelterDoors)
					{
						room.RemoveObject(door);
					}
					cosmeticShelterDoors.Clear();
					LogDebug($"Shelter behavior doorless! (cosmetic doors removed)");
				}
				else if (po.type == _Enums.ShelterBhvrHoldToTrigger)
				{
					holdToTrigger = true;
				}
			}
		}

		private Vector2? RandomSpawnPoint()
		{
			if (spawnPoints.Count == 0)
			{
				return null;
			}
			if (spawnPoints.Count == 1)
			{
				return spawnPoints[0];
			}

			int seed = room.game.clock + room.game.GetStorySession.saveState.seed + room.game.GetStorySession.saveState.cycleNumber;
			Random.State oldState = Random.state;
			Random.InitState(seed);
			Vector2 pos = spawnPoints[Random.Range(0, spawnPoints.Count)];
			Random.state = oldState;
			return pos;
        }

		public bool TryGetRandomSpawnPoint(out Vector2? spawnPoint)
		{
			return (spawnPoint = RandomSpawnPoint()) != null;
		}

		public bool ZoneCheck(Creature critter)
		{
			bool inZone = true;
			foreach (BodyChunk chunk in critter.bodyChunks)
			{
				inZone &= TileInZones(room.GetTilePosition(chunk.pos));
			}
			return inZone;
		}

		public bool TileInZones(IntVector2 tile)
		{
			bool inZone = true;
			foreach (IntRect triggerRect in triggerZones)
			{
				inZone &= triggerRect.Contains(tile);
			}
			foreach (IntRect noTriggerRect in noTriggerZones)
			{
				inZone &= !noTriggerRect.Contains(tile);
			}
			return inZone;
		}

		public void ConsumeShelter()
		{
			if (consumableShelterIndex > -1 && room.game.IsStorySession && consumableShelterData != null && Random.value <= consumableShelterData.Value.chance)
			{
				var cyclesToWait = consumableShelterData.Value.minCycles > 0 ? Random.Range(consumableShelterData.Value.minCycles, consumableShelterData.Value.maxCycles + 1) : -1;
                room.game.GetStorySession.saveState.ReportConsumedItem(room.world, false, room.abstractRoom.index, consumableShelterIndex, cyclesToWait);
			}
		}

		public bool ShelterConsumed
		{
			get
			{
				if (consumableShelterIndex > -1 && room.game.IsStorySession)
				{
					return room.game.GetStorySession.saveState.ItemConsumed(room.world, false, room.abstractRoom.index, consumableShelterIndex);
				}
				return false;
			}
		}
	}
}
