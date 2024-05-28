using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RegionKit.Extras;

namespace RegionKit.Modules.Objects;

internal class SlipperyZone : UpdatableAndDeletable
{
	private static ConditionalWeakTable<Room.Tile, SlipperyZone?> _IsSlippy = new();
	public static SlipperyZone? GetSlippy(Room.Tile t) => _IsSlippy.GetValue(t, _ => null);
	public static SlipperyZone? GetSlippy(BodyChunk b) => b.owner?.room == null? null : GetSlippy(b.owner.room.GetTile(b.pos + new Vector2(0f, b.contactPoint.y * 20f)));

	public static void ApplyHooks()
	{
		IL.BodyChunk.checkAgainstSlopesVertically += BodyChunk_checkAgainstSlopesVertically;
		IL.Player.MovementUpdate += Player_MovementUpdate;
		IL.Player.UpdateBodyMode += Player_UpdateBodyMode;
		IL.Player.UpdateAnimation += Player_UpdateAnimation;
		//IL.BodyChunk.CheckVerticalCollision += BodyChunk_CheckVerticalCollision;
	}

	public static void Undo()
	{
		IL.BodyChunk.checkAgainstSlopesVertically -= BodyChunk_checkAgainstSlopesVertically;
		IL.Player.MovementUpdate -= Player_MovementUpdate;
		IL.Player.UpdateBodyMode -= Player_UpdateBodyMode;
		IL.Player.UpdateAnimation -= Player_UpdateAnimation;
		//IL.BodyChunk.CheckVerticalCollision -= BodyChunk_CheckVerticalCollision;
	}

