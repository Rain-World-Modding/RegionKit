using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RWCustom;

namespace RegionKit.Modules.ConcealedGarden;

internal static class CGElectricArcs
{

	public abstract class CGElectricSparkData : ManagedData
	{
		private static ManagedField[] customFields = new ManagedField[]{
					new ColorField("01", new Color(0.56f, 0.66f, 0.98f), displayName: "Inner Color"),
					new ColorField("02", new Color(0.01f, 0.04f, 1f), displayName: "Outer Color"),
					};
		public Vector2 pos => owner.pos;
#pragma warning disable 0649 // We're reflecting over these fields, stop worrying about it stupid compiler
		[BackedByField("01")]
		public Color innercolor;
		[BackedByField("02")]
		public Color outtercolor;
		[BooleanField("03", false, displayName: "Cosmetic")]
		public bool cosmetic;
		[IntegerField("04", 1, 100, 8, displayName: "Nodes", control: ManagedFieldWithPanel.ControlType.slider)]
		public int numberOfSparks;
		[FloatField("05", 0f, 10f, 0.5f, 0.01f, displayName: "Minspace")]
		public float minspace;
		[FloatField("06", 0f, 10f, 2f, 0.01f, displayName: "Maxspace")]
		public float maxspace;
		[FloatField("07", 0f, 20f, 4f, 0.1f, displayName: "Jumpyness")]
		public float jumpyness;
		[FloatField("08", 0f, 1f, 0.05f, 0.001f, displayName: "Tightness")]
		public float tightness;
		[FloatField("09", -0.5f, 0.5f, 0.005f, 0.001f, displayName: "Centerness")]
		public float centerness;
		[FloatField("10", 0f, 1f, 0.05f, 0.001f, displayName: "Ellasticity")]
		public float ellasticity;
		[FloatField("11", 0f, 1f, 0.05f, 0.001f, displayName: "Spread")]
		public float spread;

		[FloatField("12", -5f, 5f, 0.5f, 0.01f, displayName: "Grav Pull")]
		public float gravitypull;
		[FloatField("13", -5f, 5f, 0f, 0.01f, displayName: "X Pull")]
		public float xpull;
		[FloatField("14", -5f, 5f, 0f, 0.01f, displayName: "Y Pull")]
		public float ypull;

		[FloatField("15", 0f, 2000f, 400f, 1f, displayName: "Lightrad")]
		public float lightrad;

#pragma warning restore 0649
		public CGElectricSparkData(PlacedObject owner) : base(owner, customFields) { }
		public CGElectricSparkData(PlacedObject owner, ManagedField[]? fields = null) : base(owner, fields == null ? customFields : customFields.ToList().Concat(fields.ToList()).ToArray()) { }
	}

	public class CGElectricArcData : CGElectricSparkData
	{
		private static ManagedField[] customFields = new ManagedField[]{
				new Vector2Field("20", new Vector2(-100, 30)),
			};
#pragma warning disable 0649
		[BackedByField("20")]
		public Vector2 end;
		[IntegerField("21", 0, 400, 15, displayName: "Natcooldown")]
		public int natcooldown;
		[IntegerField("22", 0, 400, 40, displayName: "Shockcooldown")]
		public int shockcooldown;
#pragma warning restore 0649
		public CGElectricArcData(PlacedObject owner) : base(owner, customFields) { }
		public CGElectricArcData(PlacedObject owner, ManagedField[]? fields = null) : base(owner, fields == null ? customFields : customFields.ToList().Concat(fields.ToList()).ToArray()) { }
	}

	public class CGElectricArcGeneratorData : CGElectricSparkData
	{
		private static ManagedField[] customFields = new ManagedField[]{
					new Vector2Field("20", new Vector2(-100, 30)),
					new DrivenVector2Field("21", "20", new Vector2(-10, 100), DrivenVector2Field.DrivenControlType.relativeLine, "end-to"),
					new Vector2Field("22", new Vector2(10, 100), label:"start-to"),
			};
#pragma warning disable 0649 // We're reflecting over these fields, stop worrying about it stupid compiler
		[BackedByField("20")]
		public Vector2 end;
		[BackedByField("21")]
		public Vector2 endto;
		[BackedByField("22")]
		public Vector2 startto;
		[FloatField("23", 0.01f, 1f, 0.1f, 0.01f, displayName: "Speed")]
		public float speed;
		[IntegerField("24", 0, 400, 40, displayName: "Interval", control: ManagedFieldWithPanel.ControlType.slider)]
		public int interval;
		[FloatField("25", -2f, 2f, 0.5f, 0.01f, displayName: "Forwardness")]
		public float forwardness;
#pragma warning restore 0649
		public CGElectricArcGeneratorData(PlacedObject owner) : base(owner, customFields) { }
		public CGElectricArcGeneratorData(PlacedObject owner, ManagedField[]? fields = null) : base(owner, fields == null ? customFields : customFields.ToList().Concat(fields.ToList()).ToArray()) { }

	}

