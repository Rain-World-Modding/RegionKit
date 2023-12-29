using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DevInterface;

namespace RegionKit.Modules.Objects;


internal class GuardProtectNode : UpdatableAndDeletable, INotifyWhenRoomIsReady
{
	public static void Apply()
	{
		On.TempleGuardAI.NewRoom += TempleGuardAI_NewRoom;
		On.TempleGuardAI.ThrowOutScore += TempleGuardAI_ThrowOutScore;
	}
	public static void Undo()
	{
		On.TempleGuardAI.NewRoom -= TempleGuardAI_NewRoom;
		On.TempleGuardAI.ThrowOutScore -= TempleGuardAI_ThrowOutScore;
	}

	private static float TempleGuardAI_ThrowOutScore(On.TempleGuardAI.orig_ThrowOutScore orig, TempleGuardAI self, Tracker.CreatureRepresentation crit)
	{
		var result = orig(self, crit);
		if (result > 0 && ProtectNode.TryGetValue(self, out GuardProtectNode protection))
		{
			float distance = Vector2.Distance(self.guard.room.MiddleOfTile(crit.BestGuessForPosition().Tile), self.guard.room.MiddleOfTile(self.guard.room.LocalCoordinateOfNode(self.protectExit)));
			return protection.distance / ((distance * 0.2f) + (float)crit.TicksSinceSeen / 2f);
		}

		return orig(self, crit);
	}

	static ConditionalWeakTable<TempleGuardAI, GuardProtectNode> ProtectNode = new();


	private static void TempleGuardAI_NewRoom(On.TempleGuardAI.orig_NewRoom orig, TempleGuardAI self, Room room)
	{
		orig(self, room);
		List<GuardProtectNode> protections = new();
		foreach (UpdatableAndDeletable uad in room.updateList)
		{
			if (uad is GuardProtectNode obj && obj.node != -1)
			{
				protections.Add(obj);
			}
		}

		if (protections.Count == 1)
		{
			self.protectExit = protections[0].node;
			ProtectNode.Remove(self);
			ProtectNode.Add(self, protections[0]);
		}

		else
		{
			float i = float.MaxValue;
			foreach (GuardProtectNode protection in protections)
			{
				float num = Vector2.Distance(room.MiddleOfTile(room.LocalCoordinateOfNode(protection.node)), self.guard.mainBodyChunk.pos);
				if (num < i)
				{
					self.protectExit = protection.node;
					ProtectNode.Remove(self);
					ProtectNode.Add(self, protection);
					i = num;
				}
			}
		}
	}

	public void ShortcutsReady()
	{
		for (int i = 0; i < room.abstractRoom.nodes.Length; i++)
		{
			if (room.LocalCoordinateOfNode(i).Tile == room.GetTilePosition(obj.pos) && room.abstractRoom.nodes[i].type == AbstractRoomNode.Type.Exit || room.abstractRoom.nodes[i].type == AbstractRoomNode.Type.Den)
			{ node = i; }
		}
	}

	public void AIMapReady() { }
	PlacedObject obj;
	GuardProtectNode(PlacedObject obj, Room room)
	{
		this.obj = obj;
	}

	int node = -1;
	float distance => (obj.data as GuardProtectData)!.handle.magnitude;
}

internal class GuardProtectData : ManagedData
{
	[Vector2Field("handle", 100f, 0f, Vector2Field.VectorReprType.circle)]
	public Vector2 handle;

	public GuardProtectData(PlacedObject owner) : base(owner, null) { }
}

internal class GuardProtectRepresentation : ManagedRepresentation
{
	private int tileSprite = -1;

	public GuardProtectRepresentation(PlacedObject.Type placedType, ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
	{
		fSprites.Add(new FSprite("pixel", true));
		tileSprite = fSprites.Count - 1;
		fSprites[tileSprite].scale = 20f;
		fSprites[tileSprite].alpha = 0.4f;
		owner.placedObjectsContainer.AddChild(fSprites[tileSprite]);
	}

	public bool IsOnShortcut()
	{
		for (int i = 0; i < owner.room.abstractRoom.nodes.Length; i++)
		{
			if (owner.room.LocalCoordinateOfNode(i).Tile == owner.room.GetTilePosition(pObj.pos) && owner.room.abstractRoom.nodes[i].type == AbstractRoomNode.Type.Exit || owner.room.abstractRoom.nodes[i].type == AbstractRoomNode.Type.Den)
			{ return true; }
		}
		return false;
	}

	public override void Refresh()
	{
		base.Refresh();

		if (tileSprite > -1)
		{
			fSprites[tileSprite].x = owner.room.MiddleOfTile(pObj.pos).x - owner.room.game.cameras[0].pos.x;
			fSprites[tileSprite].y = owner.room.MiddleOfTile(pObj.pos).y - owner.room.game.cameras[0].pos.y;
		}

		if (IsOnShortcut())
		{ fSprites[tileSprite].color = Color.green; }

		else
		{ fSprites[tileSprite].color = Color.red; }
	}
}
