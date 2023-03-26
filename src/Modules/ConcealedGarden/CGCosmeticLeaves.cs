using DevInterface;
using System;
using System.Collections.Generic;
using UnityEngine;
using RWCustom;

namespace RegionKit.Modules.ConcealedGarden;

internal class CGCosmeticLeaves : UpdatableAndDeletable, IDrawable
{
	private CosmeticLeavesObjectData data => (pObj.data as CosmeticLeavesObjectData);
	private PlacedObject pObj;
	private Color[] colors;
	private List<Branch> branches;
	private List<Leaf> leaves;

	public CGCosmeticLeaves(PlacedObject placedObject, Room instance)
	{
		this.pObj = placedObject;
		this.room = instance;

		this.branches = new List<Branch>();
		this.leaves = new List<Leaf>();

		var oldseed = UnityEngine.Random.seed;
		UnityEngine.Random.seed = (int)(this.pObj.pos.x * 100f);
		new Branch(this, null, 0, data.handleA, data.handleB.magnitude / 2f);
		UnityEngine.Random.seed = oldseed;

		foreach (var item in branches)
		{
			item.UpdatePositions();
		}
		foreach (var item in leaves)
		{
			item.UpdatePositions();
		}
	}

	public abstract class BranchPart
	{
		protected static float windspeed = 0.01f;
		protected CGCosmeticLeaves owner;
		protected BranchPart connectsTo;
		protected int connectsToIndex;
		protected float rotation;

		protected Vector3[] relpos;
		protected Vector3[,] pos;

		protected Vector3 attachedPoint { get { return connectsTo?.pos[0, connectsToIndex] ?? new Vector3(owner.pObj.pos.x, owner.pObj.pos.y, owner.data.depth); } }
		protected float parentRotation { get { return connectsTo?.rotation ?? 0f; } }
		public void UpdatePositions()
		{
			Vector3 rootpos = attachedPoint;
			Quaternion rotP = Quaternion.Euler(0, 0, parentRotation);
			Vector3 relative = rotP * relpos[relpos.Length - 1];
			Vector2 dir = new Vector2(relative.x, relative.y);
			float windfactor = dir.normalized.y * Mathf.Sign(dir.x) *
				Mathf.PerlinNoise(windspeed * (rootpos.x + owner.room.game.clock), 0.1f * windspeed * rootpos.y)
				/ Mathf.Pow(dir.magnitude, 0.2f)
				* Mathf.Lerp(1f, 0.33f, rootpos.z / 30f);
			this.rotation = parentRotation + 12f * windfactor;
			Quaternion rotQ = Quaternion.Euler(0, 0, rotation);
			for (int i = 0; i < relpos.Length; i++)
			{
				pos[1, i] = pos[0, i];
				pos[0, i] = rootpos + rotQ * relpos[i];
				// maybe instead of relative to rootpos we could have stacked relative positions for better movement ?
			}
		}
	}

	public class Branch : BranchPart
	{
		private readonly Vector3 goal;
		private float[] thicknesses;