	public class CGElectricArcGenerator : UpdatableAndDeletable
	{
		private readonly PlacedObject pObj;

		List<CGElectricArc.Spark> sparks;
		int cooldown;
		private bool powered = true;

		private CGElectricArcGeneratorData data => (CGElectricArcGeneratorData)pObj.data;
		public CGElectricArcGenerator(PlacedObject pObj, Room room)
		{
			this.pObj = pObj;
			this.room = room;
			PowerCycle(true);
			sparks = new List<CGElectricArc.Spark>();
			cooldown = 0;
			if (powered)
			{
				float framesToTravel = 10f / data.speed;
				for (int i = 0; i < framesToTravel / data.interval; i++)
				{
					sparks.Add(new CGElectricArc.Spark(room, pObj.pos + i * data.interval * data.startto * data.speed / 10f, pObj.pos + data.end + i * data.interval * data.endto * data.speed / 10f, this, data.numberOfSparks, data));
					room.AddObject(sparks.Last());
				}
			}
		}

		void PowerCycle(bool force)
		{
			if (this.room.world.rainCycle != null && this.room.world.rainCycle.brokenAntiGrav != null)
			{
				bool flag = this.room.world.rainCycle.brokenAntiGrav.to == 1f && this.room.world.rainCycle.brokenAntiGrav.progress == 1f;
				if (!flag)
				{
					if (this.powered && UnityEngine.Random.value < 0.15f || force)
					{
						this.powered = false;
					}
				}
				if (flag && !this.powered && UnityEngine.Random.value < 0.025f || force)
				{
					this.powered = true;
				}
			}
			else powered = true;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			cooldown--;
			PowerCycle(false);
			if (cooldown < 0 && powered)
			{
				sparks.Add(new CGElectricArc.Spark(room, pObj.pos, pObj.pos + data.end, this, data.numberOfSparks, data));
				room.AddObject(sparks.Last());
				cooldown = data.interval;
			}
			foreach (var sparkie in sparks)
			{
				sparkie.start += data.startto * data.speed / 10f;
				sparkie.stop += data.endto * data.speed / 10f;
				if ((sparkie.start - pObj.pos).sqrMagnitude > data.startto.sqrMagnitude) sparkie.Break();
				for (int i = 0; i < sparkie.nodes.Length; i++)
				{
					CGElectricArc.Spark.SparkNode node = sparkie.nodes[i];
					node.pos += (sparkie.nodes.Length > 1 ? Vector2.Lerp(data.startto, data.endto, (float)i / (float)(sparkie.nodes.Length - 1)) : data.startto) * data.forwardness * data.speed / 10f;
					//node.pos += (Vector2.Lerp(data.startto, data.endto, (float)i / (float)(sparkie.nodes.Length - 1))) * data.forwardness * data.speed / 10f;
				}
			}
			for (int i = sparks.Count - 1; i >= 0; i--)
			{
				if (sparks[i].slatedForDeletetion) sparks.RemoveAt(i);
			}
		}

		// https://answers.unity.com/questions/1271974/inverselerp-for-vector3.html
		public static float InverseLerp(Vector2 a, Vector2 b, Vector2 value)
		{
			Vector2 AB = b - a;
			Vector2 AV = value - a;
			return Mathf.Clamp01(Vector2.Dot(AV, AB) / AB.sqrMagnitude);
		}
	}

	public class CGElectricArc : UpdatableAndDeletable
	{
		private readonly PlacedObject pObj;

		private CGElectricArcData data => (CGElectricArcData)pObj.data;
		public CGElectricArc(PlacedObject pObj)
		{
			this.pObj = pObj;
		}

		Spark? spark;
		private int cooldown;

		public override void Update(bool eu)
		{
			base.Update(eu);
			if (spark == null || spark.slatedForDeletetion || spark.broken)
			{
				spark = null;
				cooldown--;
				if (cooldown <= 0)
				{
					room.AddObject(spark = new Spark(room, pObj.pos, pObj.pos + data.end, this, data.numberOfSparks, data));
					cooldown = -1;
				}
			}
		}

