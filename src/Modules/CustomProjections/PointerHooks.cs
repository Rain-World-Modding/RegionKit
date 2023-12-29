using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace RegionKit.Modules.CustomProjections;

internal static class PointerHooks
{
	public static void Apply()
	{
		On.OverseersWorldAI.DirectionFinder.ctor += DirectionFinder_ctor;
		On.OverseerHolograms.OverseerHologram.DirectionPointer.ctor += DirectionPointer_ctor;
		//On.OverseerCommunicationModule.PlayerProgressionNeed += PlayerProgressionNeedHook; //don't need this anymore
		On.OverseerCommunicationModule.PlayerBatsNeed += PlayerBatsNeedHook;
		On.OverseerCommunicationModule.PlayerShelterNeed += PlayerShelterNeedHook;
		On.OverseerCommunicationModule.CreatureDangerScore += CreatureDangerScoreHook;
		On.OverseerCommunicationModule.FoodDelicousScore += FoodDeliciousScoreHook;
	}
	public static void Undo()
	{
		On.OverseersWorldAI.DirectionFinder.ctor -= DirectionFinder_ctor;
		On.OverseerHolograms.OverseerHologram.DirectionPointer.ctor -= DirectionPointer_ctor;
		//On.OverseerCommunicationModule.PlayerProgressionNeed -= PlayerProgressionNeedHook;
		On.OverseerCommunicationModule.PlayerBatsNeed -= PlayerBatsNeedHook;
		On.OverseerCommunicationModule.PlayerShelterNeed -= PlayerShelterNeedHook;
		On.OverseerCommunicationModule.CreatureDangerScore -= CreatureDangerScoreHook;
		On.OverseerCommunicationModule.FoodDelicousScore -= FoodDeliciousScoreHook;
	}

	private static void DirectionPointer_ctor(On.OverseerHolograms.OverseerHologram.DirectionPointer.orig_ctor orig, OverseerHolograms.OverseerHologram.DirectionPointer self, Overseer overseer, OverseerHolograms.OverseerHologram.Message message, Creature communicateWith, float importance)
	{
		orig(self, overseer, message, communicateWith, importance);

		var progressionSymbol = OverseerProperties.GetOverseerProperties(self.room.world.region).ProgressionSymbol;

		if (progressionSymbol != "" && Futile.atlasManager.DoesContainElementWithName(progressionSymbol))
		{
			self.parts.Remove(self.symbol);
			self.totalSprites -= self.symbol.totalSprites;
			self.symbol = new OverseerHolograms.OverseerHologram.Symbol(self, self.totalSprites, progressionSymbol);
			self.AddPart(self.symbol);
		}
	}

	private static float FoodDeliciousScoreHook(On.OverseerCommunicationModule.orig_FoodDelicousScore orig, OverseerCommunicationModule self, AbstractPhysicalObject foodObject, Player player)
	{
		return Math.Min(orig(self, foodObject, player) * OverseerProperties.GetOverseerProperties(self.room.world.region).DeliciousFoodWeight, 1f);
	}

	private static float CreatureDangerScoreHook(On.OverseerCommunicationModule.orig_CreatureDangerScore orig, OverseerCommunicationModule self, AbstractCreature creature, Player player)
	{
		return Math.Min(orig(self, creature, player) * OverseerProperties.GetOverseerProperties(self.room.world.region).DangerousCreatureWeight, 1f);
	}

	private static float PlayerShelterNeedHook(On.OverseerCommunicationModule.orig_PlayerShelterNeed orig, OverseerCommunicationModule self, Player player)
	{
		return Math.Min(orig(self, player) * OverseerProperties.GetOverseerProperties(self.room.world.region).ShelterShowWeight, 1f);
	}

	private static float PlayerBatsNeedHook(On.OverseerCommunicationModule.orig_PlayerBatsNeed orig, OverseerCommunicationModule self, Player player)
	{
		//PlayerProgressionNeed hook... ok I know this is a goofy spot but this is method is called right before the other
		var progressionShowWeight = OverseerProperties.GetOverseerProperties(self.room.world.region).ProgressionShowWeight;

		if (progressionShowWeight < 100)
		{ self.progressionShowTendency *= progressionShowWeight; }
		else
		{ self.progressionShowTendency = progressionShowWeight - 100f; }

		return Math.Min(orig(self, player) * OverseerProperties.GetOverseerProperties(self.room.world.region).BatShowWeight, 1f);
	}

	private static void DirectionFinder_ctor(On.OverseersWorldAI.DirectionFinder.orig_ctor orig, OverseersWorldAI.DirectionFinder self, World world)
	{
		orig(self, world);

		string customDestinationRoom = OverseerProperties.GetOverseerProperties(world.region).CustomDestinationRoom;

		//guard clause
		if (customDestinationRoom == "" || world.GetAbstractRoom(customDestinationRoom) == null)
		{ return; }

		self.destroy = false;

		self.showToRoom = world.GetAbstractRoom(customDestinationRoom).index;

		if (world.GetAbstractRoom(customDestinationRoom).gate)
		{
			self.minKarma = GetLock(customDestinationRoom, world.region.name);
		}

		//these are vanilla Debug.Logs!!!! copy-pasted anyways, leave 'em in for consistency please
		Debug.Log(string.Concat("Custom guide to: ", customDestinationRoom, " karma req: ", self.minKarma));
		if ((world.game.session as StoryGameSession)?.saveState.deathPersistentSaveData.karma < self.minKarma && self.DestinationRoomVisisted)
		{
			Debug.Log("Custom progression direction founder killed b/c low karma and have been to destination");
			self.destroy = true;
		}

		AbstractRoom abstractRoom = world.GetAbstractRoom(self.showToRoom);
		for (int m = 0; m < abstractRoom.connections.Length; m++)
		{
			self.checkNext.Add(new RWCustom.IntVector2(abstractRoom.index - world.firstRoomIndex, m));
			self.matrix[abstractRoom.index - world.firstRoomIndex][m] = 0f;
		}

	}

	public static int GetLock(string roomName, string regionName)
	{
		string[] array = File.ReadAllLines(AssetManager.ResolveFilePath(string.Concat(
			"World", Path.DirectorySeparatorChar, "Gates", Path.DirectorySeparatorChar, "locks.txt" )));

		for (int k = 0; k < array.Length; k++)
		{
			if (roomName == Regex.Split(array[k], " : ")[0])
			{
				string[] array2 = Regex.Split(Regex.Split(array[k], " : ")[0], "_");
				int num = -1;
				for (int i = 1; i < array2.Length; i++)
				{
					if (array2[i] == regionName)
					{
						num = i;
						break;
					}
				}
				if (num > 0 && int.TryParse(Regex.Split(array[k], " : ")[num], out var result))
				{
					return result;
				}
			}
		}
		return 0;
	}
}
