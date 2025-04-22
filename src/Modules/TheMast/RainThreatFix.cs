using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast;

internal static class RainThreatFix
{
	private static float[]? __rainThreats;

	public static void Apply()
	{
		On.RainTracker.Utility += RainTracker_Utility;
		On.WorldLoader.LoadAbstractRoom += WorldLoader_LoadAbstractRoom;
		On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
		On.AbstractRoom.AttractionForCreature_Type += AbstractRoom_AttractionForCreature;
		On.PathFinder.CheckConnectionCost += PathFinder_CheckConnectionCost;
	}

	public static void Undo()
	{
		On.RainTracker.Utility -= RainTracker_Utility;
		On.WorldLoader.LoadAbstractRoom -= WorldLoader_LoadAbstractRoom;
		On.OverWorld.LoadFirstWorld -= OverWorld_LoadFirstWorld;
		On.AbstractRoom.AttractionForCreature_Type -= AbstractRoom_AttractionForCreature;
		On.PathFinder.CheckConnectionCost -= PathFinder_CheckConnectionCost;
	}

	private static PathCost PathFinder_CheckConnectionCost(On.PathFinder.orig_CheckConnectionCost orig, PathFinder self, PathFinder.PathingCell start, PathFinder.PathingCell goal, MovementConnection connection, bool followingPath)
	{
		PathCost cost = orig(self, start, goal, connection, followingPath);
		if (self.world?.region?.name != "TM") return cost;
		float goalThreat = GetRainThreat(goal.worldCoordinate.room, out bool hasThreat);
		if (hasThreat && (goalThreat > 0.3f))
		{
			if (cost.legality >= PathCost.Legality.Unwanted) cost.resistance += 100f;
			else cost.legality = PathCost.Legality.Unwanted;
		}
		return cost;
	}

	private static AbstractRoom.CreatureRoomAttraction AbstractRoom_AttractionForCreature(On.AbstractRoom.orig_AttractionForCreature_Type orig, AbstractRoom self, CreatureTemplate.Type tp)
	{
		AbstractRoom.CreatureRoomAttraction attract = orig(self, tp);
		if (attract == AbstractRoom.CreatureRoomAttraction.Forbidden || self.world?.region?.name != "TM") return attract;

		float threat = GetRainThreat(self, out bool hasThreat);
		if (hasThreat && (threat > 0.1f) && self.world.rainCycle.TimeUntilRain < 1500f)
			attract = AbstractRoom.CreatureRoomAttraction.Avoid;

		return attract;
	}

	private static void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
	{
		Region lastRegion = self.regions[self.regions.Length - 1];
		int totalRooms = lastRegion.firstRoomIndex + lastRegion.numberOfRooms;
		__rainThreats = new float[totalRooms];
		for (int i = 0; i < totalRooms; i++)
			__rainThreats[i] = -1f;
		orig(self);
	}

	// Calculate the rain threat of a room
	private static void WorldLoader_LoadAbstractRoom(On.WorldLoader.orig_LoadAbstractRoom orig, World world, string roomName, AbstractRoom room, RainWorldGame.SetupValues setupValues)
	{
		orig(world, roomName, room, setupValues);
		if (__rainThreats == null) return;
		int roomInd = room.index;
		if (roomInd >= 0 && roomInd < __rainThreats.Length && (world.name == "TM") && __rainThreats[roomInd] == -1f)
		{
			RoomSettings settings = new RoomSettings(roomName, world.region, false, false, world.game.TimelinePoint, world.game);
			if (settings.DangerType == RoomRain.DangerType.None)
				__rainThreats[roomInd] = 0f;
			else
				__rainThreats[roomInd] = Mathf.InverseLerp(0f, 0.3f, settings.RainIntensity + Mathf.Clamp01(settings.RumbleIntensity - 0.2f));
		}
	}

	public static float GetRainThreat(AbstractRoom room) => GetRainThreat(room.index, out _);
	public static float GetRainThreat(AbstractRoom room, out bool hasThreat) => GetRainThreat(room.index, out hasThreat);
	public static float GetRainThreat(int room) => GetRainThreat(room, out _);
	public static float GetRainThreat(int room, out bool hasThreat)
	{
		hasThreat = false;
		if (__rainThreats == null) return -1;
		if (room < 0 || room >= __rainThreats.Length) return 1f;
		if (__rainThreats[room] == -1) return 1f;
		hasThreat = true;
		return __rainThreats[room];
	}

	private static float RainTracker_Utility(On.RainTracker.orig_Utility orig, RainTracker self)
	{
		float utility = orig(self);
		utility *= GetRainThreat(self.AI.lastRoom);
		return utility;
	}
}