		public class Spark : CosmeticSprite
		{
			public Vector2 start;
			public Vector2 stop;
			private readonly UpdatableAndDeletable owner;
			private readonly int nNodes;
			private readonly CGElectricSparkData data;
			private readonly float spacing;
			internal SparkNode[] nodes;
			public bool broken = false;
			private float intensity;
			private StaticSoundLoop soundLoop;
			private StaticSoundLoop disruptedLoop;
			private float weightedDisruption;
			private LightSource? light;

			public Spark(Room room, Vector2 start, Vector2 stop, UpdatableAndDeletable owner, int nNodes, CGElectricSparkData data)
			{

				this.start = start;
				this.stop = stop;
				this.owner = owner;
				this.nNodes = nNodes;
				this.data = data;
				this.spacing = (start - stop).magnitude / (float)nNodes;
				this.nodes = new SparkNode[nNodes];

				if (data.lightrad > 1f)
				{
					this.light = new LightSource(data.pos, false, data.outtercolor, this);
					room.AddObject(light);
				}

				intensity = 1f;

				for (int i = 0; i < nNodes; i++)
				{
					nodes[i] = new SparkNode(Vector2.Lerp(start, stop, Mathf.InverseLerp(-1, nNodes, i)));
				}

				this.soundLoop = new StaticSoundLoop(SoundID.Zapper_LOOP, Vector2.Lerp(start, stop, 0.5f), room, 0f, 1f);
				this.disruptedLoop = new StaticSoundLoop(SoundID.Zapper_Disrupted_LOOP, Vector2.Lerp(start, stop, 0.5f), room, 0f, 1f);

				this.weightedDisruption = 0f;
			}

			public override void Update(bool eu)
			{
				base.Update(eu);

				this.soundLoop.Update();
				this.disruptedLoop.Update();
				soundLoop.volume = Mathf.Clamp01(intensity * (broken ? 0.5f : 1f));
				disruptedLoop.volume = Mathf.Clamp01(intensity * (broken ? 0.8f : 0f));

				Vector2 weightedCenter = start + stop;
				Vector2 frameDisruption = Vector2.zero;
				Vector2 direction = (stop - start).normalized;
				for (int i = 0; i < nodes.Length; i++)
				{
					weightedCenter += nodes[i].pos;
					Vector2 jump = UnityEngine.Random.insideUnitCircle * data.jumpyness;
					nodes[i].vel += jump * (broken ? 2f : 1f) + new Vector2(0f, room.gravity) * data.gravitypull + new Vector2(data.xpull, data.ypull);
					Vector2 correctPosition = Vector2.Lerp(start, stop, Mathf.InverseLerp(-1, nNodes, i));
					Vector2 pull = correctPosition - nodes[i].pos;
					nodes[i].vel += pull * data.tightness;
					pull -= Vector2.Dot(pull, direction) * direction;
					nodes[i].vel += pull * data.centerness;

					for (int j = -1; j < 2; j += 2)
					{
						if (i == 0 && j == -1) pull = start - nodes[i].pos;
						else if (i == nodes.Length - 1 && j == 1) pull = stop - nodes[i].pos;
						else pull = nodes[i + j].pos - nodes[i].pos;
						nodes[i].vel += pull * data.ellasticity / 2f;
						float mag = pull.magnitude;
						nodes[i].vel += data.spread / 2f * pull.normalized * (mag - spacing * data.minspace);
						if (mag > data.maxspace * spacing) this.Break();
					}
					frameDisruption += nodes[i].vel;
				}
				weightedCenter /= (nNodes + 2);
				frameDisruption /= nNodes;
				Vector2 previous = start;
				for (int i = 0; i < nodes.Length; i++)
				{
					nodes[i].Update();

					if (data.cosmetic) continue;
					foreach (var physgroup in room.physicalObjects)
					{
						foreach (var phys in physgroup)
						{
							if ((phys.firstChunk.pos - nodes[i].pos).magnitude < data.maxspace + phys.collisionRange) // in range for testing
							{
								for (int k = 0; k < phys.bodyChunks.Length; k++)
								{
									BodyChunk chunk = phys.bodyChunks[k];
									Vector2 closest = Custom.ClosestPointOnLineSegment(previous, nodes[i].pos, chunk.pos);
									if ((closest - chunk.pos).magnitude < chunk.rad + 2 || Custom.IsPointBetweenPoints(chunk.pos, chunk.lastPos, closest)) // NOT PERFECT would need some more serious checks considering lastpos but its goodenuff
									{
										this.Shock(phys, k, closest);
									}
								}
							}
						}
					}
				}
				if (broken)
				{
					this.intensity *= 0.85f;
					if (intensity < 0.05f) this.Destroy();
				}

				this.weightedDisruption = Mathf.Lerp(weightedDisruption, Mathf.InverseLerp(0f, 4f, frameDisruption.magnitude), 0.6f);

				if (this.light != null)
				{
					float visualIntensity = Mathf.Clamp01((intensity - 0.4f) + 0.4f * Mathf.Pow(weightedDisruption, 0.4f));
					light.setAlpha = new float?(visualIntensity);
					light.setRad = new float?(data.lightrad * visualIntensity);
					light.setPos = new Vector2?(weightedCenter);
				}

				if (this.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.BrokenZeroG) > 0f && this.room.world.rainCycle != null && this.room.world.rainCycle.brokenAntiGrav != null)
				{
					bool flag = this.room.world.rainCycle.brokenAntiGrav.to == 1f && this.room.world.rainCycle.brokenAntiGrav.progress == 1f;
					if (!flag)
					{
						if (!this.broken && UnityEngine.Random.value < 0.015f)
						{
							Break();
						}
					}
				}
			}

