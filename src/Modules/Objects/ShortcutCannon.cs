using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DevInterface;
using IL.MoreSlugcats;
using UnityEngine;
using static IL.PlacedObject;

namespace RegionKit.Modules.Objects;


internal class ShortcutCannon : UpdatableAndDeletable, INotifyWhenRoomIsReady
{
	private static ConditionalWeakTable<Room, List<ShortcutCannon>> _shortcutCannonsInRoom = new();

	public static List<ShortcutCannon> GetShortcutCannons(Room room) => _shortcutCannonsInRoom.GetValue(room, _ => new());

	public static bool TryGetSuperShortcut(Room room, IntVector2 pos, out ShortcutCannon shortcut)
	{
		shortcut = null;
		foreach (ShortcutCannon ss in GetShortcutCannons(room))
		{
			if (room.GetTilePosition(ss.pObj.pos) == pos)
			{
				shortcut = ss;
				return true;
				
			}
		}
		return false;
	}

	public static void Apply()
	{
		On.ShortcutHelper.Update += ShortcutHelper_Update;
		On.ShortcutGraphics.GenerateSprites += ShortcutGraphics_GenerateSprites;
	}

	public static void Undo()
	{
		On.ShortcutHelper.Update -= ShortcutHelper_Update;
		On.ShortcutGraphics.GenerateSprites -= ShortcutGraphics_GenerateSprites;
	}

	private static void ShortcutGraphics_GenerateSprites(On.ShortcutGraphics.orig_GenerateSprites orig, ShortcutGraphics self)
	{
		orig(self);
		for (int l = 0; l < self.room.shortcuts.Length; l++)
		{
			if (TryGetSuperShortcut(self.room, self.room.shortcuts[l].StartTile, out ShortcutCannon superShortcut))
			{
				self.entranceSprites[l, 0].element = Futile.atlasManager.GetElementWithName($"Shortcutcannon_Symbol_{superShortcut.amount}flip");

				int endIndex = Array.IndexOf(self.room.shortcutsIndex, self.room.shortcuts[l].DestTile);
				if (endIndex != -1 && self.room.shortcuts[l].shortCutType == ShortcutData.Type.Normal)
				{ self.entranceSprites[endIndex, 0].element = Futile.atlasManager.GetElementWithName($"Shortcutcannon_Symbol_{superShortcut.amount}"); }
			}
		}
	}