		public Branch(CGCosmeticLeaves owner, Branch connectsTo, int connectsToIndex, Vector3 goal, float thicknessAtBase)
		{
			owner.branches.Add(this);
			this.owner = owner;
			this.connectsTo = connectsTo;
			this.connectsToIndex = connectsToIndex;
			this.goal = goal;

			// Grow in a direction
			int nnodes = Mathf.CeilToInt(thicknessAtBase / 0.7f);
			Vector3 ppos = Vector3.zero;
			Vector3 dir = goal / nnodes;
			float jump = goal.magnitude / nnodes;
			this.relpos = new Vector3[nnodes];
			this.thicknesses = new float[nnodes];
			this.pos = new Vector3[2, nnodes];
			relpos[0] = Vector3.zero;
			this.thicknesses[0] = thicknessAtBase;
			for (int i = 1; i < nnodes; i++)
			{
				float inbranch = nnodes > 1 ? (float)i / (float)(nnodes - 1) : 1f;
				float oldZ = dir.z;
				// last (dir + random) jump  + goal pull
				dir = (dir * 0.8f + UnityEngine.Random.insideUnitSphere).normalized * jump * (0.5f + 0.2f * UnityEngine.Random.value)
					+ (goal - ppos) * (1 - (nnodes - i) / (nnodes)) * (0.2f + 0.3f * inbranch * UnityEngine.Random.value);
				dir.z = oldZ >= 0f ? Mathf.Clamp(dir.z, Mathf.Sign(oldZ) / 1024f, Mathf.Sign(oldZ)) : Mathf.Clamp(dir.z, Mathf.Sign(oldZ), Mathf.Sign(oldZ) / 1024f);
				ppos += dir;
				dir.Normalize();
				thicknessAtBase = Mathf.Max(1f, thicknessAtBase - 0.7f);
				relpos[i] = ppos;
				thicknesses[i] = thicknessAtBase;

				if (thicknessAtBase > 2f && (nnodes - i - 1) > 2 && UnityEngine.Random.value < Mathf.Pow(owner.data.branches, 1f / thicknessAtBase))
				{
					new Branch(owner, this, i, (dir * 0.5f + UnityEngine.Random.insideUnitSphere + (Vector3)UnityEngine.Random.insideUnitCircle) * jump * (nnodes - i - 1) * 0.6f, thicknessAtBase * 0.8f);
				}
				if (thicknessAtBase < 5f && UnityEngine.Random.value < Mathf.Pow(owner.data.leaves, thicknessAtBase / 2f))
				{
					Vector3 sproutDir = (dir * 0.6f + UnityEngine.Random.insideUnitSphere + (Vector3)UnityEngine.Random.insideUnitCircle).normalized;
					if (Mathf.Abs(sproutDir.y) < 0.2f) sproutDir.y = -0.2f;
					sproutDir.y = Mathf.Sign(sproutDir.y) * Mathf.Pow(Mathf.Abs(sproutDir.y), 0.8f); // favor anything but flat to the camera
					new Leaf(owner, this, i, sproutDir.normalized);
				}
			}
		}

		public void Update()
		{
			// Update dynamic poss
			// wind and such would go here

			UpdatePositions();
		}

		internal FSprite InitSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			TriangleMesh trimesh = TriangleMesh.MakeLongMesh(relpos.Length - 1, false, true);
			trimesh.shader = this.owner.room.game.rainWorld.Shaders["CustomDepth"];
			return trimesh;
		}

		internal void ApplyPalette(FSprite fSprite, RoomPalette palette)
		{
			TriangleMesh trimesh = fSprite as TriangleMesh;
			for (int i = 0; i < relpos.Length - 1; i++)
			{
				Vector3 a = pos[0, i];
				Vector3 b = pos[0, i + 1];
				Vector2 ab = b - a;
				//Vector2 per = Custom.PerpendicularVector(ab);
				float shine = Mathf.Abs(ab.x) / ab.magnitude;
				Color[] pala = palette.texture.GetPixels(Mathf.Clamp(Mathf.FloorToInt(a.z), 0, 29), 3, 1, 3);
				Color[] palb = palette.texture.GetPixels(Mathf.Clamp(Mathf.FloorToInt(b.z), 0, 29), 3, 1, 3);

				Color upperA = Color.Lerp(pala[1], pala[0], shine);
				Color lowerA = Color.Lerp(pala[1], pala[2], shine);
				Color upperB = Color.Lerp(palb[1], palb[0], shine);
				Color lowerB = Color.Lerp(palb[1], palb[2], shine);
				upperA.a = 1f - (Mathf.Clamp(a.z, 0f, 29f) / 29f);
				lowerA.a = upperA.a;
				upperB.a = 1f - (Mathf.Clamp(b.z, 0f, 29f) / 29f);
				lowerB.a = upperB.a;

				trimesh.verticeColors[i * 4 + 0] = ab.x < 0 ? upperA : lowerA;
				trimesh.verticeColors[i * 4 + 1] = ab.x < 0 ? lowerA : upperA;
				trimesh.verticeColors[i * 4 + 2] = ab.x < 0 ? upperB : lowerB;
				//no more pointy tip if (i == relpos.Length - 2) continue;
				trimesh.verticeColors[i * 4 + 3] = ab.x < 0 ? lowerB : upperB;
			}
		}

