using UnityEngine;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// By LB Gamer/M4rbleL1ne
/// </summary>

namespace RegionKit.Modules.Misc;

public static class ArenaFixes
{
	public static void ApplyHK()
	{
		On.ScavengerTreasury.ctor += (orig, self, room, placedObj) =>
		{
			try
			{
				orig(self, room, placedObj);
			}
			catch (NullReferenceException)
			{
				for (var k = 0; k < self.tiles.Count; k++)
				{
					if (Random.value < Mathf.InverseLerp(self.Rad, self.Rad / 5f, Vector2.Distance(room.MiddleOfTile(self.tiles[k]), placedObj.pos)))
					{
						var abstractPhysicalObject = Random.value < .1f ? new DataPearl.AbstractDataPearl(room.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, room.GetWorldCoordinate(self.tiles[k]), room.game.GetNewID(), -1, -1, null, DataPearl.AbstractDataPearl.DataPearlType.Misc) : (Random.value < .142857149f ? new AbstractPhysicalObject(room.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, room.GetWorldCoordinate(self.tiles[k]), room.game.GetNewID()) : ((!(Random.value < 1f / 20f)) ? new AbstractSpear(room.world, null, room.GetWorldCoordinate(self.tiles[k]), room.game.GetNewID(), Random.value < .75f) : new AbstractPhysicalObject(room.world, AbstractPhysicalObject.AbstractObjectType.Lantern, null, room.GetWorldCoordinate(self.tiles[k]), room.game.GetNewID())));
						self.property.Add(abstractPhysicalObject);
						if (abstractPhysicalObject is not null)
							room.abstractRoom.entities.Add(abstractPhysicalObject);
					}
				}
			}
		};
		On.DaddyCorruption.ctor += (orig, self, room) =>
		{
			try
			{
				orig(self, room);
			}
			catch (NullReferenceException)
			{
				self.GWmode = false;
				self.effectColor = Color.blue;
				self.eyeColor = self.effectColor;
			}
		};
	}
}
