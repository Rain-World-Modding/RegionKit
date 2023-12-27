using DevInterface;
using static System.Text.RegularExpressions.Regex;
using static UnityEngine.Mathf;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace RegionKit.Modules.Objects;

/// <summary>
/// By LB/M4rbleL1ne.
/// Prevents the player from sliding on walls
/// </summary>
public static class NoWallSlideZones
{
	internal static void Apply()
	{
		On.Player.WallJump += PlayerWallJump;
		On.Player.Update += PlayerUpdate;
	}

	internal static void Undo()
	{
		On.Player.WallJump -= PlayerWallJump;
		On.Player.Update -= PlayerUpdate;
	}

	private static void PlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
	{
		orig(self, eu);
		BodyChunk[] bs = self.bodyChunks;
		if (self.bodyMode == Player.BodyModeIndex.WallClimb && self.InsideNWSRects() && !self.submerged && bs[0].ContactPoint.x != 0 && bs[0].ContactPoint.x == self.input[0].x)
		{
			for (var i = 0; i < bs.Length; i++)
				bs[i].contactPoint.x = 0;
			self.bodyMode = Player.BodyModeIndex.Default;
			self.animation = Player.AnimationIndex.None;
		}
	}

	private static void PlayerWallJump(On.Player.orig_WallJump orig, Player self, int direction)
	{
		if (self.InsideNWSRects())
			return;
		orig(self, direction);
	}
}

/// <summary>
/// By LB/M4rbleL1ne.
/// Prevents the player from sticking to walls in a selected area.
/// </summary>
public class NoWallSlideZone : UpdatableAndDeletable
{
	private readonly PlacedObject _pObj;
	internal FloatRect _rect;

	///<inheritdoc/>
	public NoWallSlideZone(Room room, PlacedObject pObj)
	{
		this.room = room;
		_pObj = pObj;
		_rect = (pObj.data as FloatRectData)!.Rect;
	}

	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);
		FloatRect r = (_pObj.data as FloatRectData)!.Rect;
		if (!_rect.EqualsFloatRect(r))
			_rect = r;
	}
}

internal sealed class FloatRectData : PlacedObject.Data
{
	public Vector2 handlePos;

	public FloatRect Rect
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			float posX = owner.pos.x, posY = owner.pos.y, hPosX = handlePos.x, hPosY = handlePos.y;
			return new(Min(posX, posX + hPosX), Min(posY, posY + handlePos.y), Max(posX, posX + hPosX), Max(posY, posY + hPosY));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public FloatRectData(PlacedObject owner) : base(owner) => handlePos = new(80f, 80f);

	public override void FromString(string s)
	{
		var sAr = Split(s, "~");
		float.TryParse(sAr[0], NumberStyles.Any, CultureInfo.InvariantCulture, out handlePos.x);
		float.TryParse(sAr[1], NumberStyles.Any, CultureInfo.InvariantCulture, out handlePos.y);
		unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(sAr, 2);
	}

	public override string ToString() => SaveUtils.AppendUnrecognizedStringAttrs(SaveState.SetCustomData(this, $"{handlePos.x}~{handlePos.y}"), "~", unrecognizedAttributes);
}

internal sealed class FloatRectRepresentation : PlacedObjectRepresentation
{
	private FloatRectData Data
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (pObj.data as FloatRectData)!;
	}

	public FloatRectRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
	{
		subNodes.Add(new Handle(owner, "Float_Rect_Handle", this, new(80f, 80f)));
		(subNodes[^1] as Handle)!.pos = Data.handlePos;
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
		List<FSprite> fsprs = fSprites;
		Vector2 camPos = owner.game.cameras[0].pos;
		MoveSprite(1, absPos);
		Data.handlePos = (subNodes[0] as Handle)!.pos;
		FloatRect rect = Data.Rect;
		rect.right++;
		rect.top++;
		MoveSprite(1, new Vector2(rect.left, rect.bottom) - camPos);
		fsprs[1].scaleY = rect.Height() + 1f;
		MoveSprite(2, new Vector2(rect.left, rect.bottom) - camPos);
		fsprs[2].scaleX = rect.Width() + 1f;
		MoveSprite(3, new Vector2(rect.right, rect.bottom) - camPos);
		fsprs[3].scaleY = rect.Height() + 1f;
		MoveSprite(4, new Vector2(rect.left, rect.top) - camPos);
		fsprs[4].scaleX = rect.Width() + 1f;
		MoveSprite(5, new Vector2(rect.left, rect.bottom) - camPos);
		fsprs[5].scaleX = rect.Width() + 1f;
		fsprs[5].scaleY = rect.Height() + 1f;
	}
}

internal static class Extensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Height(this FloatRect self) => Math.Abs(self.top - self.bottom);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Width(this FloatRect self) => Math.Abs(self.right - self.left);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Area(this FloatRect self) => self.Height() * self.Width();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InsideRect(Vector2 vec, FloatRect rect) => vec.x >= rect.left && vec.x <= rect.right && vec.y >= rect.bottom && vec.y <= rect.top;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InsideRect(float x, float y, FloatRect rect) => x >= rect.left && x <= rect.right && y >= rect.bottom && y <= rect.top;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool EqualsFloatRect(this FloatRect self, FloatRect other) => self.left == other.left && self.right == other.right && self.bottom == other.bottom && self.top == other.top;

	public static bool InsideNWSRects(this Player self)
	{
		if (self.room?.updateList is List<UpdatableAndDeletable> lst)
		{
			BodyChunk[] bs = self.bodyChunks;
			for (var i = 0; i < lst.Count; i++)
			{
				if (lst[i] is NoWallSlideZone nws)
				{
					for (var j = 0; j < bs.Length; j++)
					{
						if (InsideRect(bs[j].pos, nws._rect))
							return true;
					}
				}
			}
		}
		return false;
	}
}