		internal void DrawSprites(FSprite fSprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			TriangleMesh trimesh = fSprite as TriangleMesh;
			for (int i = 0; i < relpos.Length - 1; i++)
			{
				Vector3 a = Vector2.Lerp(pos[1, i], pos[0, i], timeStacker);
				Vector3 b = Vector2.Lerp(pos[1, i + 1], pos[0, i + 1], timeStacker);
				Vector2 ab = b - a;
				Vector2 per = Custom.PerpendicularVector(ab);

				trimesh.vertices[i * 4 + 0] = (Vector2)a + per * this.thicknesses[i] - camPos;
				trimesh.vertices[i * 4 + 1] = (Vector2)a - per * this.thicknesses[i] - camPos;
				trimesh.vertices[i * 4 + 2] = (Vector2)b + per * this.thicknesses[i + 1] - camPos;
				//no more pointy tip if (i == relpos.Length - 2) continue;
				trimesh.vertices[i * 4 + 3] = (Vector2)b - per * this.thicknesses[i + 1] - camPos;
			}
			trimesh.Refresh();
		}
	}

	public class Leaf : BranchPart
	{
		private Vector3 dir;
		private float[] widths;

		public Leaf(CGCosmeticLeaves owner, Branch connectsTo, int connectsToIndex, Vector3 direction)
		{
			this.owner = owner;
			owner.leaves.Add(this);
			this.connectsTo = connectsTo;
			this.connectsToIndex = connectsToIndex;
			this.dir = direction;

			this.widths = new float[4] { 0.5f, 6f, 4, 0.4f };
			this.relpos = new Vector3[4];
			for (int i = 0; i < relpos.Length; i++)
			{
				relpos[i] = dir * i * 3f;
			}
			this.pos = new Vector3[2, 4];
		}

		public void Update()
		{
			UpdatePositions();
		}

		internal FSprite InitSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			TriangleMesh trimesh = TriangleMesh.MakeLongMesh(this.widths.Length, true, true);
			trimesh.shader = rCam.game.rainWorld.Shaders["CustomDepth"];
			return trimesh;
		}

		internal void ApplyPalette(FSprite fSprite, RoomPalette palette)
		{
			Vector3 a = attachedPoint;
			Color light = Color.Lerp(owner.colors[2], owner.colors[0], (Mathf.Clamp(a.z, 0f, 29f) / 29f));
			Color dark = Color.Lerp(owner.colors[3], owner.colors[1], (Mathf.Clamp(a.z, 0f, 29f) / 29f));

			//fSprite.color = light;
			TriangleMesh trimesh = fSprite as TriangleMesh;
			//fSprite.alpha = Mathf.InverseLerp(0, 30, attachedPoint.z);
			Vector2 prev = pos[0, 0];

			for (int i = 0; i < relpos.Length - 1; i++)
			{
				light.a = 1f - (Mathf.Clamp(pos[0, i].z, 0f, 29f) / 29f);
				dark.a = 1f - (Mathf.Clamp(pos[0, i].z, 0f, 29f) / 29f);
				Vector2 next = pos[0, i + 1];
				bool up = (next.y - prev.y) >= 0f;
				for (int j = 0; j < 4; j++)
				{
					trimesh.verticeColors[i * 4 + j] = up ? dark : light;
				}
				prev = next;
			}
			for (int j = 0; j < 3; j++)
			{
				trimesh.verticeColors[(relpos.Length - 1) * 4 + j] = light;
			}
		}

		internal void DrawSprites(FSprite fSprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			TriangleMesh trimesh = fSprite as TriangleMesh;
			Vector2 horiz = new Vector2(1f, 0f);
			Vector2 prev = Vector2.Lerp(pos[1, 0], pos[0, 0], timeStacker) - camPos;
			for (int i = 0; i < relpos.Length - 1; i++)
			{
				Vector2 cur = Vector2.Lerp(pos[1, i + 1], pos[0, i + 1], timeStacker) - camPos;
				trimesh.vertices[i * 4] = prev + horiz * this.widths[i];
				trimesh.vertices[i * 4 + 1] = prev - horiz * this.widths[i];
				trimesh.vertices[i * 4 + 2] = cur + horiz * this.widths[i + 1];
				trimesh.vertices[i * 4 + 3] = cur - horiz * this.widths[i + 1];
				prev = cur;
			}
			int last = relpos.Length - 1;
			trimesh.vertices[last * 4] = prev + horiz * this.widths[last];
			trimesh.vertices[last * 4 + 1] = prev - horiz * this.widths[last];
			trimesh.vertices[last * 4 + 2] = Vector2.Lerp(pos[1, last], pos[0, last], timeStacker) - camPos;

			//trimesh.Refresh();

			//fSprite.SetPosition((Vector2)attachedPoint - camPos);
			fSprite.alpha = 1f - (Mathf.Clamp(attachedPoint.z, 0f, 30f) / 30f);
			trimesh.Refresh();
		}
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		foreach (var item in branches)
		{
			item.Update();
		}
		foreach (var item in leaves)
		{
			item.Update();
		}
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[branches.Count + leaves.Count];

		for (int i = 0; i < branches.Count; i++)
		{
			sLeaser.sprites[i] = branches[i].InitSprite(sLeaser, rCam);
		}
		for (int i = 0; i < leaves.Count; i++)
		{
			sLeaser.sprites[branches.Count + i] = leaves[i].InitSprite(sLeaser, rCam);
		}
		this.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
		this.AddToContainer(sLeaser, rCam, null);
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		if (newContatiner == null)
		{
			newContatiner = rCam.ReturnFContainer("Water");
		}
		foreach (FSprite fsprite in sLeaser.sprites)
		{
			fsprite.RemoveFromContainer();
			newContatiner.AddChild(fsprite);
		}
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		switch (data.colorType)
		{
		case CosmeticLeavesObjectData.CosmeticLeavesColor.EffectColor1:
			this.colors = rCam.currentPalette.texture.GetPixels(30, 4, 2, 2);
			break;
		case CosmeticLeavesObjectData.CosmeticLeavesColor.EffectColor2:
			this.colors = rCam.currentPalette.texture.GetPixels(30, 2, 2, 2);
			break;
		case CosmeticLeavesObjectData.CosmeticLeavesColor.EffectColorIndex:
			this.colors = RoomCamera.allEffectColorsTexture.GetPixels(data.colorIndex * 2, 2, 2, 2);
			break;
		default:
			break;
		}

		for (int i = 0; i < branches.Count; i++)
		{
			branches[i].ApplyPalette(sLeaser.sprites[i], palette);
		}
		for (int i = 0; i < leaves.Count; i++)
		{
			leaves[i].ApplyPalette(sLeaser.sprites[branches.Count + i], palette);
		}
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		ApplyPalette(sLeaser, rCam, rCam.currentPalette);
		for (int i = 0; i < branches.Count; i++)
		{
			branches[i].DrawSprites(sLeaser.sprites[i], sLeaser, rCam, timeStacker, camPos);
		}
		for (int i = 0; i < leaves.Count; i++)
		{
			leaves[i].DrawSprites(sLeaser.sprites[branches.Count + i], sLeaser, rCam, timeStacker, camPos);
		}
	}


	public class CosmeticLeavesObjectData : ManagedData
	{
#pragma warning disable 0649
		[BackedByField("00ha")]
		public Vector2 handleA;
		[BackedByField("00hb")]
		public Vector2 handleB;

		public enum CosmeticLeavesColor
		{
			EffectColor1,
			EffectColor2,
			EffectColorIndex,
		}
		[BackedByField("01ct")]
		public CosmeticLeavesColor colorType;

		[FloatField("02dp", 0f, 30f, 2f, displayName: "Depth")]
		public float depth;
		[FloatField("03br", 0f, 1f, 0.2f, increment: 0.01f, displayName: "Branches")]
		internal float branches;
		[FloatField("04le", 0f, 1f, 0.8f, increment: 0.01f, displayName: "Leaves")]
		internal float leaves;
		[IntegerField("05ci", 0, 20, 0, displayName: "ColorIndex")]
		internal int colorIndex;



#pragma warning restore 0649
		public CosmeticLeavesObjectData(PlacedObject owner) : base(owner, new ManagedField[] {
				new Vector2Field("00ha", new Vector2(0, 100), Vector2Field.VectorReprType.line),
				new DrivenVector2Field("00hb", "00ha", new Vector2(-200, 0)),
				new EnumField<CosmeticLeavesColor>("01ct", CosmeticLeavesColor.EffectColor1, displayName:"Color Type"),
			})
		{ }
	}
}
