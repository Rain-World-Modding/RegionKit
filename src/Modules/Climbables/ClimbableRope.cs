namespace RegionKit.Modules.Climbables;

public class ClimbableRope : UpdatableAndDeletable, IClimbableVine, IDrawable
{
	protected ManagedData data => placedObject.data as ManagedData;
	protected PlacedObject placedObject;
	protected Vector2 startPos;
	protected Vector2 endPos;
	protected float length;
	protected int nodeCount;
	protected int nsteps;
	protected float stepFactor;
	protected Vector2[,] nodes;
	protected Vector2[,] speeds;
	protected Rope[] ropes;
	protected float[,] lengths;
	protected float[,] twists;

	protected float conRad = 8f;
	private float mass = 0.3f;

	private List<Player> recentlySwingedOff;
	private List<Player> recentlyCrawledOff;
	private bool playerCrawlingOff;
	protected readonly float transmissionFactor = 1.002f;

	protected readonly float pullFactor = 0.9f;

	//protected readonly float stiffnessCoef = 0.0000f;
	//protected readonly float stiffnessDampCoef = 0.02f; // ok up to 0.05

	protected readonly float airFrictionA = 0.000f;
	protected readonly float airFrictionB = 0.000f;

	readonly float externalTransfDisplace = 0.0f;
	readonly float externalTransfSpeed = 1.0f;
	readonly float externalVerticalReduction = 0.4f;


