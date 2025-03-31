using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;

class CGCosmeticWater : UpdatableAndDeletable, IDrawable
{
	private class CGCosmeticWaterSurface : Water.DefaultSurface
	{
		private float top;

		public CGCosmeticWaterSurface(Water water, Rect bounds) : base(water)
		{
			// Damn you Water.MainSurface...
			totalPoints = (int)(bounds.width / water.triangleWidth) + 1;
			points = new Water.SurfacePoint[totalPoints, 2];
			for (int i = 0; i < totalPoints; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					points[i, j] = new Water.SurfacePoint(new Vector2(bounds.xMin + ((float)i + ((j == 0) ? 0f : 0.5f)) * water.triangleWidth, water.originalWaterLevel));
				}
			}

			top = bounds.yMax;
			bounds = new Rect(bounds.xMin, -100, bounds.width, bounds.yMin + 100);
			waterCutoffs.Clear();
		}

		public override void Update()
		{
			base.Update();
			waveAmplitude = water.waveAmplitude;
			waveSpeed = water.waveSpeed;
			waveLength = water.waveLength;
			rollBackLength = water.rollBackLength;
			rollBackAmp = water.rollBackAmp;
		}

		public override float TargetWaterLevel(float horizontalPosition)
		{
			return top;
		}
	}

	private readonly PlacedObject pObj;
	private readonly Water water;

	CGCosmeticWaterData data => (CGCosmeticWaterData)pObj.data;

	public CGCosmeticWater(Room room, PlacedObject pObj)
	{
		this.room = room;
		this.pObj = pObj;

		FloatRect rect = data.rect;

		water = new Water(room, Mathf.FloorToInt(rect.top / 20f));
		// room.drawableObjects.Add(this.water);
		water.cosmeticLowerBorder = Mathf.FloorToInt(rect.bottom);

		// Water ctor stuff to be adjusted
		water.surfaces = [new CGCosmeticWaterSurface(water, Rect.MinMaxRect(rect.left, rect.bottom, rect.right, rect.top))];
		water.pointsToRender = IntClamp((int)((room.game.rainWorld.options.ScreenSize.x + 60f) / water.triangleWidth) + 2, 0, water.surfaces[0].totalPoints);
		water.waterSounds.rect = rect;
		water.waterSounds.Volume = Mathf.Pow(Mathf.Clamp01((rect.right - rect.left) / room.game.rainWorld.options.ScreenSize.x), 0.7f) * (room.water ? 0.5f : 1f);
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		water.Update();
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		water.InitiateSprites(sLeaser, rCam);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		water.DrawSprites(sLeaser, rCam, timeStacker, camPos);

		/*FloatRect rect = data.rect;
		int num = RWCustom.Custom.IntClamp(water.PreviousSurfacePoint(camPos.x - 30f), 0, water.surface.GetLength(0) - 1);
		int num2 = RWCustom.Custom.IntClamp(num + water.pointsToRender, 0, water.surface.GetLength(0) - 1);
		WaterTriangleMesh mesh0 = ((WaterTriangleMesh)sLeaser.sprites[0]);
		WaterTriangleMesh mesh1 = ((WaterTriangleMesh)sLeaser.sprites[1]);
		for (int i = num; i < num2; i++)
		{
			int num3 = (i - num) * 2;
			// get the values that ended up being used
			Vector2 vector = mesh0.vertices[num3];
			Vector2 vector2 = mesh0.vertices[num3 + 1];
			Vector2 vector3 = mesh0.vertices[num3 + 2];
			// undo the damage
			if (i == num)
			{
				vector2.x += 100f;
			}
			else if (i == num2 - 1)
			{
				vector2.x -= 100f;
			}
			//reapply
			mesh0.MoveVertice(num3, vector);
			mesh0.MoveVertice(num3 + 1, vector2);
			mesh0.MoveVertice(num3 + 2, vector3);
			mesh0.MoveVertice(num3 + 3, vector3);
			float y = rect.bottom - camPos.y;
			mesh1.MoveVertice(num3, new Vector2(vector.x, y));
			mesh1.MoveVertice(num3 + 1, vector);
			mesh1.MoveVertice(num3 + 2, new Vector2(vector3.x, y));
			mesh1.MoveVertice(num3 + 3, vector3);
		}
		mesh1.MoveVertice(0, rect.GetCorner(FloatRect.CornerLabel.D) - camPos);
		mesh1.MoveVertice(1, rect.GetCorner(FloatRect.CornerLabel.A) - camPos);
		mesh1.MoveVertice(((WaterTriangleMesh)sLeaser.sprites[1]).vertices.Length - 2, rect.GetCorner(FloatRect.CornerLabel.B) - camPos);
		mesh1.MoveVertice(((WaterTriangleMesh)sLeaser.sprites[1]).vertices.Length - 1, rect.GetCorner(FloatRect.CornerLabel.C) - camPos);*/
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		water.ApplyPalette(sLeaser, rCam, palette);
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		water.AddToContainer(sLeaser, rCam, newContatiner);
	}

	public class CGCosmeticWaterData : ManagedData
	{
		public FloatRect rect
		{
			get
			{
				return new FloatRect(
					Mathf.Min(owner.pos.x, owner.pos.x + handlePos.x),
					Mathf.Min(owner.pos.y, owner.pos.y + handlePos.y),
					Mathf.Max(owner.pos.x, owner.pos.x + handlePos.x),
					Mathf.Max(owner.pos.y, owner.pos.y + handlePos.y));
			}
		}
#pragma warning disable 0649
		[BackedByField("handle")]
		public Vector2 handlePos;
#pragma warning restore 0649
		public CGCosmeticWaterData(PlacedObject owner) : base(owner, new ManagedField[] {
					new Vector2Field("handle", new Vector2(100,100), Vector2Field.VectorReprType.rect)})
		{
		}
	}
}