			public void Shock(PhysicalObject phys, int chunkindex, Vector2 contact)
			{
				Creature? crit = phys as Creature;
				if (broken && crit != null)  // shocked during decay
				{
					crit.room.AddObject(new CreatureSpasmer(crit, true, Mathf.FloorToInt(20 * intensity)));
					crit.Stun(Mathf.FloorToInt(20 * intensity));
					phys.bodyChunks[chunkindex].vel += (phys.bodyChunks[chunkindex].pos - contact).normalized * 3f * intensity;
				}
				else if (!broken)
				{
					this.intensity = Mathf.Lerp(2.0f, phys.TotalMass, 0.5f);
					this.broken = true;
					if (owner is CGElectricArc arc)
						arc.Break(true);
					if (phys.grabbedBy != null && phys.grabbedBy.Count != 0)
					{
						for (int i = phys.grabbedBy.Count - 1; i >= 0; i--)
						{
							Creature.Grasp grasp = phys.grabbedBy[i];
							if (grasp.grabber != null) this.Shock(grasp.grabber, grasp.grabber.mainBodyChunkIndex, phys.bodyChunks[chunkindex].pos);
						}
					}
					if (crit != null)
					{
						crit.Die();
						crit.room.AddObject(new CreatureSpasmer(crit, true, Mathf.FloorToInt(40 * intensity)));
					}
					this.room.AddObject(new ZapCoil.ZapFlash(contact, Mathf.InverseLerp(-0.05f, 15f, phys.bodyChunks[chunkindex].rad)));
					phys.bodyChunks[chunkindex].vel += ((phys.bodyChunks[chunkindex].pos - contact).normalized * 6f + Custom.RNV() * UnityEngine.Random.value) / phys.bodyChunks[chunkindex].mass;
					this.room.PlaySound(SoundID.Zapper_Zap, phys.bodyChunks[chunkindex].pos, 1f, 1f);
				}
			}

			public void Break()
			{
				if (broken) return;
				this.broken = true;
				this.intensity = 1.5f;
				if (owner is CGElectricArc arc)
					arc.Break(false);
			}

			public class SparkNode
			{
				public Vector2 lastPos;
				public Vector2 vel;
				public Vector2 pos;
				public float fric = 0.5f;

				public SparkNode(Vector2 pos)
				{
					this.lastPos = pos;
					this.vel = Vector2.zero;
					this.pos = pos;
				}

				public void Update()
				{
					this.lastPos = pos;
					this.pos += vel;
					this.vel *= fric;
				}
			}

			public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				TriangleMesh outermesh = TriangleMesh.MakeLongMesh(nNodes + 1, false, false, "Futile_White");
				TriangleMesh innermesh = TriangleMesh.MakeLongMesh(nNodes + 1, false, false, "Futile_White");
				sLeaser.sprites = new FSprite[2] { outermesh, innermesh };

				float edge = 0.4f;
				outermesh.UVvertices[0] = new Vector2(0f, 0f);
				outermesh.UVvertices[1] = new Vector2(1f, 0f);
				for (int i = 0; i < nodes.Length; i++)
				{
					float factor = Mathf.Lerp(edge, 1 - edge, Mathf.InverseLerp(0, nodes.Length, i - 0.3f));
					float factor2 = Mathf.Lerp(edge, 1 - edge, Mathf.InverseLerp(0, nodes.Length, i + 0.3f));
					outermesh.UVvertices[i * 4 + 2] = new Vector2(0f, factor);
					outermesh.UVvertices[i * 4 + 3] = new Vector2(1f, factor);
					outermesh.UVvertices[i * 4 + 4] = new Vector2(0f, factor2);
					outermesh.UVvertices[i * 4 + 5] = new Vector2(1f, factor2);
				}
				outermesh.UVvertices[nodes.Length * 4 + 2] = new Vector2(0f, 1f);
				outermesh.UVvertices[nodes.Length * 4 + 3] = new Vector2(1f, 1f);

