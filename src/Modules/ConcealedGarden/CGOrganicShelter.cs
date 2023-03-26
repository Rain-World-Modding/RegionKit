using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RWCustom;
using System.Linq;

namespace RegionKit.Modules.ConcealedGarden;

public static class CGOrganicShelter
{

	public class CGOrganicShelterCoordinator : UpdatableAndDeletable, IDrawable, ShelterBehaviors.IReactToShelterEvents
	{
		private readonly PlacedObject pObj;
		private readonly RainCycle rainCycle;
		private float closedFac;
		private float closeSpeed;
		ManagedData data;

		private Vector2[] lockPos;
		private Vector2[] lockTarget;
		private RootedPaart[] locks;

		//List<PlacedObject> linings;
		//private List<TileInterface> tiles;
		//private List<List<TileInterface>> lines;
		private RootedPaart[] blobs;

		private float stiff;
		//private int framesToClose;
		private float rmin;
		private float rmax;
		private float gmin;
		private float gmax;
		private float bmin;
		private float bmax;

		public CGOrganicShelterCoordinator(Room room, PlacedObject pObj)
		{
			this.room = room;
			this.pObj = pObj;
			this.data = pObj.data as ManagedData;
			//Debug.Log("Coordinator start");
			//Debug.Log("Reading data");

			this.rmin = data.GetValue<float>("rmin");
			this.rmax = data.GetValue<float>("rmax");
			this.gmin = data.GetValue<float>("gmin");
			this.gmax = data.GetValue<float>("gmax");
			this.bmin = data.GetValue<float>("bmin");
			this.bmax = data.GetValue<float>("bmax");
			this.stiff = data.GetValue<float>("stiff");
			//this.framesToClose = data.GetValue<int>("ftc");

			PlacedObject.Type lockType = new PlacedObject.Type("CGOrganicLockPart", false);
			
			PlacedObject.Type liningType = new PlacedObject.Type("CGOrganicLining", false);

			List<PlacedObject> locks = new List<PlacedObject>();
			List<PlacedObject> linings = new List<PlacedObject>();

			//Debug.Log("finding objects");

			foreach (var item in room.roomSettings.placedObjects)
			{
				if (item.active && item.type == lockType) locks.Add(item);
				if (item.active && item.type == liningType) linings.Add(item);
			}


			//Debug.Log("making locks");
			lockPos = new Vector2[locks.Count];
			lockTarget = new Vector2[locks.Count];
			this.locks = new RootedPaart[locks.Count];
			for (int i = 0; i < locks.Count; i++)
			{
				PlacedObject lk = locks[i];
				ManagedData lkdata = (lk.data as ManagedData);
				this.lockPos[i] = lk.pos;
				this.lockTarget[i] = lk.pos + lkdata.GetValue<Vector2>("dest");
				this.locks[i] = new RootedPaart(lk.pos, lkdata.GetValue<Vector2>("size").magnitude, UnityEngine.Random.value * 360f, lkdata.GetValue<float>("stiff"), 0);
			}
			locks.Clear();

			// Corruption my old friend
			// huh except I decided to make it more complicated lol
			// Find affected tiles, store tiles and intensity and properties
			//Debug.Log("parsing linings");
			List<TileInterface> tiles = new List<TileInterface>();
			for (int i = 0; i < linings.Count; i++)
			{
				//Debug.Log("and " + (i+1));
				ManagedData lidata = (linings[i].data as ManagedData);
				float placerad = lidata.GetValue<Vector2>("size").magnitude;
				float smin = lidata.GetValue<float>("sizemin");
				float smax = lidata.GetValue<float>("sizemax");
				float depth = lidata.GetValue<float>("depth");
				float density = lidata.GetValue<float>("density");
				float stiff = lidata.GetValue<float>("stiff");
				float spread = lidata.GetValue<float>("spread");
				int seed = lidata.GetValue<int>("seed");

				IntVector2 bottomleft = this.room.GetTilePosition(linings[i].pos - new Vector2(placerad, placerad));
				IntVector2 topright = this.room.GetTilePosition(linings[i].pos + new Vector2(placerad, placerad));
				for (int j = bottomleft.x; j <= topright.x; j++)
				{
					for (int k = bottomleft.y; k <= topright.y; k++)
					{
						Vector2 tilepos = this.room.MiddleOfTile(j, k);
						Vector2 towardsCenter = linings[i].pos - tilepos;
						float distance = towardsCenter.magnitude;
						float intensity = Mathf.InverseLerp(placerad, 0, distance);

						// Solid looking for air interface
						if (this.room.GetTile(j, k).Solid && distance < placerad)
						{
							bool flag = false;
							int num = 0;
							while (num < 4 && !flag)
							{
								if (!this.room.GetTile(j + Custom.fourDirections[num].x, k + Custom.fourDirections[num].y).Solid)
								{
									// If tile already in, influence properties with own new properties
									TileInterface newTile = new TileInterface(new IntVector2(j, k), Custom.fourDirections[num], intensity, smin, smax, depth, density, stiff, spread, seed, towardsCenter);
									if (tiles.Contains(newTile)) tiles.Find((t) => t == newTile).Assimilate(newTile);
									else tiles.Add(newTile);
									seed++;
								}
								num++;
							}
						}
					}
				}
			}
			//Debug.Log("and done");
			linings.Clear();

			//Debug.Log("sorting tiles");
			// Sort and align into long lines
			tiles.Sort((a, b) => (a.tile.y - b.tile.y) * 1000 + a.tile.x - b.tile.x); // Sorted in Y then X, very important so I can make some assumptions later
																					  //Debug.Log("making lines");
			List<List<TileInterface>> lines = new List<List<TileInterface>>();
			foreach (var tile in tiles)
			{
				bool added = false;
				foreach (var line in lines)
				{
					if (TileInterface.TilesLineUp(line.GetLastObject(), tile)) // this makes assumptions
					{
						line.Add(tile);
						added = true;
					}
				}
				if (!added)
				{
					lines.Add(new List<TileInterface>() { tile });
				}
			}
			tiles.Clear(); // done with you

			int oldseed = UnityEngine.Random.seed;
			//Debug.Log("spawning blobs");
			List<RootedPaart> blobs = new List<RootedPaart>();
			// Fill these lines with blobs
			foreach (var line in lines)
			{
				//Debug.Log("a");
				int len = line.Count;
				float avrDensity = line.Aggregate(0f, (f, t) => f + t.density) / len;
				float avrMinSize = line.Aggregate(0f, (f, t) => f + t.smin) / len;
				float avrMaxSize = line.Aggregate(0f, (f, t) => f + t.smax) / len;
				UnityEngine.Random.seed = line.Aggregate(0, (i, t) => i + t.seed);
				int nodes = Mathf.FloorToInt(UnityEngine.Random.Range(0.5f, 1.5f) * len * avrDensity * 20f / (0.5f * avrMinSize + 0.5f * avrMaxSize));
				//Debug.Log("b");
				for (int i = 0; i < nodes; i++)
				{
					float factorAt = nodes > 1 ? (float)i / (nodes - 1) : 0;
					TileInterface tileAtFactor;
					Vector2 towardsEpicenter = Custom.PerpendicularVector(line[0].dir.ToVector2());
					//Debug.Log("c");
					if (len > 1)
					{
						int tileAt = Mathf.FloorToInt(factorAt * (len - 1));
						//Debug.Log("d");
						if (tileAt == len - 1) tileAt = len - 2;
						float factorBetween = (factorAt * (len - 1)) - tileAt;
						//Debug.Log("line.c is " + line.Count);
						//Debug.Log("len is " + len);
						//Debug.Log("tileat is " + tileAt);
						tileAtFactor = line[tileAt] * (1 - factorBetween) + line[tileAt + 1] * (factorBetween); // SOME PROPER INTERPOLATION the way I like it
																												//Debug.Log("e");
																												// this has flaws though
						towardsEpicenter = (line[tileAt + 1].intensity - line[tileAt].intensity) * (line[tileAt + 1].tile - line[tileAt].tile).ToVector2() * (1 - factorBetween) * factorBetween;
						//Debug.Log("f");
						UnityEngine.Random.seed = tileAtFactor.seed;
						line[tileAt].seed++;
					}
					else
					{
						//Debug.Log("g");
						tileAtFactor = line[0];
						towardsEpicenter *= 0f;
						//Debug.Log("h");
						UnityEngine.Random.seed = tileAtFactor.seed;
						line[0].seed++;
					}
					//Debug.
					//Debug.Log("j");
					Vector2 widenUp = Vector2.zero; //Custom.PerpendicularVector(new IntVector2(-Mathf.Abs(line[0].dir.x), Mathf.Abs(line[0].dir.y)).ToVector2()) / 2f; // Used to widen up
					Vector2 pos = new Vector2(10f, 10f) // center of tile
						+ line[0].dir.ToVector2() * (10f - tileAtFactor.depth) // edge
						+ Vector2.Lerp(line[0].tile.ToVector2() + widenUp, line.GetLastObject().tile.ToVector2() - widenUp, factorAt) * 20f //tile, this make assumptions on tile order
						+ towardsEpicenter * 10f //skew
						+ Custom.RNV() * tileAtFactor.spread; //spread
															  //Debug.Log("k");
					blobs.Add(new RootedPaart(pos,
						Mathf.Lerp(tileAtFactor.smin, tileAtFactor.smax, UnityEngine.Random.value),
						UnityEngine.Random.value * 360f,
						tileAtFactor.stiff,
						UnityEngine.Random.value));
				}
				//Debug.Log("l");
				line.Clear();
			}
			//Debug.Log("m");
			lines.Clear();
			//Debug.Log("blobs done");
			UnityEngine.Random.seed = oldseed;

			this.blobs = blobs.ToArray();

			this.rainCycle = room.world.rainCycle;
			if (this.Broken)
			{

			}

			if (this.rainCycle == null)
			{
				this.closedFac = 1f;
				this.closeSpeed = 1f;
			}
			else
			{
				this.closedFac = 0;// ((!room.game.setupValues.cycleStartUp) ? 1f : Mathf.InverseLerp(this.initialWait + this.openUpTicks, this.initialWait, (float)this.rainCycle.timer));
				this.closeSpeed = -1f;
			}
			//Debug.Log("Coordinator done");
		}


