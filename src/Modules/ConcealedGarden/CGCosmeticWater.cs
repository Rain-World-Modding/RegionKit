using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;

class CGCosmeticWater : UpdatableAndDeletable, IDrawable
{
	private PlacedObject pObj;
	private Water water;

	CGCosmeticWaterData data => pObj.data as CGCosmeticWaterData;

	public CGCosmeticWater(Room room, PlacedObject pObj)
	{
		this.room = room;
		this.pObj = pObj;

		FloatRect rect = data.rect;

		this.water = new Water(room, Mathf.FloorToInt(rect.top / 20f));
		// room.drawableObjects.Add(this.water);
		this.water.cosmeticLowerBorder = Mathf.FloorToInt(rect.bottom);

		// Water ctor stuff to be adjusted
		this.water.surface = new Water.SurfacePoint[(int)((rect.right - rect.left) / this.water.triangleWidth) + 1, 2];
		for (int i = 0; i < this.water.surface.GetLength(0); i++)
		{
			for (int j = 0; j < 2; j++)
			{
				this.water.surface[i, j] = new Water.SurfacePoint(new Vector2(rect.left + ((float)i + ((j != 0) ? 0.5f : 0f)) * this.water.triangleWidth, this.water.originalWaterLevel));
			}
		}
		this.water.pointsToRender = RWCustom.Custom.IntClamp((int)((room.game.rainWorld.options.ScreenSize.x + 60f) / this.water.triangleWidth) + 2, 0, this.water.surface.GetLength(0));
		this.water.waterSounds.rect = rect;
		this.water.waterSounds.Volume = Mathf.Pow(Mathf.Clamp01((rect.right - rect.left) / room.game.rainWorld.options.ScreenSize.x), 0.7f) * (room.water ? 0.5f : 1f);
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		this.water.Update();
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		this.water.InitiateSprites(sLeaser, rCam);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		this.water.DrawSprites(sLeaser, rCam, timeStacker, camPos);

		FloatRect rect = data.rect;
		int num = RWCustom.Custom.IntClamp(this.water.PreviousSurfacePoint(camPos.x - 30f), 0, this.water.surface.GetLength(0) - 1);
		int num2 = RWCustom.Custom.IntClamp(num + this.water.pointsToRender, 0, this.water.surface.GetLength(0) - 1);
		WaterTriangleMesh mesh0 = (sLeaser.sprites[0] as WaterTriangleMesh);
		WaterTriangleMesh mesh1 = (sLeaser.sprites[1] as WaterTriangleMesh);
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
		mesh1.MoveVertice((sLeaser.sprites[1] as WaterTriangleMesh).vertices.Length - 2, rect.GetCorner(FloatRect.CornerLabel.B) - camPos);
		mesh1.MoveVertice((sLeaser.sprites[1] as WaterTriangleMesh).vertices.Length - 1, rect.GetCorner(FloatRect.CornerLabel.C) - camPos);
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		this.water.ApplyPalette(sLeaser, rCam, palette);
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		this.water.AddToContainer(sLeaser, rCam, newContatiner);
	}

	public class CGCosmeticWaterData : ManagedData
	{
		public FloatRect rect
		{
			get
			{
				return new FloatRect(
					Mathf.Min(this.owner.pos.x, this.owner.pos.x + this.handlePos.x),
					Mathf.Min(this.owner.pos.y, this.owner.pos.y + this.handlePos.y),
					Mathf.Max(this.owner.pos.x, this.owner.pos.x + this.handlePos.x),
					Mathf.Max(this.owner.pos.y, this.owner.pos.y + this.handlePos.y));
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