	private static void ShortcutHelper_Update(On.ShortcutHelper.orig_Update orig, ShortcutHelper self, bool eu)
	{
		Dictionary<BodyChunk, Vector2[]> UpdatePos = new();

		foreach (AbstractCreature absPlayer in self.room.game.Players)
		{
			if (absPlayer.realizedCreature != null && absPlayer.realizedCreature.room == self.room && absPlayer.realizedCreature.Consious && absPlayer.realizedCreature.grabbedBy.Count == 0)
			{
				Player player = absPlayer.realizedCreature as Player;
				IntVector2 playerInput = new IntVector2(player.input[0].x, player.input[0].y);

				foreach (ShortcutHelper.ShortcutPusher pusher in self.pushers)
				{
					if (!TryGetSuperShortcut(self.room, pusher.shortCutPos, out ShortcutCannon shortcutCannon))
					{ continue; }

					bool locked = self.room.lockedShortcuts.Contains(pusher.shortCutPos);

					if ((locked) && (!pusher.floor || player.GoThroughFloors) && (pusher.shortcutDir.y <= 0 || (!(player.animation == Player.AnimationIndex.BellySlide) && !(player.animation == Player.AnimationIndex.DownOnFours))))
					{
						bool holdAway = (playerInput.x != 0 && playerInput.x == -pusher.shortcutDir.x) || (playerInput.y != 0 && playerInput.y == -pusher.shortcutDir.y);
						bool jumping = player.input[0].jmp || player.jumpBoost > 0f || pusher.validNeighbors.Count > 0;
						
						for (int k = 0; k < player.bodyChunks.Length; k++)
						{
							if (holdAway && player.input[0].jmp && !player.input[1].jmp && Custom.DistLess(pusher.pushPos, player.bodyChunks[k].pos, 30f + player.bodyChunks[k].rad))
							{
								//booster
								//player.bodyChunks[k].vel = Vector2.Lerp(player.bodyChunks[k].vel, pusher.shortcutDir.ToVector2() * 6f + new Vector2(0f, (pusher.shortcutDir.x != 0) ? 6f : 0f), 0.5f);
							}
							else if (jumping)
							{
								/*
								float num = 20f + player.bodyChunks[k].rad + Custom.LerpMap(pusher.swell, 0.5f, 1f, -5f, 10f, 3f) - ((playerInput.y != 0 && playerInput.y == -pusher.shortcutDir.y) ? 5f : 0f);
								pusher.swellUp = (Custom.DistLess(pusher.pushPos, player.bodyChunks[k].pos, Mathf.Max(20f + player.bodyChunks[k].rad, num - 1f)) && holdAway);
								if (Custom.DistLess(pusher.pushPos, player.bodyChunks[k].pos, num))
								{
									float num2 = Mathf.InverseLerp(num - (holdAway ? 2.5f : 5f), num - 20f, Vector2.Distance(pusher.pushPos, player.bodyChunks[k].pos));

									player.bodyChunks[k].vel *= Mathf.Lerp(1f, 0.5f, num2);
									BodyChunk bodyChunk = player.bodyChunks[k];
									bodyChunk.vel.y = bodyChunk.vel.y + player.gravity * self.room.gravity * num2;
									player.bodyChunks[k].vel += (Vector2)Vector3.Slerp(Custom.DirVec(pusher.pushPos, player.bodyChunks[k].pos), pusher.shortcutDir.ToVector2(), 0.9f) * (holdAway ? 3f : 0.9f) * num2;
									player.bodyChunks[k].pos += (Vector2)Vector3.Slerp(Custom.DirVec(pusher.pushPos, player.bodyChunks[k].pos), pusher.shortcutDir.ToVector2(), 0.9f) * (holdAway ? 3f : 0.9f) * num2;
									if (holdAway && pusher.shortcutDir.x != 0)
									{
										player.bodyChunks[k].vel.y = Mathf.Lerp(player.bodyChunks[k].vel.y, Mathf.Clamp(player.bodyChunks[k].vel.y, -2f, 20f), 0.75f);
									}
								}*/
							}
							
							float chunkDistToEntrance = 10f + player.bodyChunks[k].rad;
							if (locked)
							{
								//chunkDistToEntrance *= Mathf.InverseLerp(0f, 500f, player.timeSinceSpawned);
							}

							if (Vector2.Distance(player.bodyChunks[k].pos, pusher.pushPos) < chunkDistToEntrance)
							{
								
								if (pusher.shortcutDir.x != 0)
								{
									player.bodyChunks[k].vel.x += (pusher.pushPos.x + chunkDistToEntrance * pusher.shortcutDir.x - player.bodyChunks[k].pos.x) * shortcutCannon.boostMultiplier();
									Vector2 cannonOffset = new Vector2((pusher.pushPos.x + chunkDistToEntrance * pusher.shortcutDir.x - player.bodyChunks[k].pos.x) * shortcutCannon.boostMultiplier(), 0f);
									UpdatePos.Add(player.bodyChunks[k], new Vector2[] { cannonOffset, player.bodyChunks[k].pos });
									
									//upwards correction
									player.bodyChunks[k].vel.y += Math.Abs((pusher.pushPos.x + chunkDistToEntrance * pusher.shortcutDir.x - player.bodyChunks[k].pos.x) * (shortcutCannon.amount * 0.1f * self.room.gravity));
								}
								else
								{
									player.bodyChunks[k].vel.y += (pusher.pushPos.y + chunkDistToEntrance * pusher.shortcutDir.y - player.bodyChunks[k].pos.y) * shortcutCannon.boostMultiplier();

									Vector2 cannonOffset = new Vector2(0f, (pusher.pushPos.y + chunkDistToEntrance * pusher.shortcutDir.y - player.bodyChunks[k].pos.y) * shortcutCannon.boostMultiplier());
									UpdatePos.Add(player.bodyChunks[k], new Vector2[] { cannonOffset, player.bodyChunks[k].pos });
								}
							}
						}
					}
				}
			}
		}

		orig(self, eu);

		if (UpdatePos.Count <= 0) return;

		foreach (KeyValuePair<BodyChunk, Vector2[]> chunk in UpdatePos)
		{ chunk.Key.pos += chunk.Value[0]; }

		while (UpdatePos.Count > 0)
		{
			//find fastest chunk in owner
			KeyValuePair<BodyChunk, Vector2[]> fastestChunk = UpdatePos.ElementAt(0);
			foreach (BodyChunk chunk2 in fastestChunk.Key.owner.bodyChunks.ToArray())
			{
				if ((chunk2.vel.magnitude > fastestChunk.Key.vel.magnitude) && UpdatePos.ContainsKey(chunk2))
				{ fastestChunk = new(chunk2, UpdatePos[chunk2]); }
			}

			//remove chunks from update
			foreach (BodyChunk chunk2 in fastestChunk.Key.owner.bodyChunks)
			{
				if (UpdatePos.ContainsKey(chunk2))
				{ UpdatePos.Remove(chunk2); }
			}

			if (fastestChunk.Key.owner is Player player && player.bodyChunks.Length == 2 && 
				player.bodyChunks.IndexOf(fastestChunk.Key) != 0)
			{
				//swap positions
				(player.bodyChunks[0].vel, player.bodyChunks[1].vel) = (player.bodyChunks[1].vel, player.bodyChunks[0].vel);
				(player.bodyChunks[0].pos, player.bodyChunks[1].pos) = (player.bodyChunks[1].pos, player.bodyChunks[0].pos);
			}
		}

			//proper fix, but has strange results
			/*while (UpdatePos.Count > 0)
			{
				KeyValuePair<BodyChunk, Vector2[]> fastestChunk = UpdatePos.ElementAt(0);

				//locate fastest chunk that was boosted
				foreach (BodyChunk chunk2 in fastestChunk.Key.owner.bodyChunks.ToArray())
				{
					if ((chunk2.vel.magnitude > fastestChunk.Key.vel.magnitude) && UpdatePos.ContainsKey(chunk2))
					{ fastestChunk = new(chunk2, UpdatePos[chunk2]); }
				}

				foreach (BodyChunk chunk2 in fastestChunk.Key.owner.bodyChunks)
				{
					if (UpdatePos.ContainsKey(chunk2))
					{ 
						chunk2.pos += chunk2.pos - UpdatePos[chunk2][1]; //remove previous adjustment
						UpdatePos.Remove(chunk2);
					}

					chunk2.pos += fastestChunk.Value[0] + (fastestChunk.Value[1] - fastestChunk.Key.pos);
					chunk2.vel = fastestChunk.Key.vel;
				}

				Debug.Log($"bodychunk pos");
				int m = 0;
				foreach (BodyChunk chunk in fastestChunk.Key.owner.bodyChunks)
				{
					Debug.Log($"[{m}] [{chunk.pos}] [{chunk.vel}]");
					m++;
				}
			}*/
		}