	private static void Player_UpdateAnimation(ILContext il)
	{
		var c = new ILCursor(il);
		//slow down roll
		for (int i = 0; i < 2; i++)
		{
			if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdcR4(1.1f),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<Player>(nameof(Player.rollDirection)),
			x => x.MatchConvR4(),
			x => x.MatchMul()
			))
			{
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate((float orig, Player self) => GetSlippy(self.mainBodyChunk) is SlipperyZone zone ? orig * (0.3f + zone.Slippy * 0.7f) : orig);
			}
		}

		//slow down slide boost to not be absurd
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdcR4(39f),
			x => x.MatchDiv(),
			x => x.MatchLdcR4(3.1415927f),
			x => x.MatchMul(),
			x => x.MatchCall(typeof(Mathf).GetMethod(nameof(Mathf.Sin))),
			x => x.MatchMul()
			))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((float orig, Player self) => GetSlippy(self.mainBodyChunk) is SlipperyZone zone ? orig * (0.4f + zone.Slippy * 0.6f) : orig);
		}
	}

	private static void Player_UpdateBodyMode(ILContext il)
	{
		//slow down skid turn
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchCall(typeof(Mathf).GetMethod(nameof(Mathf.Sin))),
			x => x.MatchNeg(),
			x => x.MatchLdcR4(0.5f),
			x => x.MatchAdd()
			))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((float orig, Player self) => GetSlippy(self.mainBodyChunk) is SlipperyZone zone ? orig * zone.Slippy : orig);
		}
	}

	private static void BodyChunk_CheckVerticalCollision(ILContext il)
	{
		//unused currently cuz it feels weird
		var c = new ILCursor(il);
		while (c.TryGotoNext(MoveType.After,
			x => x.MatchLdfld<PhysicalObject>(nameof(PhysicalObject.surfaceFriction))
			))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((float orig, BodyChunk self) => GetSlippy(self) is SlipperyZone zone ? orig * zone.Slippy : orig);
		}
	}

	private static void Player_MovementUpdate(ILContext il)
	{
		var c = new ILCursor(il);

		//disable corridor climb
		ILLabel label = null!;
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchBrfalse(out label),
			x => x.MatchLdarg(0),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<Player>(nameof(Player.goIntoCorridorClimb)),
			x => x.MatchLdcI4(1),
			x => x.MatchAdd(),
			x => x.MatchStfld<Player>(nameof(Player.goIntoCorridorClimb))
			))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((Player self) => GetSlippy(self.bodyChunks[1]) is SlipperyZone zone && zone.DisableCrawlHoles);
			c.Emit(OpCodes.Brtrue, label);
		}
		else
		{ LogError("failed to il hook Player.MovementUpdate"); }

		//most readable il hook
		//1st for moving left, 2nd for moving right, 3rd for grounded traction\deaccel
		for (int i = 0; i < 3; i++)
		{
			int index = 0;
			if (c.TryGotoNext(MoveType.After,
				x => x.MatchCall<PhysicalObject>("get_bodyChunks"),
				x => x.MatchLdloc(out index),
				x => x.MatchLdelemRef(),
				x => x.MatchLdflda<BodyChunk>(nameof(BodyChunk.vel)),
				x => x.MatchLdflda<Vector2>(nameof(Vector2.x)),
				x => x.MatchDup(),
				x => x.MatchLdindR4(),
				x => x.MatchLdloc(out _)) 
				&& c.TryGotoNext(i == 2 ? MoveType.After : MoveType.Before, i switch
				{
					0 => new Func<Instruction, bool>[]
					{
						x => x.MatchSub(),
						x => x.MatchStindR4(),
						x => x.MatchBr(out _)
					},
					1 => new Func<Instruction, bool>[]
					{
						x => x.MatchAdd(),
						x => x.MatchStindR4(),
						x => x.MatchLdarg(0)
					},
					2 or _ => new Func<Instruction, bool>[]
					{
						x => x.MatchLdflda<BodyChunk>(nameof(BodyChunk.vel)),
						x => x.MatchLdfld<Vector2>(nameof(Vector2.x)),
						x => x.MatchSub(),
						x => x.MatchLdarg(0),
						x => x.MatchLdfld<PhysicalObject>(nameof(PhysicalObject.surfaceFriction))
					}
				}))
			{
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldloc, index);
				c.EmitDelegate((float orig, Player self, int i) => (self.bodyChunks[0].ContactPoint.y != 0 || self.bodyChunks[1].ContactPoint.y != 0)
				&& GetSlippy(self.bodyChunks[i]) is SlipperyZone zone ? orig * zone.Slippy : orig);
			}
			else
			{ LogError("failed to il hook Player.MovementUpdate part " + i); }
		}
	}

	/// <summary>
	/// makes slopes slippy
	/// </summary>
	private static void BodyChunk_checkAgainstSlopesVertically(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdfld<BodyChunk>(nameof(BodyChunk.slopeRad)),
			x => x.MatchAdd(),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<BodyChunk>(nameof(BodyChunk.slopeRad)),
			x => x.MatchAdd()
			))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, 3);
			c.Emit(OpCodes.Ldloc, 4);
			c.EmitDelegate((float orig, BodyChunk self, Vector2 slopePos, int direction) => {
				if (GetSlippy(self) is not SlipperyZone zone || !zone.AffectSlopes) return orig;


				slopePos -= new Vector2(10f, 10f); //set to bottom left
				float offset = direction != 1 ? self.pos.y - slopePos.y : 20f - (self.pos.y - slopePos.y);
				float nextXPos = slopePos.x + offset + (self.slopeRad * 2 * direction);

				/*if (self.owner is Player player && self == player.bodyChunks[1])
				{
					SlipperyZoneDebug.bodyChunkPos = self.pos;
					SlipperyZoneDebug.slopePos.x = nextXPos;
					SlipperyZoneDebug.slopePos.y = orig;
				}*/

				if ((direction == 1 && self.pos.x > nextXPos) || (direction == -1 && self.pos.x < nextXPos)) return orig;
				self.pos.x = nextXPos;
				self.contactPoint.y = -1;
				//self.vel.x = self.vel.x * (1f - self.owner.surfaceFriction);
				//self.vel.x = self.vel.x + Mathf.Abs(self.vel.y) * Mathf.Clamp(0.5f - self.owner.surfaceFriction, 0f, 0.5f) * (float)direction * 0.2f;
				//self.vel.y = 0f;
				//self.vel.x += direction * 0.1f;
				//self.onSlope = direction;
				self.slopeRad = self.TerrainRad - 1f;

				return float.NegativeInfinity; //don't run orig
			});
		}
		else { LogError("failed to il hook BodyChunk.checkAgainstSlopesVertically"); }
	}

	public SlipperyZone(PlacedObject pObj, Room room) 
	{
		//room.AddObject(new SlipperyZoneDebug());
		IntVector2 originIntVec = new((int)pObj.pos.x / 20, (int)pObj.pos.y / 20);
		IntRect zone = IntRect.MakeFromIntVector2(originIntVec);
		data = (pObj.data as ManagedData)!;
		zone.ExpandToInclude(originIntVec + data.GetValue<IntVector2>("0zone"));
		foreach (IntVector2 pos in RainWorldTools.ReturnTiles(zone))
		{
			if (room.IsPositionInsideBoundries(pos))
				_IsSlippy.GetValue(room.GetTile(pos), _ => this);
		}
	}

	private ManagedData data;

	public float Slippy => data.GetValue<float>("1traction");
	public bool AffectSlopes => data.GetValue<bool>("2slope");
	public bool DisableCrawlHoles => data.GetValue<bool>("3tunnel");

	public class SlipperyZoneDebug : UpdatableAndDeletable, IDrawable
	{
		public static Vector2 bodyChunkPos = new();
		public static Vector2 slopePos = new();
		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			newContatiner ??= rCam.ReturnFContainer("HUD");
			foreach (FSprite sprite in sLeaser.sprites)
			{
				sprite.RemoveFromContainer();
				newContatiner.AddChild(sprite);
			}
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			sLeaser.sprites[0].SetPosition(bodyChunkPos.x - camPos.x, bodyChunkPos.y - camPos.y);
			sLeaser.sprites[1].SetPosition(bodyChunkPos.x - camPos.x, slopePos.y - camPos.y);
			sLeaser.sprites[2].SetPosition(slopePos.x - camPos.x, bodyChunkPos.y - camPos.y);
		}

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[3];
			for (int i = 0; i < 3; i++)
			{
				sLeaser.sprites[i] = new FSprite("pixel");
			}
			sLeaser.sprites[0].color = Color.red;
			sLeaser.sprites[1].color = Color.green;
			sLeaser.sprites[2].color = Color.blue;
			AddToContainer(sLeaser, rCam, null!);
		}
	}
}
