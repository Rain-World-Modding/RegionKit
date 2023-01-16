using System;
using DevInterface;
using UnityEngine;
using static System.Text.RegularExpressions.Regex;
using static RWCustom.Custom;
using static UnityEngine.Mathf;
using Random = UnityEngine.Random;

/// <summary>
/// By LB Gamer/M4rbleL1ne
/// </summary>

namespace RegionKit.Modules.Objects
{
	public class NoWallSlideZones
	{
		public static class Enums_NoWallSlideZones
		{
			public static PlacedObject.Type NoWallSlideZone = new("NoWallSlideZone", true);
		}

		public static void Apply()
		{
			On.Room.Loaded += (orig, self) =>
			{
				orig(self);
				for (var i = 0; i < self.roomSettings.placedObjects.Count; i++)
				{
					var pObj = self.roomSettings.placedObjects[i];
					if (pObj.active && pObj.type == Enums_NoWallSlideZones.NoWallSlideZone)
						self.AddObject(new NoWallSlideZone(self, pObj));
				}
			};
			On.DevInterface.ObjectsPage.CreateObjRep += (orig, self, tp, pObj) =>
			{
				if (tp == Enums_NoWallSlideZones.NoWallSlideZone)
				{
					if (pObj is null)
					{
						self.RoomSettings.placedObjects.Add(pObj = new(tp, null)
						{
							pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new(-683f, 384f), .25f) + DegToVec(Random.value * 360f) * .2f
						});
					}
					var pObjRep = new FloatRectRepresentation(self.owner, $"{tp}_Rep", self, pObj, tp.ToString());
					self.tempNodes.Add(pObjRep);
					self.subNodes.Add(pObjRep);
				}
				else
					orig(self, tp, pObj);
			};
			On.PlacedObject.GenerateEmptyData += (orig, self) =>
			{
				orig(self);
				if (self.type == Enums_NoWallSlideZones.NoWallSlideZone)
					self.data = new FloatRectData(self);
			};
			On.Player.WallJump += (orig, self, direction) =>
			{
				if (self.InsideNWSRects())
					return;
				orig(self, direction);
			};
			On.Player.Update += (orig, self, eu) =>
			{
				orig(self, eu);
				if (self.bodyMode == Player.BodyModeIndex.WallClimb && self.InsideNWSRects() && !self.submerged && self.bodyChunks[0].ContactPoint.x is not 0 && self.bodyChunks[0].ContactPoint.x == self.input[0].x)
				{
					foreach (var b in self.bodyChunks)
						b.contactPoint.x = 0;
					self.bodyMode = Player.BodyModeIndex.Default;
					self.animation = Player.AnimationIndex.None;
				}
			};
		}
	}

	public class NoWallSlideZone : UpdatableAndDeletable
	{
		public PlacedObject pObj;
		public FloatRect rect;

		public NoWallSlideZone(Room room, PlacedObject pObj)
		{
			this.room = room;
			this.pObj = pObj;
			rect = (pObj.data as FloatRectData)!.Rect;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			var r = (pObj.data as FloatRectData)!.Rect;
			if (!rect.EqualsFloatRect(r))
				rect = r;
		}
	}

	public class FloatRectData : PlacedObject.Data
	{
		public Vector2 handlePos;

		public virtual FloatRect Rect => new(Min(owner.pos.x, owner.pos.x + handlePos.x), Min(owner.pos.y, owner.pos.y + handlePos.y), Max(owner.pos.x, owner.pos.x + handlePos.x), Max(owner.pos.y, owner.pos.y + handlePos.y));

		public FloatRectData(PlacedObject owner) : base(owner) => handlePos = new(80f, 80f);

		public override void FromString(string s)
		{
			var sAr = Split(s, "~");
			handlePos.x = float.Parse(sAr[0]);
			handlePos.y = float.Parse(sAr[1]);
		}

		public override string ToString() => $"{handlePos.x}~{handlePos.y}";
	}

	public class FloatRectRepresentation : PlacedObjectRepresentation
	{
		public virtual FloatRectData Data => (pObj.data as FloatRectData)!;

		public FloatRectRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
		{
			subNodes.Add(new Handle(owner, "Float_Rect_Handle", this, new(80f, 80f)));
			(subNodes[subNodes.Count - 1] as Handle)!.pos = Data.handlePos;
			for (var i = 0; i < 5; i++)
			{
				fSprites.Add(new("pixel")
				{
					anchorX = 0f,
					anchorY = 0f
				});
				owner.placedObjectsContainer.AddChild(fSprites[1 + i]);
			}
			fSprites[5].alpha = .05f;
		}

		public override void Refresh()
		{
			base.Refresh();
			var camPos = owner.room.game.cameras[0].pos;
			MoveSprite(1, absPos);
			Data.handlePos = (subNodes[0] as Handle)!.pos;
			var rect = Data.Rect;
			rect.right++;
			rect.top++;
			MoveSprite(1, new Vector2(rect.left, rect.bottom) - camPos);
			fSprites[1].scaleY = rect.Height() + 1f;
			MoveSprite(2, new Vector2(rect.left, rect.bottom) - camPos);
			fSprites[2].scaleX = rect.Width() + 1f;
			MoveSprite(3, new Vector2(rect.right, rect.bottom) - camPos);
			fSprites[3].scaleY = rect.Height() + 1f;
			MoveSprite(4, new Vector2(rect.left, rect.top) - camPos);
			fSprites[4].scaleX = rect.Width() + 1f;
			MoveSprite(5, new Vector2(rect.left, rect.bottom) - camPos);
			fSprites[5].scaleX = rect.Width() + 1f;
			fSprites[5].scaleY = rect.Height() + 1f;
		}
	}

	public static class Extensions
	{
		public static float Height(this FloatRect self) => Math.Abs(self.top - self.bottom);

		public static float Width(this FloatRect self) => Math.Abs(self.right - self.left);

		public static float Area(this FloatRect self) => self.Height() * self.Width();

		public static bool InsideRect(Vector2 vec, FloatRect rect) => vec.x >= rect.left && vec.x <= rect.right && vec.y >= rect.bottom && vec.y <= rect.top;

		public static bool InsideRect(float x, float y, FloatRect rect) => x >= rect.left && x <= rect.right && y >= rect.bottom && y <= rect.top;

		public static bool EqualsFloatRect(this FloatRect self, FloatRect other) => self.left == other.left && self.right == other.right && self.bottom == other.bottom && self.top == other.top;

		public static bool InsideNWSRects(this Player self)
		{
			if (self.room is not null)
			{
				foreach (var uad in self.room.updateList)
				{
					if (uad is NoWallSlideZone nws)
					{
						foreach (var bc in self.bodyChunks)
						{
							if (InsideRect(bc.pos, nws.rect))
								return true;
						}
					}
				}
			}
			return false;
		}
	}
}