	public PlacedObject pObj;

	public int prevAmount;

	public int amount => (pObj.data as shortcutCannonData)!.boost;

	public float boostMultiplier()
	{
		return amount switch
		{
			1 => -0.55f,
			2 => -0.1f,
			3 => 0.7f,
			4 => 2.5f,
			_ => 0f,
		};
	}

	public int shortcutIndex = -1;

	public bool IsOnShortcut() => room.GetTile(pObj.pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance;

	public void AssignIndex()
	{
		shortcutIndex = -1;
		if (!IsOnShortcut()) { return; }

		for (int i = 0; i < room.shortcuts.Length; i++)
		{
			if (room.GetTilePosition(pObj.pos) == room.shortcuts[i].StartTile)
			{
				shortcutIndex = i;
				break;
			}
		}
	}

	public void ResetGraphicsAndLocks()
	{
		if (shortcutIndex == -1) return;
		foreach (RoomCamera camera in room.game.cameras)
		{
			if (camera.room == room)
			{
				foreach (FSprite fsprite in camera.shortcutGraphics.entranceSprites)
				{ fsprite?.RemoveFromContainer(); }

				foreach (FSprite fsprite in camera.shortcutGraphics.sprites.Values)
				{ fsprite?.RemoveFromContainer(); }

				camera.shortcutGraphics.GenerateSprites(); 
			}
		}
		room.lockedShortcuts.Remove(room.shortcuts[shortcutIndex].StartTile);
	}

	public void UpdateGraphics()
	{
		if (shortcutIndex == -1) return;
		foreach (RoomCamera camera in room.game.cameras)
		{
			if (camera.room == room)
			{
				camera.shortcutGraphics.entranceSprites[shortcutIndex, 0].element = Futile.atlasManager.GetElementWithName($"Shortcutcannon_Symbol_{amount}flip");
				
				int endIndex = Array.IndexOf(room.shortcutsIndex, room.shortcuts[shortcutIndex].DestTile);
				if (endIndex != -1 && room.shortcuts[shortcutIndex].shortCutType == ShortcutData.Type.Normal)
				{ camera.shortcutGraphics.entranceSprites[endIndex, 0].element = Futile.atlasManager.GetElementWithName($"Shortcutcannon_Symbol_{amount}"); }
			}
		}
	}

	public ShortcutCannon(PlacedObject pObj, Room room)
	{
		this.pObj = pObj;
		this.room = room;
		prevAmount = amount;
		if (!GetShortcutCannons(room).Contains(this))
		{ GetShortcutCannons(room).Add(this); }
	}

	public void AIMapReady()
	{ }

	public void ShortcutsReady()
	{ Refresh(); }

	public void Refresh()
	{
		if (!IsOnShortcut() && shortcutIndex != -1)
		{
			ResetGraphicsAndLocks();
			shortcutIndex = -1;
		}

		int prevIndex = shortcutIndex;
		AssignIndex();

		if (prevIndex != shortcutIndex || prevAmount != amount)
		{ 
			ResetGraphicsAndLocks();

			prevAmount = amount;

			if (shortcutIndex != -1)
			{ room.lockedShortcuts.Add(room.GetTilePosition(pObj.pos)); }
		}
	}
}

internal class shortcutCannonData : ManagedData
{
	[IntegerField("boost", 1, 4, 2)]
	public int boost;
	public shortcutCannonData(PlacedObject po) : base(po, new ManagedField[]{ })
	{
	}
}

internal class ShortcutCannonRepresentation : ManagedRepresentation
{