	public ClimbableRope(PlacedObject placedObject, Room instance)
	{
		this.placedObject = placedObject;
		this.room = instance;

		recentlySwingedOff = new List<Player>();
		recentlyCrawledOff = new List<Player>();


		this.startPos = placedObject.pos;
		this.endPos = this.startPos + data.GetValue<Vector2>("vector");
		this.length = data.GetValue<Vector2>("vector").magnitude;

		this.nodeCount = RWCustom.Custom.IntClamp((int)(length / this.conRad) + 1, 2, 200);
		this.conRad = length / (nodeCount - 1);

		this.nsteps = Mathf.CeilToInt(nodeCount / conRad);
		this.stepFactor = 1f / nsteps;

		this.nodes = new Vector2[nodeCount, 2];
		this.speeds = new Vector2[nodeCount, 2];
		this.ropes = new Rope[this.nodeCount - 1];
		this.lengths = new float[this.nodeCount - 1, 2];
		this.twists = new float[this.nodeCount - 1, 2];


		for (int i = 0; i < this.nodeCount; i++)
		{
			Vector2 speed = 0.1f * RWCustom.Custom.RNV(); // Speed
			this.speeds[i, 0] = speed;
			this.speeds[i, 1] = speed;

			Vector2 pos = Vector2.Lerp(startPos, endPos, (float)i / (float)(this.nodeCount - 1));
			this.nodes[i, 0] = pos; // Pos
			this.nodes[i, 1] = pos - speed; // Prev

		}
		this.speeds[0, 0] = Vector2.zero; // anchor
		this.speeds[0, 1] = Vector2.zero; // anchor

		for (int i = 0; i < this.ropes.Length; i++)
		{
			this.ropes[i] = new Rope(room, this.nodes[i, 0], this.nodes[i + 1, 0], 2f);

			this.lengths[i, 0] = this.ropes[i].totalLength;
			this.lengths[i, 1] = this.ropes[i].totalLength;

			this.twists[i, 0] = 0f;
			this.twists[i, 1] = 0f;
		}

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

		//if (ClimbablesMod.ropeWatch != null) ClimbablesMod.ropeWatch.Start();

		// position updated lol
		if (placedObject.pos != nodes[0, 0])
		{
			this.startPos = placedObject.pos;
			//this.endPos = this.startPos + (placedObject.data as PlacedObject.ResizableObjectData).handlePos;
			//this.length = (placedObject.data as PlacedObject.ResizableObjectData).handlePos.magnitude;
			this.endPos = this.startPos + data.GetValue<Vector2>("vector").normalized * this.length;

			for (int i = 0; i < this.nodeCount; i++)
			{
				Vector2 speed = 0.1f * RWCustom.Custom.RNV(); // Speed
				this.speeds[i, 0] = speed;
				this.speeds[i, 1] = speed;

				Vector2 pos = Vector2.Lerp(startPos, endPos, (float)i / (float)(this.nodeCount - 1));
				this.nodes[i, 0] = pos; // Pos
				this.nodes[i, 1] = pos - speed; // Prev

			}
		}


		foreach (var player in recentlySwingedOff)
		{
			player.vineGrabDelay = 3;
		}
		recentlySwingedOff.Clear();


		this.playerCrawlingOff = false;
		foreach (var player in recentlyCrawledOff)
		{
			player.vineGrabDelay = 30;
		}
		recentlyCrawledOff.Clear();

		for (int i = 0; i < this.ropes.Length; i++)
		{
			this.nodes[i + 1, 1] = this.nodes[i + 1, 0];
			this.speeds[i + 1, 1] = this.speeds[i + 1, 0];
			this.lengths[i, 1] = this.lengths[i, 0];
			this.twists[i, 1] = this.twists[i, 0];
		}

		for (int n = 0; n < nsteps; n++)
		{
			for (int i = 0; i < this.ropes.Length; i++)
			{
				this.speeds[i + 1, 0] *= 1 - stepFactor * (airFrictionA + this.speeds[i + 1, 0].magnitude * airFrictionB);
				this.nodes[i + 1, 0] += stepFactor * this.speeds[i + 1, 0];

				// Collision will go here

				this.ropes[i].Update(this.nodes[i, 0], this.nodes[i + 1, 0]);
				this.lengths[i, 0] = this.ropes[i].totalLength;

				//Vector2 pullA = Custom.DirVec(ropes[i].A, ropes[i].AConnect);
				Vector2 pullB = Custom.DirVec(ropes[i].B, ropes[i].BConnect);
				Vector2 pullG = new Vector2(0f, -room.gravity);

				//nodes[i + 1, 0] += stepFactor * pullB * (lengths[i, 0] - conRad);
				nodes[i + 1, 0] += pullB * (lengths[i, 0] - conRad) * pullFactor;

				speeds[i + 1, 0] += stepFactor * pullG;
				// speeds[i + 1, 0] = perpB * Vector2.Dot(speeds[i + 1, 0], perpB);

				//this.ropes[i].Update(this.nodes[i, 0], this.nodes[i + 1, 0]);
				//this.lengths[i, 0] = this.ropes[i].totalLength;
			}

			////// Straighten up
			//for (int i = 1; i < this.ropes.Length; i++)
			//{
			//    Vector2 dirprev = Custom.DirVec(this.ropes[i - 1].A, this.ropes[i - 1].AConnect);
			//    Vector2 dir = Custom.DirVec(this.ropes[i].A, this.ropes[i].AConnect);
			//    Vector2 perp = Custom.PerpendicularVector(dir);

			//    float twist = Custom.Angle(dir, dirprev);
			//    this.twists[i, 0] = twist;
			//    float deltaTwist = (twist - twists[i, 1]) / stepFactor;

			//    Vector2 reaction = stepFactor * perp * conRad * (twist * stiffnessCoef + deltaTwist * stiffnessDampCoef);
			//    speeds[i, 0] -= transmissionFactor * reaction / 2;
			//    speeds[i + 1, 0] += reaction / 2;
			//}

			// Up the chain, propagating "speed" (it's actually forces)
			for (int i = this.ropes.Length - 1; i >= 0; i--)
			{
				Vector2 pullB = Custom.DirVec(ropes[i].B, ropes[i].BConnect);

				Vector2 perpB = Custom.PerpendicularVector(pullB);
				Vector2 relative = speeds[i + 1, 0] - speeds[i, 0];
				Vector2 tangential = perpB * Vector2.Dot(relative, perpB) * transmissionFactor;
				speeds[i, 0] += (relative - tangential);
				//speeds[i + 1, 0] = tangential;
				speeds[i + 1, 0] -= (relative - tangential);
			}

			speeds[0, 0] = Vector2.zero;
		}

		//if (ClimbablesMod.ropeWatch != null) ClimbablesMod.ropeWatch.Stop();
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
		// Joars code :slugmystery:
		Vector2 vector = Vector2.Lerp(this.nodes[0, 1], this.nodes[0, 0], timeStacker);
		vector += RWCustom.Custom.DirVec(Vector2.Lerp(this.nodes[1, 1], this.nodes[1, 0], timeStacker), vector) * 1f;
		float d = 2f;
		for (int i = 0; i < this.nodeCount; i++)
		{
			float num = (float)i / (float)(this.nodeCount - 1);
			Vector2 vector2 = Vector2.Lerp(this.nodes[i, 1], this.nodes[i, 0], timeStacker);
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
		this.playerCrawlingOff = false;
		if (crit is Player)
		{
			Player p = (crit as Player);
			float ropeindexFloat = Mathf.Lerp(0, this.ropes.Length - 1, p.vinePos.floatPos);
			int ropeindex = Mathf.FloorToInt(ropeindexFloat);
			Vector2 speee = speeds[ropeindex, 0];
			Vector2 updir = (ropes[ropeindex].A - ropes[ropeindex].AConnect).normalized;
			Vector2 perp = Custom.PerpendicularVector(updir);
			Vector2 inputDirDigital = new Vector2(p.input[0].x, p.input[0].y);
			Vector2 inputDirAnalog = p.SwimDir(true);


			// player was glitching out on touching walls and semi-going into ledges
			p.standing = true;
			p.ledgeGrabCounter = 0;
			p.bodyMode = Player.BodyModeIndex.Default;


			p.wallSlideCounter = 0;

			// Crawl near the top if on narrow terrain
			if (p.input[0].y == 1 && ropeindexFloat * conRad < 40f && ((this.room.aimap.getAItile(p.bodyChunks[0].pos).narrowSpace) || this.room.aimap.getAItile(p.bodyChunks[1].pos).narrowSpace))
			{
				Debug.Log("ClimbableRope: Player crawl off");
				this.playerCrawlingOff = true;
				this.recentlyCrawledOff.Add(p);
			}

			// Can jump up when near the top
			if (p.input[0].jmp && !p.input[1].jmp)
			{
				if (p.input[0].y != -1)
					p.standing = true;
				if (p.input[0].y == 1 && ropeindex * conRad < 40f)
				{
					p.canJump = 1;
					p.wantToJump = 1;
					p.jumpBoost = 4;
				}
			}

			// hands animate
			if (ropeindexFloat * conRad > 40f && p.input[0].y == 0)
			{
				PlayerGraphics pgraphics = p.graphicsModule as PlayerGraphics;
				Vector2 handTarget = nodes[0, 0];
				float handIndexTargetFloat = ropeindexFloat - 16f / conRad;
				int handIndexTarget = Mathf.FloorToInt(handIndexTargetFloat);
				handTarget = Vector2.Lerp(nodes[handIndexTarget, 0], nodes[handIndexTarget + 1, 0], handIndexTargetFloat - handIndexTarget) + Vector2.Lerp(speeds[handIndexTarget, 0], speeds[handIndexTarget + 1, 0], handIndexTargetFloat - handIndexTarget);
				for (int i = 0; i < pgraphics.hands.Length; i++)
				{
					pgraphics.hands[i].absoluteHuntPos = handTarget + i * 4f * updir;
					pgraphics.hands[i].quickness = 1f;
					pgraphics.hands[i].huntSpeed = 50f;
				}
			}

			// Swing and Swingjump
			if (inputDirDigital.magnitude > 0)
			{
				// update faster
				p.vineClimbCursor = p.vineClimbCursor * 0.2f + p.vineClimbCursor.magnitude * inputDirAnalog * 0.8f;

				if (p.input[0].jmp && !p.input[1].jmp)
				{
					if (Vector2.Dot(inputDirAnalog, speee.normalized) > 0.5f) // jump off gets boost
					{
						// Needed better direction of boost;
						p.jumpBoost = 6;
						Vector2 directionOfBoost = (p.bodyChunks[0].vel.normalized + speee.normalized + updir.normalized + inputDirAnalog.normalized + Vector2.up).normalized;
						float boostSpeed = Mathf.Clamp01(Vector2.Dot(inputDirAnalog.normalized, speee.normalized)) * Custom.LerpMap(ropeindex, 2f, 80f, 2.5f, 5f, 0.5f) * Mathf.Pow(p.vinePos.floatPos, 0.5f);
						//p.bodyChunks[0].vel += directionOfBoost * boostSpeed;
						p.bodyChunks[0].vel -= 0.2f * directionOfBoost * boostSpeed;
						p.bodyChunks[1].vel += 1.0f * directionOfBoost * boostSpeed;
						if (p.input[0].x != 0)
						{
							this.recentlySwingedOff.Add(p);
						}
					}
				}
				else
				{
					// Catch up to speed
					Vector2 speedInDirectionOfSwing = Vector2.Dot(p.bodyChunks[0].vel, speee.normalized) * speee.normalized;
					if (Vector2.Dot(p.bodyChunks[0].vel, speedInDirectionOfSwing) > 0 && speedInDirectionOfSwing.magnitude < speee.magnitude)
					{
						p.bodyChunks[0].vel += speedInDirectionOfSwing.normalized * (speee.magnitude - speedInDirectionOfSwing.magnitude);
					}
					// Extra swing motion
					if (Vector2.Dot(inputDirAnalog, speee.normalized) > 0.5f)
					{
						Vector2 directionOfBoost = (2 * speee.normalized + 2 * (Vector2.Dot(inputDirAnalog.normalized, perp.normalized) * perp).normalized + updir).normalized;
						Vector2 boost = Mathf.Pow(p.vinePos.floatPos, 0.5f) * directionOfBoost * Custom.LerpMap(ropeindex, 2f, 80f, 0.8f, 1.2f, 0.5f);
						// lower body animates :)
						p.bodyChunks[0].vel -= 0.2f * boost;
						p.bodyChunks[1].vel += 1.0f * boost;
					}
				}
			}

			// Cursor modifiers, can really only apply one of these

			// Stop the player from pushing against terrain, fricking hell
			Vector2 contactPoint = new Vector2(p.bodyChunks[0].ContactPoint.x, p.bodyChunks[0].ContactPoint.y);
			if (contactPoint.magnitude > 0 && Vector2.Dot(contactPoint.normalized, updir) > 0f)
			{
				if (Mathf.Abs(contactPoint.x) > 0) // Trim to vertical component
				{
					p.vineClimbCursor = Vector2.up * Vector2.Dot(Vector2.up, p.vineClimbCursor);
				}
				if (Mathf.Abs(contactPoint.y) > 0)// Trim to horiz component
				{
					p.vineClimbCursor = Vector2.right * Vector2.Dot(Vector2.right, p.vineClimbCursor);
				}
			}
			// prevent hangling too low
			else
			if ((nodeCount - 1 - ropeindexFloat) * conRad < 45f)
			{
				p.vinePos.floatPos -= 0.5f * Mathf.Lerp(2.1f, 1.5f, p.room.gravity) / p.room.climbableVines.TotalLength(p.vinePos.vine);
				if (inputDirAnalog.magnitude == 0) // wouldnt animate
				{
					p.animationFrame++;
					if (p.animationFrame > 30)
					{
						p.animationFrame = 0;
					}
				}
				if (Vector2.Dot(updir, p.vineClimbCursor) < 0) // no don't go down
				{
					p.vineClimbCursor = perp * Vector2.Dot(perp, p.vineClimbCursor) + updir * 10f;
				}
			}
			// snap to perpendicular
			else
			if (inputDirDigital.y == 0 && inputDirDigital.x != 0)
			{
				p.vineClimbCursor = p.vineClimbCursor.magnitude * (Vector2.Dot(inputDirAnalog.normalized, perp.normalized) * perp).normalized;
			}
		}
	}

	bool IClimbableVine.CurrentlyClimbable()
	{
		// set in beingclimbed from player.update, this is or way to make the player let go elegantly
		bool returnval = !this.playerCrawlingOff;
		playerCrawlingOff = false;
		return returnval;
	}

	float IClimbableVine.Mass(int index)
	{
		return mass;
	}

	Vector2 IClimbableVine.Pos(int index)
	{
		return this.nodes[index, 0];
	}

	void IClimbableVine.Push(int index, Vector2 movement)
	{
		if (index > 0 && index < nodeCount)
		{
			Vector2 lineDirection;
			if (index == 0 || ropes.Length == 1) lineDirection = ropes[0].A - ropes[0].B;
			else if (index == nodeCount - 1) lineDirection = ropes[index - 1].A - ropes[index - 1].B;
			else
			{
				lineDirection = ropes[index - 1].A - ropes[index - 1].B + ropes[index].A - ropes[index].B;
			}
			lineDirection = lineDirection.normalized;
			//Vector2 perpDirection = Custom.PerpendicularVector(lineDirection.normalized);
			movement = movement * (1 - externalVerticalReduction) + externalVerticalReduction * (movement - lineDirection * Vector2.Dot(lineDirection, movement));

			this.speeds[index, 0] += movement * externalTransfSpeed;
			this.nodes[index, 0] += movement * externalTransfDisplace;
			if (index == nodeCount - 1) this.speeds[index, 0] = Vector2.Lerp(this.speeds[index, 0], this.speeds[index - 1, 0], 0.67f);
		}
	}

	float IClimbableVine.Rad(int index)
	{
		return 3f;
	}

	int IClimbableVine.TotalPositions()
	{
		return this.nodeCount;
	}
}
