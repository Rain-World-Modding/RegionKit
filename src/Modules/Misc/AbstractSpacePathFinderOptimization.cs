using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using Expedition;
using MonoMod.RuntimeDetour;

namespace RegionKit.Modules.Misc
{
	/// <summary>
	/// Optimization from Faster World by glebi574, used with permission
	/// </summary>
	public static class AbstractSpacePathFinderOptimization
	{
		public static Detour? __detour;

		internal static void Apply()
		{
			if (!Chainloader.PluginInfos.ContainsKey("0gelbi.faster-world"))
			{
				Undo();
				__detour = new Detour(
					typeof(AbstractSpacePathFinder).GetMethod(nameof(AbstractSpacePathFinder.Path), BF_ALL_CONTEXTS), 
					typeof(AbstractSpacePathFinderOptimization).GetMethod(nameof(AbstractSpacePathFinder_Path), BF_ALL_CONTEXTS_STATIC));
			}
		}

		internal static void Undo()
		{
			try
			{
				if (__detour != null)
				{
					__detour.Undo();
					__detour.Dispose();
				}
			}
			catch (Exception ex)
			{
				LogError(ex);
			}
			__detour = null;
		}

		public static readonly Dictionary<AbstractRoomNode.Type, int> nodeTypes = new()
		{
			{ AbstractRoomNode.Type.Exit, 0 },
			{ AbstractRoomNode.Type.SideExit, 1 },
			{ AbstractRoomNode.Type.SkyExit, 2 },
			{ AbstractRoomNode.Type.SeaExit, 3 },
			{ AbstractRoomNode.Type.RegionTransportation, 4 },
		};