		public override void Update(bool eu)
		{
			base.Update(eu);

			bool exciteAll = false;
			if (room.game.devToolsActive && UnityEngine.Input.GetKey("l")) exciteAll = true; ;

			this.closedFac += closeSpeed;
			if (closedFac <= 0)
			{
				closedFac = 0;
				closeSpeed = 0;
			}
			else if (closedFac >= 1)
			{
				closedFac = 1;
				closeSpeed = 0;
			}



			for (int i = 0; i < locks.Length; i++)
			{
				this.locks[i].Update();
				float stiffness = stiff + locks[i].stiff;
				this.locks[i].ConnectToPoint(
					Vector2.Lerp(this.lockPos[i], this.lockTarget[i], closedFac),
					locks[i].size * (1f - 0.5f * stiffness) * Mathf.Pow(1f - 0.8f * closedFac, 2f),
					false, 0.02f + 0.08f * stiffness,
					Vector2.zero, 0f, 0f);
				if (exciteAll) this.locks[i].excitement = 1f;
				if (UnityEngine.Random.value < locks[i].excitement) locks[i].vel += RWCustom.Custom.RNV() * (0.5f + 0.5f * locks[i].excitement) * (1f - 0.5f * stiffness) * 5f;
			}
			foreach (var blob in blobs)
			{
				blob.Update();
				blob.ConnectToPoint(
					blob.root,
					blob.size * (1f - blob.stiff) * (1f - closedFac) * (0.5f + 0.5f * blob.excitement),
					false, 0.02f,
					Vector2.zero, 0f, 0f);
				if (exciteAll) blob.excitement = 1f;
				if (UnityEngine.Random.value < blob.excitement) blob.vel += RWCustom.Custom.RNV() * (0.5f + 0.5f * blob.excitement) * (1f - 0.5f * blob.stiff) * 5f;
			}


			foreach (var upd in room.updateList)
			{
				if (upd is PhysicalObject phys)
				{
					foreach (var blob in blobs)
					{
						//phys.PushOutOf(blob.pos, blob.size * (0.77f + blob.stiff * 0.16f), -1);
						foreach (var chunk in phys.bodyChunks)
						{
							if (blob.PushFromPoint(chunk.pos, chunk.rad + blob.size * (0.9f + blob.stiff * 0.12f), 0.3f - 0.1f * blob.stiff))
								blob.excitement = Mathf.Max(0.2f, blob.excitement + 0.01f); ;
						}
					}
					for (int i = 0; i < locks.Length; i++)
					{
						phys.PushOutOf(this.locks[i].pos, this.locks[i].size * (0.77f + this.locks[i].stiff * 0.16f), -1);
						foreach (var chunk in phys.bodyChunks)
						{
							if (this.locks[i].PushFromPoint(chunk.pos, chunk.rad + this.locks[i].size * (0.9f + this.locks[i].stiff * 0.12f), 0.3f - 0.1f * this.locks[i].stiff))
								this.locks[i].excitement = Mathf.Max(0.2f, this.locks[i].excitement + 0.01f); ;
						}
					}

				}
			}

		}