	ShortcutCannon? shortcutCannon;

	private int tileSprite = -1;

	public ShortcutCannonRepresentation(PlacedObject.Type placedType, ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
	{
		if (shortcutCannon == null)
		{
			for (int i = 0; i < owner.room.updateList.Count; i++)
			{
				if (owner.room.updateList[i] is ShortcutCannon && (owner.room.updateList[i] as ShortcutCannon)!.pObj == pObj)
				{
					shortcutCannon = (owner.room.updateList[i] as ShortcutCannon);
					break;
				}
			}
		}

		if (shortcutCannon == null)
		{
			shortcutCannon = new ShortcutCannon(pObj, owner.room);
			owner.room.AddObject(shortcutCannon);
		}

		fSprites.Add(new FSprite("pixel", true));
		tileSprite = fSprites.Count - 1;
		fSprites[tileSprite].scale = 20f;
		fSprites[tileSprite].alpha = 0.4f;
		owner.placedObjectsContainer.AddChild(fSprites[tileSprite]);
	}

	public override void Refresh()
	{
		base.Refresh();
		shortcutCannon?.Refresh();

		if (tileSprite > -1)
		{
			fSprites[tileSprite].x = owner.room.MiddleOfTile(pObj.pos).x - owner.room.game.cameras[0].pos.x;
			fSprites[tileSprite].y = owner.room.MiddleOfTile(pObj.pos).y - owner.room.game.cameras[0].pos.y;
		}

		if (shortcutCannon != null && shortcutCannon.IsOnShortcut())
		{ fSprites[tileSprite].color = Color.green; }

		else
		{ fSprites[tileSprite].color = Color.red; }
	}
}
