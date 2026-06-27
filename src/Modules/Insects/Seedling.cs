using System.Runtime.CompilerServices;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.Insects
{
	/// <summary>
	/// Little swimmy creatures in the water.
	/// Made by Alduris
	/// </summary>
	public class Seedling : CosmeticInsect
	{
		private static readonly ConditionalWeakTable<Room, RoomTracker> trackerCWT = new();
		private RoomTracker AddToRoomSchool()
		{
			if (!trackerCWT.TryGetValue(room, out RoomTracker swarm))
			{
				swarm = new RoomTracker(room);
				trackerCWT.Add(room, swarm);
			}
			swarm.AddSeedling(this);
			return swarm;
		}

		private void RemoveFromRoomSchool()
		{
			roomSchool?.RemoveSeedling(this);
		}

		private static bool showDebug = false;

		private Vector2 lastHeadDir;
		private Vector2 headDir;
		private SimpleSegment[] actualSegments;
		private SimpleSegment[] dragSegments;
		private float maxSegLength;
		private RoomTracker? roomSchool;
		private int tailSin;
		private float tailWiggleSpeed;
		private float tailWiggleOffset;

		private Vector2 startPos;
		private Vector2 orbitPoint;
		private float orbitPointScore;
		private float orbitIdealDistance;
		private float orbitIdealDistanceScore;

		private float minOrbitIdealDist = 5f;
		private float maxOrbitIdealDist = 120f;

		private float CornerTailSlowdown => Custom.LerpMap(Vector2.Dot(lastHeadDir, headDir), 1f, 0.8f, 1f, 0f);

		private bool OutOfRoomBounds
		{
			get
			{
				return pos.x < 0 || pos.y < 0 || pos.x > room.PixelWidth || pos.y > room.PixelHeight;
			}
		}

		public Seedling(Room room, Vector2 pos) : base(room, pos, _Enums.Seedling)
		{
			this.room = room;
			creatureAvoider = new(this, 10, 200f, .3f);
			actualSegments = new SimpleSegment[Random.Range(5, 8)];
			dragSegments = new SimpleSegment[actualSegments.Length];
			maxSegLength = 2f;
			roomSchool = AddToRoomSchool();
			orbitPoint = pos;
			orbitIdealDistance = Random.Range(minOrbitIdealDist, maxOrbitIdealDist); // random
			tailSin = Random.Range(0, 32767);
			tailWiggleSpeed = Random.Range(14f, 22f);
			tailWiggleOffset = Random.Range(6f, 8f);
			Reset(pos);
		}

		public override void Reset(Vector2 resetPos)
		{
			base.Reset(resetPos);
			startPos = resetPos;
			foreach (SimpleSegment segment in actualSegments)
			{
				segment.Reset(pos + Custom.RNV());
			}
		}
		public override void Update(bool eu)
		{
			base.Update(eu);
			if (room == null) return;

			lastHeadDir = headDir;

			// Update main vel
			if (submerged)
			{
				vel *= 0.75f;
				if (!alive)
				{
					vel.y += 0.1f;
				}
			}
			else
			{
				vel.y -= 0.75f;
			}

			// Update stuff when alive
			if (alive)
			{
				vel += headDir * 0.5f;
			}

			// Update segments
			Vector2 lastSegPos = pos;
			Vector2 lastDragPos = pos;
			tailSin++;
			for (int i = 0; i < actualSegments.Length; i++)
			{
				ref SimpleSegment seg = ref actualSegments[i];
				ref SimpleSegment drag = ref dragSegments[i];

				// Update positions
				seg.lastPos = seg.pos;
				drag.lastPos = drag.pos;

				if (alive)
				{
					Vector2 perpDir = Custom.PerpendicularVector(lastPos - seg.pos);
					seg.pos += perpDir * Mathf.Sin((tailSin * (1f / tailWiggleSpeed) - i * (1f / tailWiggleOffset)) * Mathf.PI * 2f) * Math.Min(2, i) * 0.75f * CornerTailSlowdown;
				}

				// Clamp lengths
				seg.pos = lastSegPos + Vector2.ClampMagnitude(seg.pos - lastSegPos, maxSegLength);
				drag.pos = lastDragPos + Vector2.ClampMagnitude(drag.pos - lastDragPos, maxSegLength);

				// Prepare for next
				lastSegPos = seg.pos;
				lastDragPos = drag.pos;
			}

			if (showDebug && alive)
			{
				DebugDrawing.DrawArrow(room, pos, orbitPoint, Color.red, 1, 5);
				DebugDrawing.DrawText(room, $"{orbitPointScore:N3}/{orbitIdealDistanceScore:N3}", pos, Color.white);
			}
		}

		public override void Act()
		{
			base.Act();
			if (room == null) return;

			if (mySwarm != null)
			{
				maxOrbitIdealDist = Mathf.Max(10f, mySwarm.insectGroupData.Rad);
			}

			// Look for a better orbit point
			float newOrbitScore = OrbitPointScore(mySwarm?.placedObject.pos ?? orbitPoint);
			if (mySwarm != null && !float.IsNegativeInfinity(newOrbitScore))
			{
				orbitPoint = mySwarm.placedObject.pos;
			}
			else
			{
				orbitPointScore = newOrbitScore;
				Vector2 newTestPos = pos + Random.insideUnitCircle * 200f;
				float newScore = OrbitPointScore(newTestPos);
				if (newScore > orbitPointScore)
				{
					orbitPoint = newTestPos;
					orbitPointScore = newScore;
				}

				if (Random.value < 1f / 150f && roomSchool != null)
				{
					Seedling otherSeedling = roomSchool.RandomSeedling(evenUpdate);
					newTestPos = Vector2.Lerp(newTestPos, otherSeedling.orbitPoint, SeedlingDistanceFactor(otherSeedling) * Random.value);
					newScore = OrbitPointScore(newTestPos);
					if (newScore > orbitPointScore || (newScore == orbitPointScore && Random.value < 1f / 60f))
					{
						orbitPoint = newTestPos;
						orbitPointScore = newScore;
					}
				}
			}

			// Look for a better orbit distance
			if (orbitIdealDistance < minOrbitIdealDist)
			{
				orbitIdealDistance = Custom.LerpAndTick(orbitIdealDistance, minOrbitIdealDist, 0.2f, 2f);
			}
			else if (orbitIdealDistance > maxOrbitIdealDist)
			{
				orbitIdealDistance = Custom.LerpAndTick(orbitIdealDistance, maxOrbitIdealDist, 0.2f, 2f);
			}

			Vector2 orbitAwayDir = (pos - orbitPoint).normalized;
			Vector2 orbitPerp = Custom.PerpendicularVector(orbitAwayDir);

			Vector2 distanceTestPoint = orbitPoint + orbitAwayDir * orbitIdealDistance + orbitPerp * 10f;
			orbitIdealDistanceScore = DistanceScore(distanceTestPoint);

			float newTestDistance = orbitIdealDistance + Mathf.Lerp(-Mathf.Min(20f, orbitIdealDistance / 3f), 20f, 0.5f + Mathf.Pow(Random.value, 1.5f) * 0.5f * (Random.value < 0.5f ? -1f : 1f));
			Vector2 newTestPoint = orbitPoint + orbitAwayDir * newTestDistance + orbitPerp * 10f;
			float newTestScore = DistanceScore(newTestPoint);
			if (newTestScore > orbitIdealDistanceScore)
			{
				orbitIdealDistance = newTestDistance;
				orbitIdealDistanceScore = newTestScore;
			}

			// Swim around orbit point
			Vector2 wantToSwimDir = (orbitPoint + orbitAwayDir * orbitIdealDistance + orbitPerp * 10f - pos).normalized;

			// Flee from creatures
			if (creatureAvoider.currentWorstCrit != null)
			{
				float fleeFac = creatureAvoider.FleeSpeed;
				Vector2 creaturePos = creatureAvoider.currentWorstCrit.DangerPos;
				Vector2 creatureAwayDir = (pos - creaturePos).normalized;
				wantToSwimDir = Vector3.Slerp(wantToSwimDir, creatureAwayDir, fleeFac * 0.45f);
			}

			// Stay in placed object bounds
			if (mySwarm != null && OutOfBounds)
			{
				Vector2 towardsCenter = (mySwarm.placedObject.pos - pos).normalized;
				float dist = Vector2.Distance(towardsCenter, pos) - mySwarm.insectGroupData.Rad;
				wantToSwimDir = Vector3.Slerp(wantToSwimDir, towardsCenter, Mathf.InverseLerp(-20f, 60f, dist) * 0.4f);
			}

			// Stay in room bounds
			if (OutOfRoomBounds)
			{
				Vector2 toRoomVec = Vector2.zero;

				if (pos.x < 0)
					toRoomVec.x = -pos.x;
				else
					toRoomVec.x = pos.x - room.PixelWidth;

				if (pos.y < 0)
					toRoomVec.y = -pos.y;
				else
					toRoomVec.y = pos.y - room.PixelHeight;

				Vector2 towardsRoomVec = (new Vector2(room.PixelWidth, room.PixelHeight) - pos).normalized; // toRoomVec.normalized;
				float distanceFromRoom = toRoomVec.magnitude;

				wantToSwimDir = Vector3.Slerp(wantToSwimDir, towardsRoomVec, Mathf.InverseLerp(-20f, 80f, distanceFromRoom) * 0.5f);
			}

			// Actually turn head
			headDir = Vector3.Slerp(headDir, wantToSwimDir, 0.1f);

			if (showDebug)
			{
				DebugDrawing.DrawArrow(room, pos, pos + wantToSwimDir * 7f, Color.green, 1, 3);
			}
		}

		private float SeedlingDistanceFactor(Seedling other)
		{
			if (!alive || !other.alive || other.room != room)
			{
				return 0f;
			}

			float dist = Vector2.Distance(other.pos, pos);
			//return dist < 5f ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 5f, dist)) : (Mathf.Pow(Mathf.InverseLerp(Mathf.Max(30f, maxOrbitIdealDist * 0.5f), 20f, dist), 2f / 3f) * 0.5f + 0.5f);
			return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2f, 10f, dist));
		}

		private static float DistFromCamEdgeFromInside(Vector2 camera, Vector2 point)
		{
			var rect = FloatRect.MakeFromVector2(camera, camera + new Vector2(1366f, 768f));
			if (!rect.Vector2Inside(point)) return 0f;

			return Mathf.Min(point.x - camera.x, camera.x + 1366f - point.x, point.y - camera.y, camera.y + 768f - point.y);
		}

		private float OrbitPointScore(Vector2 testPos)
		{
			if (room?.aimap == null) return float.NegativeInfinity;
			if (room.HasAnySolid(room.GetTilePosition(testPos)) || !room.PointSubmerged(testPos))
			{
				return float.NegativeInfinity;
			}

			float score = 0f;
			int proximity = room.aimap.getTerrainProximity(testPos);
			score += (Mathf.Clamp(proximity, 0, 4) - (6 - Mathf.Clamp(proximity, 6, 10))) * 30f; // max: 120
			if (creatureAvoider.currentWorstCrit != null)
			{
				score -= Mathf.InverseLerp(creatureAvoider.visualRange * 0.75f, 20f, Vector2.Distance(creatureAvoider.currentWorstCrit.DangerPos, testPos)) * 200f;
			}
			if (mySwarm != null)
			{
				float rad = mySwarm.insectGroupData.Rad;
				score += (rad - Vector2.Distance(mySwarm.placedObject.pos, testPos)) / rad * 80f; // bias towards pObj center
			}
			if (!room.GetTile(testPos).DeepWater)
			{
				score -= 100f;
			}

			int camera = room.CameraViewingPoint(testPos);
			if (camera == -1)
			{
				score -= 120f;
			}
			else
			{
				float distFromEdge = DistFromCamEdgeFromInside(room.cameraPositions[camera], testPos);
				score -= Math.Max(0, maxOrbitIdealDist - distFromEdge);
			}

			score -= Math.Max(0, Vector2.Distance(startPos, testPos) - maxOrbitIdealDist * 1.5f) * 2f;

			return score;
		}

		private float DistanceScore(Vector2 testPos)
		{
			if (room?.aimap == null) return float.NegativeInfinity;
			if (room.HasAnySolid(room.GetTilePosition(testPos)) || !room.PointSubmerged(testPos))
			{
				return float.NegativeInfinity;
			}

			float score = 0f;
			score += Mathf.Clamp(room.aimap.getTerrainProximity(testPos), 0, 3) * 25f; // max: 75

			float orbitDist = Vector2.Distance(testPos, orbitPoint);
			score -= Math.Max(-20f, Math.Max(minOrbitIdealDist + 5f - orbitDist, orbitDist - maxOrbitIdealDist)) * 2f;

			Seedling? randomSeedling = roomSchool?.RandomSeedling(evenUpdate);
			if (randomSeedling != null && randomSeedling != this)
			{
				float compareToOther = Mathf.Abs(randomSeedling.orbitIdealDistance - orbitDist);
				score -= Math.Max(-20f, 30f - compareToOther * 4f); // range: -20 to 30 (which means score changes by 20 to -30)
			}

			if (mySwarm != null)
			{
				float rad = mySwarm.insectGroupData.Rad;
				if (orbitDist > rad)
				{
					score -= 80f;
				}
			}
			if (!room.GetTile(testPos).DeepWater)
			{
				score -= 100f;
			}

			int camera = room.CameraViewingPoint(testPos);
			if (camera == -1)
			{
				score -= maxOrbitIdealDist;
			}
			else
			{
				float distFromEdge = DistFromCamEdgeFromInside(room.cameraPositions[camera], testPos);
				score -= Math.Max(0, minOrbitIdealDist * 2f - distFromEdge);
			}

			score -= Math.Max(0, Vector2.Distance(startPos, testPos) - maxOrbitIdealDist * 1.5f);

			return score;
		}

		public override void Destroy()
		{
			RemoveFromRoomSchool();
			base.Destroy();
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			List<FSprite> sprites = [
				new FSprite("BodyA")
				{
					scale = 0.3f,
					anchorY = 1f,
				},
				new FSprite("pixel"),
				];

			for (int i = 0; i < actualSegments.Length; i++)
			{
				sprites.Add(new FSprite("pixel")
				{
					scaleX = 1f,
					scaleY = maxSegLength,
					anchorY = 1f
				});
			}

			sLeaser.sprites = [.. sprites];
			base.InitiateSprites(sLeaser, rCam);
			AddToContainer(sLeaser, rCam, null);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

			// Head
			FSprite headSprite = sLeaser.sprites[0];
			Vector2 headPos = Vector2.Lerp(lastPos, pos, timeStacker);
			Vector2 headRot = Vector3.Slerp(lastHeadDir, headDir, timeStacker);
			headSprite.SetPosition(headPos - camPos);
			headSprite.rotation = Custom.VecToDeg(headRot) + 180f; // flip

			// Eye
			FSprite eyeSprite = sLeaser.sprites[1];
			Vector2 eyePosition = headPos + headRot * 3f;
			eyeSprite.SetPosition(eyePosition - camPos);
			eyeSprite.color = room.PointSubmerged(eyePosition) ? new Color(0f, 1f / 255f, 0f) : rCam.currentPalette.blackColor;
			eyeSprite.isVisible = alive;

			// Tail
			for (int i = 0; i < actualSegments.Length; i++)
			{
				Vector2 currPos = SegmentPos(i);
				Vector2 lastPos = SegmentPos(i - 1);
				FSprite segmentSprite = sLeaser.sprites[i + 2];
				segmentSprite.SetPosition(currPos - camPos);
				segmentSprite.rotation = Custom.AimFromOneVectorToAnother(lastPos, currPos);
				segmentSprite.scaleY = Vector2.Distance(lastPos, currPos) + 1f;
			}

			Vector2 SegmentPos(int i)
			{
				if (i < 0) return Vector2.Lerp(lastPos, pos, timeStacker);
				return actualSegments[i].DrawPos(timeStacker);
			}
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			base.ApplyPalette(sLeaser, rCam, palette);
			Color white = Color.Lerp(palette.fogColor, Color.white, 0.7f);
			for (var i = 0; i < sLeaser.sprites.Length; i++)
			{
				sLeaser.sprites[i].color = white;
			}
		}

		private class RoomTracker(Room room)
		{
			private readonly Room room = room;
			private readonly List<Seedling> seedlings = [];
			private bool lastEvenUpdate = false;

			public void AddSeedling(Seedling seedling)
			{
				if (!seedlings.Contains(seedling))
				{
					seedlings.Add(seedling);
				}
			}

			public void RemoveSeedling(Seedling seedling)
			{
				seedlings.Remove(seedling);
			}

			public List<Seedling> GetSeedlings(bool eu)
			{
				if (eu != lastEvenUpdate)
				{
					lastEvenUpdate = eu;
					for (int i = seedlings.Count - 1; i >= 0; i--)
					{
						if (seedlings[i].room != room || !seedlings[i].alive || seedlings[i].slatedForDeletetion)
						{
							seedlings.RemoveAt(i);
						}
					}
				}
				return seedlings;
			}

			public Seedling RandomSeedling(bool eu)
			{
				List<Seedling> list = GetSeedlings(eu);
				return list[Random.Range(0, list.Count)];
			}
		}
	}
}