		public bool Broken
		{
			get
			{
				//return false;
				return this.room.abstractRoom.shelterIndex > -1 && this.room.world.brokenShelters != null & this.room.world.brokenShelters[this.room.abstractRoom.shelterIndex];
			}
		}

		//public void Close()
		//{
		//    this.closeSpeed = 1f/(1 + Mathf.Max(0, data.GetValue<int>("ftc")));// 0.003125f;
		//}


		public void ShelterEvent(float newFactor, float closeSpeed)
		{
			this.closedFac = newFactor;
			this.closeSpeed = closeSpeed;
			if (closeSpeed < 0f) closedFac = 0f; // open instantly because the locks can push the player out of terrain
		}

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[locks.Length + blobs.Length];
			for (int i = 0; i < locks.Length; i++)
			{
				sLeaser.sprites[i] = new FSprite("Futile_White", true);
				sLeaser.sprites[i].scale = locks[i].size / 8f;
				sLeaser.sprites[i].rotation = locks[i].rot;
				sLeaser.sprites[i].alpha = 0.5f + 0.2f * locks[i].stiff;
				sLeaser.sprites[i].shader = rCam.room.game.rainWorld.Shaders["JaggedCircle"];
			}
			for (int i = 0; i < blobs.Length; i++)
			{
				sLeaser.sprites[locks.Length + i] = new FSprite("Futile_White", true);
				sLeaser.sprites[locks.Length + i].scale = blobs[i].size / 8f;
				sLeaser.sprites[locks.Length + i].rotation = blobs[i].rot;
				sLeaser.sprites[locks.Length + i].alpha = 0.5f + 0.2f * blobs[i].stiff;
				sLeaser.sprites[locks.Length + i].shader = rCam.room.game.rainWorld.Shaders["JaggedCircle"];
			}

