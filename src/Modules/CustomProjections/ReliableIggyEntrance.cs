using System;

namespace RegionKit.Modules.CustomProjections;

internal class ReliableIggyEntrance : UpdatableAndDeletable
{
	public static void Apply()
	{
		On.Overseer.PlaceInRoom += Overseer_PlaceInRoom;
	}
	public static void Undo()
	{
		On.Overseer.PlaceInRoom -= Overseer_PlaceInRoom;
	}

	private static void Overseer_PlaceInRoom(On.Overseer.orig_PlaceInRoom orig, Overseer self, Room placeRoom)
	{
		orig(self, placeRoom);

		List<ReliableEntranceData> validOptions = new();

		foreach (PlacedObject pobj in self.room.roomSettings.placedObjects)
		{
			if (pobj.type.value == "ReliableIggyEntrance" && pobj.data is ReliableEntranceData data && data.validTiles.Count > 0)
			{
				validOptions.Add(data);
			}
		}

		if (validOptions.Count > 0)
		{
			var selected = validOptions[UnityEngine.Random.Range(0, validOptions.Count)];
			IntVector2 pos = selected.validTiles.Keys.ToList()[UnityEngine.Random.Range(0, selected.validTiles.Count)];
			self.HardSetTile(pos, selected.validTiles[pos]);
		}
	}

	//constructor for the object - doesn't actually do anything, but POM breaks if it's not included
	public ReliableIggyEntrance(PlacedObject pObj, Room room)
	{
	}
}

public class ReliableEntranceData : ManagedData
{
	[Vector2Field("handle", 100f, 0f, Vector2Field.VectorReprType.circle)]
	public Vector2 handle;

	public Dictionary<IntVector2, IntVector2> validTiles = new();

	public ReliableEntranceData(PlacedObject owner) : base(owner, null)
	{
		validTiles = new();
	}

	public static bool TileIsValid(IntVector2 pos, Room room, out IntVector2 stemPos)
	{
		stemPos = new();

		if (room.GetTile(pos).Solid) return false;

		for (int k = 0; k < 8; k++)
		{
			if (room.GetTile(pos + Custom.eightDirectionsDiagonalsLast[k]).Solid)
			{
				stemPos = pos + Custom.eightDirectionsDiagonalsLast[k];
				return true;
			}
		}
		return false;
	}

	public void GenerateValidTiles(Room room)
	{
		validTiles = new();
		IntVector2 tilePosition = room.GetTilePosition(owner.pos);
		int num = (int)(handle.magnitude / 20f);
		for (int x = tilePosition.x - num; x <= tilePosition.x + num; x++)
		{
			for (int y = tilePosition.y - num; y <= tilePosition.y + num; y++)
			{
				if (Custom.DistLess(room.MiddleOfTile(x, y), owner.pos, handle.magnitude) && TileIsValid(new IntVector2(x, y), room, out var stemPos))
				{
					validTiles.Add(new IntVector2(x, y), stemPos);
				}
			}
		}
	}
}



internal class ReliableEntranceRep : ManagedRepresentation
{
	List<FSprite> sprites = new();

	ReliableEntranceData data;

	public ReliableEntranceRep(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
	{
		data = (pObj.data as ReliableEntranceData)!;

		ResizeSprites(1);
	}


	public void ResizeSprites(int newCount)
	{
		for (int i = sprites.Count; i < newCount; i++)
		{
			var sprite = new FSprite("pixel", true)
			{
				scale = 20f,
				alpha = 0.4f,
				color = Color.green
			};
			sprites.Add(sprite);
			fSprites.Add(sprite);
			owner.placedObjectsContainer.AddChild(sprite);
		}

		while (newCount < sprites.Count)
		{
			sprites[0].RemoveFromContainer();
			fSprites.Remove(sprites[0]);
			sprites.Remove(sprites[0]);
		}
	}

	public override void Refresh()
	{
		base.Refresh();

		data.GenerateValidTiles(owner.room);

		List<IntVector2> tiles = data.validTiles.Keys.ToList();

		ResizeSprites(tiles.Count);

		for (int i = 0; i < sprites.Count && i < tiles.Count; i++)
		{
			sprites[i].x = owner.room.MiddleOfTile(tiles[i]).x - owner.room.game.cameras[0].pos.x;
			sprites[i].y = owner.room.MiddleOfTile(tiles[i]).y - owner.room.game.cameras[0].pos.y;
		}
	}
}