		public static List<WorldCoordinate>? AbstractSpacePathFinder_Path(World world, WorldCoordinate start, WorldCoordinate goal, CreatureTemplate creatureType, IOwnAnAbstractSpacePathFinder owner)
		{
			if (start == goal)
				return [];
			int startNumber = start.room, goalNumber = goal.room;
			if (!world.IsRoomInRegion(startNumber) || !world.IsRoomInRegion(goalNumber))
				return null;
			AbstractRoom startRoom = world.GetAbstractRoom(startNumber), goalRoom = world.GetAbstractRoom(goalNumber);
			if (startRoom == null || goalRoom == null || startRoom.nodes.Length == 0 || goalRoom.nodes.Length == 0)
				return null;
			int startNode = start.abstractNode, goalNode = goal.abstractNode;
			if (!start.NodeDefined || startNode >= startRoom.nodes.Length)
				start.abstractNode = UnityEngine.Random.Range(0, startRoom.nodes.Length);
			if (!goal.NodeDefined || goalNode >= goalRoom.nodes.Length)
				goal.abstractNode = UnityEngine.Random.Range(0, goalRoom.nodes.Length);
			int startNodeIndex = startRoom.nodes[startNode].type.Index;
			if ((startNodeIndex == -1 || !creatureType.mappedNodeTypes[startNodeIndex]) && !startRoom.offScreenDen)
			{
				UnityEngine.Debug.LogWarning("Looking for path from inside node that's inaccessible to creature: " + creatureType.name);
				UnityEngine.Debug.LogWarning("start: " + start);
				return null;
			}
			int goalNodeIndex = goalRoom.nodes[goalNode].type.Index;
			List<WorldCoordinate> result = [];
			if (goalNodeIndex == -1 || !creatureType.mappedNodeTypes[goalNodeIndex])
			{
				UnityEngine.Debug.LogWarning(creatureType.name + " is looking for path to inaccessible node. Attempting to assign random other node in room.");
				for (int n = 0; n < goalRoom.nodes.Length; ++n)
					if (goalRoom.nodes[n].type.Index != -1 && creatureType.mappedNodeTypes[goalRoom.nodes[n].type.Index])
						result.Add(new(goalNumber, -1, -1, n));
				if (result.Count < 1)
				{
					UnityEngine.Debug.LogWarning("However... failed, as there are no possible nodes!");
					return null;
				}
				goal = result[UnityEngine.Random.Range(0, result.Count)];
				if (start == goal)
					return [];
			}
			if (startNumber == goalNumber && startRoom.ConnectivityCost(startNode, goalNode, creatureType) >= 0)
				return [goal];
			for (int i = 0; i < startRoom.connections.Length; ++i)
				if (startRoom.connections[i] == goalNumber)
				{
					int exitIndex = goalRoom.ExitIndex(startNumber);
					if (exitIndex == -1
					 || startNode != i && startRoom.ConnectivityCost(startNode, i, creatureType) < 0f
					 || exitIndex != goalNode && goalRoom.ConnectivityCost(exitIndex, goalNode, creatureType) < 0f)
						break;
					if (goalNode != exitIndex)
						result.Add(new(goalNumber, -1, -1, goalNode));
					result.Add(new(goalNumber, -1, -1, exitIndex));
					if (startNode != i)
						result.Add(new(startNumber, -1, -1, i));
					return result;
				}
			start.Tile = goal.Tile = new(-1, -1);
			List<AbstractSpacePathFinder.Node> checkNext = [new(goal, null, 0)];
			bool[,] alreadyChecked = new bool[world.NumberOfRooms, world.mostNodesInARoom];
			AbstractSpacePathFinder.Node foundStart = null!;
			int attempts = 0, firstRoomIndex = -world.firstRoomIndex;
			PathCost[] resistance = creatureType.pathingPreferencesConnections;
			bool[] allowedNodes = creatureType.mappedNodeTypes;
			float
			  offscreenRes = resistance[(int)MovementConnection.MovementType.OffScreenMovement].resistance,
			  shortcutRes = resistance[(int)MovementConnection.MovementType.ShortCut].resistance,
			  sideRes = resistance[(int)MovementConnection.MovementType.SideHighway].resistance,
			  skyRes = resistance[(int)MovementConnection.MovementType.SkyHighway].resistance,
			  seaRes = resistance[(int)MovementConnection.MovementType.SeaHighway].resistance,
			  roomRes = resistance[(int)MovementConnection.MovementType.BetweenRooms].resistance;
			bool
				shortcutDisallowed = !creatureType.mappedNodeTypes[0],
				sideDisallowed = !allowedNodes[(int)AbstractRoomNode.Type.SideExit] || !resistance[(int)MovementConnection.MovementType.SideHighway].Allowed,
				skyDisallowed = !allowedNodes[(int)AbstractRoomNode.Type.SkyExit] || !resistance[(int)MovementConnection.MovementType.SkyHighway].Allowed,
				seaDisallowed = !allowedNodes[(int)AbstractRoomNode.Type.SeaExit] || !resistance[(int)MovementConnection.MovementType.SeaHighway].Allowed,
				regionDisallowed = !allowedNodes[(int)AbstractRoomNode.Type.RegionTransportation] || !resistance[(int)MovementConnection.MovementType.RegionTransportation].Allowed,
				// Enable extra optimization if start is offscreen den (and check just in case that no nodes were added, idk why)
				offscreenDenOptimization = startRoom.offScreenDen && startRoom.nodes.Length == 5,
				sideFound = sideDisallowed, skyFound = skyDisallowed, seaFound = seaDisallowed, regionFound = regionDisallowed;
			while (checkNext.Count != 0)
			{
				AbstractSpacePathFinder.Node current;
				int lcIndex = 0;
				for (int i = 1; i < checkNext.Count; ++i)
					if (checkNext[i].costToGoal < checkNext[lcIndex].costToGoal)
						lcIndex = i; // go to node with lowest cost
				current = checkNext[lcIndex];
				checkNext.RemoveAt(lcIndex);
				AbstractRoom room = world.abstractRooms[current.pos.room + firstRoomIndex];
				for (int i = 0; i < room.nodes.Length; ++i)
					if (i != current.pos.abstractNode)
					{
						WorldCoordinate worldCoordinate = new(room.index, -1, -1, i);
						ref bool visited = ref alreadyChecked[room.index + firstRoomIndex, i];
						if (visited)
							continue;
						float cost = room.ConnectivityCost(i, current.pos.abstractNode, creatureType) * offscreenRes;
						if (cost >= 0f && AddNode(ref worldCoordinate, ref start, current, cost, ref checkNext, ref foundStart, ref visited, owner))
							goto _found_path;
					}
				if (nodeTypes.TryGetValue(room.nodes[current.pos.abstractNode].type, out int type))
					switch (type)
					{
						case 0:
							if (shortcutDisallowed)
								break;
							int connection = room.connections[current.pos.abstractNode];
							if (connection > -1 && current.pos.abstractNode < room.connections.Length)
							{
								WorldCoordinate worldCoordinate = new(connection, -1, -1, world.abstractRooms[connection + firstRoomIndex].ExitIndex(room.index));
								ref bool visited = ref alreadyChecked[worldCoordinate.room + firstRoomIndex, worldCoordinate.abstractNode];
								if (visited)
									continue;
								float num4 = world.TotalShortCutLengthBetweenTwoConnectedRooms(room.index, connection) * shortcutRes;
								if (num4 > 0f && AddNode(ref worldCoordinate, ref start, current, num4 + roomRes, ref checkNext, ref foundStart, ref visited, owner))
									goto _found_path;
							}
							break;
						case 1:
							if (sideDisallowed)
								break;
							for (int i = 0; i < world.sideAccessNodes.Length; ++i)
							{
								if (world.sideAccessNodes[i].CompareDisregardingTile(current.pos))
									continue;
								WorldCoordinate worldCoordinate = world.sideAccessNodes[i];
								ref bool visited = ref alreadyChecked[worldCoordinate.room + firstRoomIndex, worldCoordinate.abstractNode];
								if (visited)
									continue;
								float num4 = world.SideHighwayDistanceBetweenNodes(current.pos, worldCoordinate) * sideRes;
								if (num4 > 0f && AddNode(ref worldCoordinate, ref start, current, num4 + roomRes, ref checkNext, ref foundStart, ref visited, owner))
									goto _found_path;
								if (!offscreenDenOptimization || sideFound || worldCoordinate.room != startNumber)
									continue;
								sideFound = true;
								if (skyFound && seaFound && regionFound)
									goto _offscreen_all_found;
							}
							break;
						case 2:
							if (skyDisallowed)
								break;
							for (int i = 0; i < world.skyAccessNodes.Length; ++i)
							{
								if (world.skyAccessNodes[i].CompareDisregardingTile(current.pos))
									continue;
								WorldCoordinate worldCoordinate = world.skyAccessNodes[i];
								ref bool visited = ref alreadyChecked[worldCoordinate.room + firstRoomIndex, worldCoordinate.abstractNode];
								if (visited)
									continue;
								float num4 = world.SkyHighwayDistanceBetweenNodes(current.pos, worldCoordinate) * skyRes;
								if (num4 > 0f && AddNode(ref worldCoordinate, ref start, current, num4 + roomRes, ref checkNext, ref foundStart, ref visited, owner))
									goto _found_path;
								if (!offscreenDenOptimization || skyFound || worldCoordinate.room != startNumber)
									continue;
								skyFound = true;
								if (sideFound && seaFound && regionFound)
									goto _offscreen_all_found;
							}
							break;
						case 3:
							if (seaDisallowed)
								break;
							for (int i = 0; i < world.seaAccessNodes.Length; ++i)
							{
								if (world.seaAccessNodes[i].CompareDisregardingTile(current.pos))
									continue;
								WorldCoordinate worldCoordinate = world.seaAccessNodes[i];
								ref bool visited = ref alreadyChecked[worldCoordinate.room + firstRoomIndex, worldCoordinate.abstractNode];
								if (visited)
									continue;
								float num4 = world.SeaHighwayDistanceBetweenNodes(current.pos, worldCoordinate) * seaRes;
								if (num4 > 0f && AddNode(ref worldCoordinate, ref start, current, num4 + roomRes, ref checkNext, ref foundStart, ref visited, owner))
									goto _found_path;
								if (!offscreenDenOptimization || seaFound || worldCoordinate.room != startNumber)
									continue;
								seaFound = true;
								if (sideFound && skyFound && regionFound)
									goto _offscreen_all_found;
							}
							break;
						case 4:
							if (regionDisallowed)
								break;
							for (int i = 0; i < world.regionAccessNodes.Length; ++i)
							{
								if (world.regionAccessNodes[i].CompareDisregardingTile(current.pos))
									continue;
								WorldCoordinate worldCoordinate = world.regionAccessNodes[i];
								ref bool visited = ref alreadyChecked[worldCoordinate.room + firstRoomIndex, worldCoordinate.abstractNode];
								if (visited)
									continue;
								float num4 = world.RegionTransportationDistanceBetweenNodes(current.pos, worldCoordinate) * sideRes;
								if (num4 > 0f && AddNode(ref worldCoordinate, ref start, current, num4 + roomRes, ref checkNext, ref foundStart, ref visited, owner))
									goto _found_path;
								if (!offscreenDenOptimization || regionFound || worldCoordinate.room != startNumber)
									continue;
								regionFound = true;
								if (sideFound && skyFound && seaFound)
									goto _offscreen_all_found;
							}
							break;
					}
				if (++attempts <= 500)
					continue;
				UnityEngine.Debug.LogWarning("EMERGENCY EXIT AbstractSpacePathFinder after " + attempts + " generations!");
				return null;
			_offscreen_all_found:
				// No need to check other nodes, all ways to offscreen den are found and can't be changed
				checkNext.RemoveAll(node => node.pos.room != start.room);
			}
			if (foundStart == null)
			{
				if (!ModManager.DevTools
				  || ModManager.Expedition && world.game.rainWorld.ExpeditionMode && (ExpeditionGame.activeUnlocks.Contains("bur-hunted") || ExpeditionGame.activeUnlocks.Contains("bur-pursued")))
					return null;
				UnityEngine.Debug.LogWarning("Abstract path finder has given up after " + attempts + " generations");
				UnityEngine.Debug.LogWarning($"Tried to find path between r:{startRoom.name}, n:{startNode} and r:{goalRoom.name}, n:{goalNode}");
				return null;
			}
		_found_path:
			AbstractSpacePathFinder.Node node4 = foundStart!;
			result.Add(node4.pos);
			while (!node4.pos.Equals(goal))
			{
				result.Add((node4 = node4.oneStepCloserToGoal).pos);
			}
			result.Reverse();
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AddNode(ref WorldCoordinate pos, ref WorldCoordinate start, AbstractSpacePathFinder.Node parent, float cost, ref List<AbstractSpacePathFinder.Node> checkNext, ref AbstractSpacePathFinder.Node foundStart, ref bool visited, IOwnAnAbstractSpacePathFinder owner)
		{
			if (owner != null)
				cost += owner.CostAddOfNode(pos);
			if (pos.abstractNode == start.abstractNode && pos.room == start.room)
			{
				foundStart = new(pos, parent, parent.costToGoal + (int)cost);
				return true;
			}
			visited = true;
			checkNext.Add(new(pos, parent, parent.costToGoal + (int)cost));
			return false;
		}
	}
}
