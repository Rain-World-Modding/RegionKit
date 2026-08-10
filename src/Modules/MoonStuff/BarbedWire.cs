using MoreSlugcats;
using Watcher;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class BarbedWire : UpdatableAndDeletable, IDrawable
	{
		public Vector2[] spikes;

		public Vector2 startPos;

		public Vector2 endPos;

		public float health;

		public float breakPoint;

		public bool broken;

		public int BreakCounter = 4;

		public bool Breakable;

		public bool Harmful;

		public PlacedObject placedObject;

		public BarbedWire(Room room, PlacedObject pObj) : this(room, pObj, true, true) { }

		public BarbedWire(Room room, PlacedObject pObj, bool breakable, bool Harmful) : base()
		{
			this.room = room;
			this.placedObject = pObj;
			this.Breakable = breakable;
			this.Harmful = Harmful;

			health = 1f;
			broken = false;

			startPos = pObj.pos;
			endPos = (pObj.data as BarbedWireData).endPos;

			spikes = new Vector2[(int)(endPos.magnitude / 15f)];
			for (int i = 0; i < spikes.Length; i++)
			{
				spikes[i] = new Vector2(Mathf.Lerp(0f, 1f, (float)i / (float)spikes.Length), room.game.SeededRandom((int)startPos.magnitude + i) >= 0.5f ? 1 : -1);
			}
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			if (BreakCounter < 4 && BreakCounter > 0)
			{
				BreakCounter--;
			}

			if (broken) return;

			for (int i = 0; i < room.physicalObjects.Length; i++)
			{
				foreach (PhysicalObject Obj in room.physicalObjects[i])
				{
					if (Obj is Creature creature)
					{
						if (creature == null || creature.Stunned) continue;

						for (int b = 0; b < creature.bodyChunks.Length; b++)
						{
							if (Custom.DistLess(_Functions.ClosestPointOnLineClamped(startPos, startPos + endPos, creature.bodyChunks[b].pos), creature.bodyChunks[b].pos, creature.bodyChunks[b].rad))
							{
								if (!CreatureIsImmune(creature))
								{
									float t = 2.5f;
									if (creature.bodyChunks[b].vel.x > t || creature.bodyChunks[b].vel.x < -t || creature.bodyChunks[b].vel.y > t || creature.bodyChunks[b].vel.y < -t)
									{
										if (Harmful)
										{
											creature.Stun(40);
											if (!(creature is Player))
											{
												creature.Violence(null, Vector2.zero, creature.bodyChunks[b], null, Creature.DamageType.Stab, 0.05f, 0f);
											}

											room.PlaySound(SoundID.Rock_Hit_Creature, _Functions.ClosestPointOnLineClamped(startPos, startPos + endPos, creature.bodyChunks[b].pos));
										}

										health -= 0.1f * creature.bodyChunks[b].mass * (3f / 1.05f);
										if (Breakable)
										{
											if (health <= 0f)
											{
												Break(creature.bodyChunks[b]);
												return;
											}
										}
									}
								}
								else if (CreatureBreaks(creature) && Breakable)
								{
									Break(creature.bodyChunks[b]);
									return;
								}

								break;
							}
						}
					}
					else
					{
						if (Custom.DistLess(_Functions.ClosestPointOnLineClamped(startPos, startPos + endPos, Obj.firstChunk.pos), Obj.firstChunk.pos, 20f))
						{
							if (Harmful)
							{
								Obj.firstChunk.vel *= 0.88f;
							}

							if (Obj is ExplosiveSpear s && s.mode == Weapon.Mode.Thrown)
							{
								health = 0f;
								if (Harmful)
								{
									s.Explode();
								}
							}
							else if (Obj is ScavengerBomb b && b.mode == Weapon.Mode.Thrown)
							{
								health = 0f;
								if (Harmful)
								{
									b.Explode(null);
								}
							}
							else if (ModManager.Watcher && Obj is Boomerang r && r.mode == Weapon.Mode.Thrown)
							{
								health -= 0.1f;
								if (Harmful)
								{
									room.PlaySound(SoundID.Rock_Hit_Creature, _Functions.ClosestPointOnLineClamped(startPos, startPos + endPos, Obj.firstChunk.pos));
									r.BeginReturnArc(false);
								}
							}
							else if (Obj is Spear sp && sp.mode == Weapon.Mode.Thrown)
							{
								health = 0f;
							}
							else if (Obj is Rock ro && ro.mode == Weapon.Mode.Thrown)
							{
								health -= 0.1f;
								if (Harmful)
								{
									room.PlaySound(SoundID.Rock_Hit_Creature, _Functions.ClosestPointOnLineClamped(startPos, startPos + endPos, Obj.firstChunk.pos));
									Obj.firstChunk.vel.x = -(Obj.firstChunk.vel.x * 0.66f);
								}
							}

							if (Breakable)
							{
								if (health <= 0f)
								{
									Break(Obj.firstChunk);
									return;
								}
							}

							continue;
						}
					}
				}
			}
		}

		public void Break(BodyChunk touchingChunk)
		{
			Break(touchingChunk.pos);
		}

		public virtual void Break(Vector2 touchingPos)
		{
			if (broken) return;

			room.PlaySound(SoundID.Big_Spider_Slash_Creature, _Functions.ClosestPointOnLineClamped(startPos, startPos + endPos, touchingPos), 0.2f, 0.85f);
			room.PlaySound(SoundID.Slugcat_Throw_Puffball, _Functions.ClosestPointOnLineClamped(startPos, startPos + endPos, touchingPos), 1.4f, 1.33f);

			if (room.world.game.session is StoryGameSession)
			{
				BarbedWireData d = placedObject.data as BarbedWireData;
				(room.world.game.session as StoryGameSession).saveState.ReportConsumedItem(room.world, false, room.abstractRoom.index, room.roomSettings.placedObjects.IndexOf(placedObject), (d.minRegen > 0) ? UnityEngine.Random.Range(d.minRegen, d.maxRegen + 1) : (-1));
			}
			broken = true;
			breakPoint = Custom.Dist(startPos, _Functions.ClosestPointOnLineClamped(startPos, startPos + endPos, touchingPos)) / endPos.magnitude;
			BreakCounter = 3;
		}

		public virtual bool CreatureIsImmune(Creature creature)
		{
			if (creature is null || creature.dead || CreatureBreaks(creature) || creature.Template.smallCreature)
			{
				return true;
			}

			if (creature is PoleMimic || creature is TentaclePlant || creature is Leech || creature is Spider || creature is Scavenger || creature is Overseer || creature is Fly || creature is Snail || (creature is Centipede c && c.Red) || creature is MirosBird)
			{
				return true;
			}
			else if (ModManager.MSC && (creature is StowawayBug || (creature is Vulture v && v.IsMiros)))
			{
				return true;
			}
			else if (ModManager.Watcher && (creature is Barnacle || creature is FireSprite || creature is Frog || creature is RippleSpider || creature is Loach))
			{
				return true;
			}

			return false;
		}

		public virtual bool CreatureBreaks(Creature creature)
		{
			if (creature is BigEel)
			{
				return true;
			}
			else if (ModManager.Watcher && creature is SkyWhale)
			{
				return true;
			}

			return false;
		}

		public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[2 + spikes.Length];

			sLeaser.sprites[0] = new FSprite("pixel");
			sLeaser.sprites[0].anchorY = 0;
			sLeaser.sprites[0].scaleX = 4f;

			sLeaser.sprites[1] = new FSprite("pixel");
			sLeaser.sprites[1].anchorY = 0;
			sLeaser.sprites[1].scaleX = 4f;

			for (int i = 2; i < sLeaser.sprites.Length; i++)
			{
				sLeaser.sprites[i] = new FSprite("ShortcutArrow");
				sLeaser.sprites[i].scaleX = (2f / 3f) * 1.5f;
				sLeaser.sprites[i].scaleY = 1.22f * 1.5f;
				sLeaser.sprites[i].anchorY = 0;
			}

			AddToContainer(sLeaser, rCam, null);
		}

		public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			float v = Mathf.Lerp(0f, endPos.magnitude, broken ? breakPoint : 1f);

			sLeaser.sprites[0].SetPosition(startPos - camPos);
			sLeaser.sprites[0].scaleY = Mathf.Lerp(0f, v, (float)BreakCounter / 4f);
			sLeaser.sprites[0].rotation = Custom.AimFromOneVectorToAnother(Vector2.zero, endPos);

			sLeaser.sprites[1].SetPosition(startPos + endPos - camPos);
			sLeaser.sprites[1].scaleY = Mathf.Lerp(1f, v, (float)BreakCounter / 4f);
			sLeaser.sprites[1].rotation = Custom.AimFromOneVectorToAnother(endPos, Vector2.zero);

			for (int i = 2; i < sLeaser.sprites.Length; i++)
			{
				Vector2 perp = Custom.PerpendicularVector(Custom.DegToVec(Custom.AimFromOneVectorToAnother(Vector2.zero, endPos)));
				Vector2 pos = Vector2.Lerp(startPos, startPos + endPos, spikes[i - 2].x);

				pos += spikes[i - 2].y * perp;
				pos = Vector2.Lerp(Custom.DistLess(startPos, pos, endPos.magnitude / 2f) ? startPos : (startPos + endPos), pos, (float)BreakCounter / 4f);
				sLeaser.sprites[i].SetPosition(pos - camPos);

				Vector2 o = Custom.RotateAroundOrigo(new Vector2(0, 4), Mathf.Clamp01(spikes[i - 2].y) * 180f + Custom.VecToDeg(perp));
				sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(Vector2.zero, o);
			}

			if (base.slatedForDeletetion || room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
			}
		}

		public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			for (int i = 0; i < sLeaser.sprites.Length; i++)
			{
				sLeaser.sprites[i].color = rCam.currentPalette.blackColor;
			}
		}

		public virtual void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			if (newContatiner == null)
			{
				newContatiner = rCam.ReturnFContainer("Items");
			}

			FSprite[] sprites = sLeaser.sprites;
			foreach (FSprite fSprite in sprites)
			{
				fSprite.RemoveFromContainer();
				newContatiner.AddChild(fSprite);
			}
		}
	}
}
