using RegionKit.Modules.Objects;

namespace RegionKit.Modules.Climbables;

public class ClimbableArc : UpdatableAndDeletable, IClimbJumpVine, IDrawable
{
	protected ManagedData data => placedObject.data as ManagedData;
	private Vector2[] _Quad
	{
		get
		{
			var vecs = data.GetValue<Vector2[]>("vectors")!;
			return new[]
			{
			vecs[1],
			vecs[2],
			vecs[3]
		};
		}
	}
	protected const float nodeDistance = 10f;
	public PlacedObject placedObject;
	protected int nodeCount;
	protected bool nodeCountChanged;
	private Vector2[] nodes;

	public ClimbableArc(PlacedObject placedObject, Room instance)
	{
		this.placedObject = placedObject;
		this.room = instance;

		UpdateNodes();

		if (room.climbableVines == null)
		{
			room.climbableVines = new ClimbableVinesSystem();
			room.AddObject(room.climbableVines);
		}
		room.climbableVines.vines.Add(this);
	}

	public override void Update(bool eu)
	{
		base.Update(eu);

		UpdateNodes();

	}

	private void UpdateNodes()
	{
		float heuristicDistance = _Quad[0].magnitude;
		heuristicDistance += (_Quad[1] - _Quad[0]).magnitude;
		heuristicDistance += (_Quad[2] - _Quad[1]).magnitude;
		int newNodeCount = 2 + Mathf.CeilToInt(heuristicDistance / nodeDistance);

		if (Mathf.Abs((float)(newNodeCount - nodeCount) / (float)nodeCount) > 0.2f)
		{
			nodeCountChanged = true;
			nodeCount = newNodeCount;
			this.nodes = new Vector2[nodeCount];
		}


		Vector2 posA = placedObject.pos;
		Vector2 posB = posA + _Quad[0];
		Vector2 posC = posA + _Quad[1];
		Vector2 posD = posA + _Quad[2];

		float step = 1f / (nodeCount - 1);
		for (int i = 0; i < nodeCount; i++)
		{
			float t = step * i;
			float num = 1f - t;
			Vector2 pt = num * num * num * posA + 3f * num * num * t * posB + 3f * num * t * t * posD + t * t * t * posC;
			this.nodes[i] = pt;
		}
	}

	void IDrawable.InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = TriangleMesh.MakeLongMeshAtlased(this.nodeCount, false, true);

		(this as IDrawable).AddToContainer(sLeaser, rCam, null);
	}
	void IDrawable.AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		sLeaser.sprites[0].RemoveFromContainer();
		if (newContatiner == null)
		{
			newContatiner = rCam.ReturnFContainer("Midground");
		}
		newContatiner.AddChild(sLeaser.sprites[0]);
	}

	void IDrawable.ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		sLeaser.sprites[0].color = palette.blackColor;
	}

	void IDrawable.DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (nodeCountChanged)
		{
			sLeaser.sprites[0].RemoveFromContainer();
			sLeaser.sprites[0] = TriangleMesh.MakeLongMeshAtlased(this.nodeCount, false, true);

			(this as IDrawable).AddToContainer(sLeaser, rCam, null);
			(this as IDrawable).ApplyPalette(sLeaser, rCam, rCam.currentPalette);
			nodeCountChanged = false;
		}

		// Joars code :slugmystery:
		Vector2 vector = this.nodes[0];
		float d = 2f;
		for (int i = 0; i < this.nodeCount; i++)
		{
			float num = (float)i / (float)(this.nodeCount - 1);
			Vector2 vector2 = this.nodes[i];
			Vector2 normalized = (vector - vector2).normalized;
			Vector2 a = RWCustom.Custom.PerpendicularVector(normalized);
			(sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4, vector - a * d - camPos);
			(sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 1, vector + a * d - camPos);
			(sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - a * d - camPos);
			(sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + a * d - camPos);
			vector = vector2;
		}
	}

	void IClimbableVine.BeingClimbedOn(Creature crit)
	{
		//pass
	}

	bool IClimbableVine.CurrentlyClimbable()
	{
		return true;
	}

	float IClimbableVine.Mass(int index)
	{
		return 1000f;
	}

	Vector2 IClimbableVine.Pos(int index)
	{
		return this.nodes[index];
	}

	void IClimbableVine.Push(int index, Vector2 movement)
	{
		// pass
	}

	float IClimbableVine.Rad(int index)
	{
		return 2f;
	}

	int IClimbableVine.TotalPositions()
	{
		return this.nodeCount;
	}

	SoundID IClimbJumpVine.GrabSound() => SoundID.Slugcat_Grab_Beam;

	SoundID IClimbJumpVine.ClimbSound() => SoundID.Slugcat_Climb_Along_Horizontal_Beam;

	bool IClimbJumpVine.JumpAllowed() => true;
}
