using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DevInterface;
using MonoMod.Cil;
using OverseerHolograms;
using Mono.Cecil.Cil;

namespace RegionKit.Modules.CustomProjections;


public class CustomDoorPointer : ReliableIggyDirection
{
	public static void Apply()
	{
		IL.Overseer.TryAddHologram += Overseer_TryAddHologram;
		IL.OverseerCommunicationModule.ReevaluateConcern += OverseerCommunicationModule_ReevaluateConcern;
	}

	public static void Undo()
	{
		IL.Overseer.TryAddHologram -= Overseer_TryAddHologram;
		IL.OverseerCommunicationModule.ReevaluateConcern -= OverseerCommunicationModule_ReevaluateConcern;
	}

	private static void OverseerCommunicationModule_ReevaluateConcern(ILContext il)
	{
		var c = new ILCursor(il);

		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdfld<OverseerCommunicationModule>(nameof(OverseerCommunicationModule.currentConcernWeight)),
			x => x.MatchLdcR4(0.98f)))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((float orig, OverseerCommunicationModule self) => (self.forcedDirectionToGive is CustomDoorPointer dp) ? dp.data.ConcernWeight : orig);
		}
		else
		{
			LogMessage("failed to il hook OverseerCommunicationModule.ReevaluateConcern");
		}
	}

	private static void Overseer_TryAddHologram(ILContext il)
	{
		var c = new ILCursor(il);

		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdarg(1),
			x => x.MatchLdsfld<OverseerHologram.Message>(nameof(OverseerHologram.Message.ForcedDirection)),
			x => x.MatchCall(typeof(ExtEnum<OverseerHologram.Message>).GetMethod("op_Equality"))
			))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldarg_2);
			c.Emit(OpCodes.Ldarg_3);
			c.EmitDelegate((bool orig, Overseer self, OverseerHologram.Message message, Creature communicateWith, float importance) =>
			{
				if (orig == true && self.AI.communication.forcedDirectionToGive is CustomDoorPointer)
				{
					self.hologram = new DoorPointerHologram(self, message, communicateWith, importance);
					return false;
				}
				return orig;
			});
		}
		else
		{
			LogMessage("failed to il hook Overseer.TryAddHologram");
		}
	}

	public CustomDoorPointer(PlacedObject placedObject, Room room) : base(placedObject)
	{
		this.room = room;
	}

	public new DoorPointerData data => (pObj.data as DoorPointerData)!;

	public override void Update(bool eu)
	{
		//base.Update(eu);
		this.evenUpdate = eu; //I see nothing bad ever coming from ignoring base

		//if it's not an atlas, don't allow it
		if (!Futile.atlasManager.DoesContainElementWithName(data.Symbol))
		{
			Destroy();
			return;
		}

		if (firstUpdate)
		{
			firstUpdate = false;

			if (data.CyclesToShow > 0 && guideState.HowManyTimesHasForcedDirectionBeenGiven(room.abstractRoom.index) >= data.CyclesToShow)
			{
				Destroy();
				return;
			}
		}

		bool PlayerWithinRange = false;
		for (int i = 0; i < room.game.Players.Count; i++)
		{
			if (
				room.game.Players[i].Room.index == room.abstractRoom.index &&
				room.game.Players[i].realizedCreature != null &&
				(data.PointPlayerBack || room.game.Players[i].pos.abstractNode != data.Exit) &&
				room.game.Players[i].realizedCreature.room == room &&
				Custom.DistLess(pObj.pos, room.game.Players[i].realizedCreature.mainBodyChunk.pos, data.Radius.magnitude)
				)
			{
				PlayerWithinRange = true;
				break;
			}
		}

		Overseer overseer = null!;
		for (int j = 0; j < room.abstractRoom.creatures.Count; j++)
		{
			if (room.abstractRoom.creatures[j].creatureTemplate.type == CreatureTemplate.Type.Overseer &&
				room.abstractRoom.creatures[j].realizedCreature is Overseer seer &&
				room.abstractRoom.creatures[j].realizedCreature.room == room && seer.PlayerGuide)
			{
				overseer = seer;
				break;
			}
		}

		if (PlayerWithinRange)
		{
			if (overseer != null)
			{
				if (!hasBeenActivated)
				{ Activate(overseer); }
				else
				{ overseer.AI.communication.forcedDirectionToGive = this; }
			}
			else
			{ BringGuideToPlayer(); }
		}
	}

	public new void Activate(Overseer guide) //if you remove guide make sure to update
	{
		hasBeenActivated = true;
		if (data.CyclesToShow > 0)
		{
			guideState.IncrementTimesForcedDirectionHasBeenGiven(room.abstractRoom.index);
		}
	}
}

public class DoorPointerData : ManagedData
{
#pragma warning disable
	[Vector2Field("radius", 50f, 0f, Vector2Field.VectorReprType.circle)]
	public Vector2 Radius;

	[DoorPointerRep.AtlasStringField("sprite", "GuidanceSlugcat", "Symbol")]
	public string Symbol;

	[BooleanField("Point Player Back", false, ManagedFieldWithPanel.ControlType.button, "Point Player Back")]
	public bool PointPlayerBack;

	[IntegerField("exit", -1, 20, 0, ManagedFieldWithPanel.ControlType.arrows, "exit")]
	public int Exit;

	[IntegerField("cycles to show", 0, 9, 0, ManagedFieldWithPanel.ControlType.arrows, "Cycles to show")]
	public int CyclesToShow;

	[FloatField("concernweight", 0f, 1f, 0.98f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "ConcernWeight")]
	public float ConcernWeight;
#pragma warning restore

	public DoorPointerData(PlacedObject owner) : base(owner, null) { }
}


public class DoorPointerRep : ManagedRepresentation
{
	public DoorPointerRep(PlacedObject.Type placedType, ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
	{
		//adding line sprite to show which door
		exitSprite = new FSprite("pixel", true)
		{
			anchorY = 0f,
			scaleX = 2f,
			color = new Color(1f, 0f, 0f)
		};

		fSprites.Add(exitSprite);
		owner.placedObjectsContainer.AddChild(exitSprite);
	}

	public DoorPointerData data => (pObj.data as DoorPointerData)!;

	public override void Refresh()
	{
		if (data.Exit < 0)
		{ data.Exit = owner.room.abstractRoom.exits - 1; }

		if (data.Exit >= owner.room.abstractRoom.exits)
		{ data.Exit = 0; }

		base.Refresh();

		if (data.Exit < 0 || data.Exit >= owner.room.abstractRoom.connections.Length)
		{
			exitSprite.isVisible = false;
		}
		else
		{
			exitSprite.isVisible = true;
			Vector2 absPos = panel!.absPos;
			Vector2 vector = owner.room.MiddleOfTile(owner.room.ShortcutLeadingToNode(data.Exit).startCoord) - owner.room.game.cameras[0].pos;
			exitSprite.x = absPos.x;
			exitSprite.y = absPos.y;
			exitSprite.rotation = Custom.AimFromOneVectorToAnother(absPos, vector);
			exitSprite.scaleY = Vector2.Distance(absPos, vector);
		}
	}

	public FSprite exitSprite;

	public class AtlasStringField : StringField
	{
		public AtlasStringField(string key, string defaultValue, string? displayName = null) : base(key, defaultValue, displayName) { }

		public override void ParseFromText(PositionedDevUINode node, ManagedData data, string newValue)
		{
			if (!Futile.atlasManager.DoesContainElementWithName(newValue)) throw new Exception();
			base.ParseFromText(node, data, newValue);
		}
	}
}