			this.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
			this.AddToContainer(sLeaser, rCam, null);
		}
		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			foreach (var sprt in sLeaser.sprites)
			{
				sprt.color = new Color(UnityEngine.Random.Range(rmin, rmax),
										UnityEngine.Random.Range(gmin, gmax),
										UnityEngine.Random.Range(bmin, bmax));
			}
		}

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			//if (newContatiner == null) newContatiner = rCam.ReturnFContainer("Midground");
			FContainer mg = rCam.ReturnFContainer("Midground");
			FContainer fg = rCam.ReturnFContainer("Items");
			for (int i = 0; i < locks.Length; i++)
			{
				mg.AddChild(sLeaser.sprites[i]);
			}
			for (int i = 0; i < blobs.Length; i++)
			{
				if (blobs[i].z < 0.6)
					mg.AddChild(sLeaser.sprites[locks.Length + i]);
				else
					fg.AddChild(sLeaser.sprites[locks.Length + i]);
			}
			//foreach (var sprt in sLeaser.sprites)
			//{
			//    newContatiner.AddChild(sprt);
			//}
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			for (int i = 0; i < locks.Length; i++)
			{
				sLeaser.sprites[i].x = Mathf.Lerp(this.locks[i].lastPos.x, this.locks[i].pos.x, timeStacker) - camPos.x;
				sLeaser.sprites[i].y = Mathf.Lerp(this.locks[i].lastPos.y, this.locks[i].pos.y, timeStacker) - camPos.y;
			}
			for (int i = 0; i < blobs.Length; i++)
			{
				sLeaser.sprites[locks.Length + i].x = Mathf.Lerp(this.blobs[i].lastPos.x, this.blobs[i].pos.x, timeStacker) - camPos.x;
				sLeaser.sprites[locks.Length + i].y = Mathf.Lerp(this.blobs[i].lastPos.y, this.blobs[i].pos.y, timeStacker) - camPos.y;
			}
		}
	}

	class TileInterface
	{
		public IntVector2 tile;
		public IntVector2 dir;

		public float intensity;
		public float smin;
		public float smax;
		public float depth;
		public float density;
		public float stiff;
		public float spread;
		public int seed;
		public Vector2 towardsCenter;

		public TileInterface(IntVector2 tile, IntVector2 dir, float intensity, float smin, float smax, float depth, float density, float stiff, float spread, int seed, Vector2 towardsCenter)
		{
			this.tile = tile;
			this.dir = dir;
			this.intensity = intensity;
			this.smin = smin;
			this.smax = smax;
			this.depth = depth;
			this.density = density;
			this.stiff = stiff;
			this.spread = spread;
			this.seed = seed;
			this.towardsCenter = towardsCenter;
		}

		internal void Assimilate(TileInterface n)
		{
			this.smin = (smin * intensity + n.smin * n.intensity) / (intensity + n.intensity);
			this.smax = (smax * intensity + n.smax * n.intensity) / (intensity + n.intensity);
			this.depth = (depth * intensity + n.depth * n.intensity) / (intensity + n.intensity);
			this.density = (density * intensity + n.density * n.intensity) / (intensity + n.intensity);
			this.stiff = (stiff * intensity + n.stiff * n.intensity) / (intensity + n.intensity);
			this.spread = (spread * intensity + n.spread * n.intensity) / (intensity + n.intensity);
			this.seed = seed + n.seed;
			this.towardsCenter = (towardsCenter * intensity + n.towardsCenter * n.intensity) / (intensity + n.intensity);
			this.intensity = (intensity * intensity + n.intensity * n.intensity) / (intensity + n.intensity);
		}

		public override bool Equals(object obj) => obj is TileInterface other && this.tile == other.tile && this.dir == other.dir;
		public override int GetHashCode() => tile.x + 1000 * tile.y + 1000000 * dir.x + 10000000 * dir.y;
		public override string ToString() => "[tile:" + tile.ToString() + ";dir:" + dir.ToString() + ";]";
		public static TileInterface operator *(TileInterface a, float f) => new TileInterface(a.tile, a.dir, a.intensity * f, a.smin * f, a.smax * f, a.depth * f, a.density * f, a.stiff * f, a.spread * f, Mathf.FloorToInt(a.seed * f), a.towardsCenter * f);
		public static TileInterface operator /(TileInterface a, float f) => new TileInterface(a.tile, a.dir, a.intensity / f, a.smin / f, a.smax / f, a.depth / f, a.density / f, a.stiff / f, a.spread / f, Mathf.FloorToInt(a.seed / f), a.towardsCenter / f);
		public static TileInterface operator +(TileInterface a, TileInterface b) => new TileInterface(a.tile, a.dir, a.intensity + b.intensity, a.smin + b.smin, a.smax + b.smax, a.depth + b.depth, a.density + b.density, a.stiff + b.stiff, a.spread + b.spread, a.seed + b.seed, a.towardsCenter + b.towardsCenter);
		public static bool operator ==(TileInterface a, TileInterface b) => a.Equals(b);
		public static bool operator !=(TileInterface a, TileInterface b) => !a.Equals(b);

		public static bool TilesLineUp(TileInterface older, TileInterface newer) // Very specific to how I'm going through these
		{
			return older.dir == newer.dir && ((older.dir.x != 0 && newer.tile == older.tile + new IntVector2(0, 1)) || (older.dir.y != 0 && newer.tile == older.tile + new IntVector2(1, 0)));
		}
	}


	public class RootedPaart : DisembodyedPart
	{
		public Vector2 root;
		public float size;
		public float rot;
		public float stiff;
		public float z;
		public float excitement;

		public RootedPaart(Vector2 root, float size, float rot, float stiff, float z) : base(0.9f + 0.08f * stiff, root)
		{
			this.root = root;
			this.size = size;
			this.rot = rot;
			this.stiff = stiff;
			this.z = z;
			excitement = 0f;
		}

		public override void Update()
		{
			base.Update();
			excitement = Mathf.Max(0f, excitement * 0.98f - 0.003f);
		}
	}



	public class DisembodyedPart
	{
		public DisembodyedPart(float aFric, Vector2 startpos)
		{
			this.airFriction = aFric;
			this.Reset(startpos);
		}

		public virtual void Update()
		{
			this.lastPos = this.pos;
			this.pos += this.vel;
			this.vel *= this.airFriction;
		}

		public virtual void Reset(Vector2 resetPoint)
		{
			this.pos = resetPoint + RWCustom.Custom.DegToVec(UnityEngine.Random.value * 360f);
			this.lastPos = this.pos;
			this.vel = new Vector2(0f, 0f);
		}

		public void ConnectToPoint(Vector2 pnt, float connectionRad, bool push, float elasticMovement, Vector2 hostVel, float adaptVel, float exaggerateVel)
		{
			if (elasticMovement > 0f)
			{
				this.vel += RWCustom.Custom.DirVec(this.pos, pnt) * Vector2.Distance(this.pos, pnt) * elasticMovement;
			}
			this.vel += hostVel * exaggerateVel;
			if (push || !RWCustom.Custom.DistLess(this.pos, pnt, connectionRad))
			{
				float num = Vector2.Distance(this.pos, pnt);
				Vector2 a = RWCustom.Custom.DirVec(this.pos, pnt);
				this.pos -= (connectionRad - num) * a * 1f;
				this.vel -= (connectionRad - num) * a * 1f;
			}
			this.vel -= hostVel;
			this.vel *= 1f - adaptVel;
			this.vel += hostVel;
		}

		public bool PushFromPoint(Vector2 pnt, float pushRad, float elasticity)
		{
			if (RWCustom.Custom.DistLess(this.pos, pnt, pushRad))
			{
				float num = Vector2.Distance(this.pos, pnt);
				Vector2 a = RWCustom.Custom.DirVec(this.pos, pnt);
				this.pos -= (pushRad - num) * a * elasticity;
				this.vel -= (pushRad - num) * a * elasticity;
				return true;
			}
			return false;
		}
		public Vector2 lastPos;
		public Vector2 pos;
		public Vector2 vel;
		protected float airFriction;
	}
}