				innermesh.shader = rCam.room.game.rainWorld.Shaders["OverseerZip"];
				innermesh.color = data.innercolor;// new Color(0.56f, 0.66f, 0.98f);
				innermesh.alpha = intensity;


				outermesh.shader = rCam.room.game.rainWorld.Shaders["FlareBomb"];
				outermesh.color = data.outtercolor;// new Color(0.01f, 0.04f, 1f);
				outermesh.alpha = 0.8f;

				ApplyPalette(sLeaser, rCam, rCam.currentPalette);
				AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
			}

			public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
			{
				//TriangleMesh trimesh = sLeaser.sprites[0] as TriangleMesh;

				TriangleMesh outermesh = (TriangleMesh)sLeaser.sprites[0];
				TriangleMesh innermesh = (TriangleMesh)sLeaser.sprites[1];
				Vector2 prev = start;
				Vector2 lastPerp = nNodes > 0 ? RWCustom.Custom.PerpendicularVector(Vector2.Lerp(nodes[0].pos, nodes[0].lastPos, timeStacker) - prev).normalized : RWCustom.Custom.PerpendicularVector(stop - prev).normalized;
				lastPerp = Vector2.Lerp(lastPerp, RWCustom.Custom.PerpendicularVector(stop - start).normalized, 0.6f);
				float width = 0.5f;

				for (int i = 0; i <= nodes.Length; i++)
				{
					Vector2 next = i == nodes.Length ? stop : Vector2.Lerp(nodes[i].pos, nodes[i].lastPos, timeStacker);
					Vector2 perp = RWCustom.Custom.PerpendicularVector(next - prev).normalized;
					Vector2 nextPerp = i < nodes.Length - 1 ? RWCustom.Custom.PerpendicularVector(Vector2.Lerp(nodes[i + 1].pos, nodes[i + 1].lastPos, timeStacker) - next).normalized
						: i == nodes.Length - 1 ? RWCustom.Custom.PerpendicularVector(stop - next).normalized : perp;
					perp = Vector2.Lerp(lastPerp, perp, 0.3f).normalized;
					nextPerp = Vector2.Lerp(perp, nextPerp, 0.5f).normalized;
					float nextWidth;
					if (i != nodes.Length) nextWidth = Mathf.Lerp(1f + Mathf.Abs(Vector2.Dot(perp, nodes[i].vel)), width, 0.5f);
					else nextWidth = 0.5f;

					Vector2 avr1 = i == 0 ? prev : Vector2.Lerp(prev, next, 0.2f);
					Vector2 avr2 = i == nodes.Length ? next : Vector2.Lerp(prev, next, 0.8f);

					innermesh.MoveVertice(4 * i + 0, avr1 + perp * width - camPos);
					innermesh.MoveVertice(4 * i + 1, avr1 - perp * width - camPos);
					innermesh.MoveVertice(4 * i + 2, avr2 + nextPerp * nextWidth - camPos);
					innermesh.MoveVertice(4 * i + 3, avr2 - nextPerp * nextWidth - camPos);

					avr1 = i == 0 ? prev : Vector2.Lerp(prev, next, 0.3f);
					avr2 = i == nodes.Length ? next : Vector2.Lerp(prev, next, 0.7f);

					if (i == 0) avr1 = prev + (prev - next).normalized * 40f * intensity;
					if (i == nodes.Length) avr2 = next + (next - prev).normalized * 40f * intensity;
					outermesh.MoveVertice(4 * i + 0, avr1 + perp * 20 * Mathf.Lerp(width * intensity, 1f, 0.7f) - camPos);
					outermesh.MoveVertice(4 * i + 1, avr1 - perp * 20 * Mathf.Lerp(width * intensity, 1f, 0.7f) - camPos);
					outermesh.MoveVertice(4 * i + 2, avr2 + nextPerp * 20 * Mathf.Lerp(nextWidth * intensity, 1f, 0.7f) - camPos);
					outermesh.MoveVertice(4 * i + 3, avr2 - nextPerp * 20 * Mathf.Lerp(nextWidth * intensity, 1f, 0.7f) - camPos);

					prev = next;
					lastPerp = nextPerp;
					width = nextWidth;
				}

				innermesh.alpha = intensity * 0.7f;
				outermesh.alpha = intensity * 0.5f;

				base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			}

			public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
			{

			}
		}

		private void Break(bool shock)
		{
			this.cooldown = shock ? data.shockcooldown : data.natcooldown;
		}
	}
}
