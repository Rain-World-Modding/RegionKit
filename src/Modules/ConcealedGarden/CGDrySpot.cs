using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RWCustom;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;

internal class CGDrySpot : UpdatableAndDeletable, IDrawable
{
	private readonly PlacedObject pObj;
	private bool swappedDrawOrder;
	private RoomCamera.SpriteLeaser waterLeaser;

	internal static void Register()
	{
		RegisterManagedObject(new ManagedObjectType("CGDrySpot",
			typeof(CGDrySpot), typeof(CGDrySpotData), typeof(ManagedRepresentation)));
	}

	CGDrySpotData data => pObj.data as CGDrySpotData;

	public CGDrySpot(Room room, PlacedObject pObj)
	{
		this.room = room;
		this.pObj = pObj;
	}

	public override void Update(bool eu)
	{
		base.Update(eu);

		if (room.waterObject != null)
		{
			FloatRect ownrect = data.rect;
			bool inside = false;
			for (int i = 0; i < room.waterObject.surface.GetLength(0); i++)
			{
				Water.SurfacePoint pt = room.waterObject.surface[i, 0];
				if (pt.defaultPos.x > ownrect.left && pt.defaultPos.x < ownrect.right)
				{
					if (!inside && i > 0)
					{
						// first in
						room.waterObject.surface[i - 1, 0].defaultPos.x = ownrect.left - 1f;
						room.waterObject.surface[i - 1, 1].defaultPos.x = ownrect.left - 1f;
						pt.defaultPos.x = ownrect.left + 1f;
						room.waterObject.surface[i, 1].defaultPos.x = ownrect.left + 1f;

						room.waterObject.surface[i - 1, 0].pos *= 0.1f;
						room.waterObject.surface[i - 1, 1].pos *= 0.1f;
						pt.pos *= 0.1f;
						room.waterObject.surface[i, 1].pos *= 0.1f;
					}
					inside = true;
					if (pt.defaultPos.y > ownrect.bottom)
					{
						pt.defaultPos.y = ownrect.bottom;
						room.waterObject.surface[i, 1].defaultPos.y = ownrect.bottom;
					}
				}
				else if (inside) // was inside already
				{
					// first out
					pt.defaultPos.x = ownrect.right + 1f;
					room.waterObject.surface[i, 1].defaultPos.x = ownrect.right + 1f;
					room.waterObject.surface[i - 1, 0].defaultPos.x = ownrect.right - 1f;
					room.waterObject.surface[i - 1, 1].defaultPos.x = ownrect.right - 1f;

					pt.pos *= 0.1f;
					room.waterObject.surface[i, 1].pos *= 0.1f;
					room.waterObject.surface[i - 1, 0].pos *= 0.1f;
					room.waterObject.surface[i - 1, 1].pos *= 0.1f;
					break;
				}
			}
		}
		if (room.updateList[room.updateList.Count - 1] != this)
		{
			room.updateList.Remove(this);
			room.updateList.Add(this); // reorder so runs first!
		}
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (room.waterObject != null)
		{
			if (!this.swappedDrawOrder) // water updates first, so we can update after it :/
			{
				RoomCamera.SpriteLeaser found = null;
				foreach (var item in rCam.spriteLeasers)
				{
					if (item.drawableObject == room.waterObject)
					{
						found = item;
					}
				}
				if (found != null)
				{
					rCam.spriteLeasers.Remove(found);
					rCam.spriteLeasers.Add(found);
					swappedDrawOrder = true;
					this.waterLeaser = found;
				}
			}

			if (this.waterLeaser != null) // redraw water but good
			{
				float y = -10f;
				if (room.waterObject.cosmeticLowerBorder > -1f)
				{
					y = room.waterObject.cosmeticLowerBorder - camPos.y;
				}
				int num = Custom.IntClamp(room.waterObject.PreviousSurfacePoint(camPos.x - 30f), 0, room.waterObject.surface.GetLength(0) - 1);
				int num2 = Custom.IntClamp(num + room.waterObject.pointsToRender, 0, room.waterObject.surface.GetLength(0) - 1);
				for (int i = num; i < num2; i++)
				{
					int num3 = (i - num) * 2;
					Vector2 vector = room.waterObject.surface[i, 0].defaultPos + Vector2.Lerp(room.waterObject.surface[i, 0].lastPos, room.waterObject.surface[i, 0].pos, timeStacker) - camPos + new Vector2(0f, room.waterObject.cosmeticSurfaceDisplace);
					Vector2 vector2 = room.waterObject.surface[i, 1].defaultPos + Vector2.Lerp(room.waterObject.surface[i, 1].lastPos, room.waterObject.surface[i, 1].pos, timeStacker) - camPos + new Vector2(0f, room.waterObject.cosmeticSurfaceDisplace);
					Vector2 vector3 = room.waterObject.surface[i + 1, 0].defaultPos + Vector2.Lerp(room.waterObject.surface[i + 1, 0].lastPos, room.waterObject.surface[i + 1, 0].pos, timeStacker) - camPos + new Vector2(0f, room.waterObject.cosmeticSurfaceDisplace);
					Vector2 v = room.waterObject.surface[i + 1, 1].defaultPos + Vector2.Lerp(room.waterObject.surface[i + 1, 1].lastPos, room.waterObject.surface[i + 1, 1].pos, timeStacker) - camPos + new Vector2(0f, room.waterObject.cosmeticSurfaceDisplace);
					vector = Custom.ApplyDepthOnVector(vector, new Vector2(rCam.sSize.x / 2f, rCam.sSize.y * 0.6666667f), -10f);
					vector2 = Custom.ApplyDepthOnVector(vector2, new Vector2(rCam.sSize.x / 2f, rCam.sSize.y * 0.6666667f), 30f);
					vector3 = Custom.ApplyDepthOnVector(vector3, new Vector2(rCam.sSize.x / 2f, rCam.sSize.y * 0.6666667f), -10f);
					v = Custom.ApplyDepthOnVector(v, new Vector2(rCam.sSize.x / 2f, rCam.sSize.y * 0.6666667f), 30f);
					if (i == num)
					{
						vector2.x -= 100f;
					}
					else if (i == num2 - 1)
					{
						vector2.x += 100f;
					}

					// goes straight down rather than at an angle
					(waterLeaser.sprites[1] as WaterTriangleMesh).MoveVertice(num3, new Vector2(vector.x, y));
					(waterLeaser.sprites[1] as WaterTriangleMesh).MoveVertice(num3 + 1, vector);
					(waterLeaser.sprites[1] as WaterTriangleMesh).MoveVertice(num3 + 2, new Vector2(vector3.x, y));
					(waterLeaser.sprites[1] as WaterTriangleMesh).MoveVertice(num3 + 3, vector3);
				}

				(waterLeaser.sprites[1] as WaterTriangleMesh).color = new Color(0f, 0f, 0f);
			}
		}
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) { sLeaser.sprites = new FSprite[0]; swappedDrawOrder = false; }
	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) { }


	class CGDrySpotData : ManagedData
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
		public CGDrySpotData(PlacedObject owner) : base(owner, new ManagedField[] {
					new Vector2Field("handle", new Vector2(100,100), Vector2Field.VectorReprType.rect)})
		{
		}
	}
}
