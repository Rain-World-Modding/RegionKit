namespace RegionKit.Modules.ShelterBehaviors;

public static class ShelterBehaviorExt
{
#pragma warning disable 1591
	public static float Abs(this float f)
	{
		return Mathf.Abs(f);
	}
	public static float Abs(this int f)
	{
		return Mathf.Abs(f);
	}
	public static float Sign(this float f)
	{
		return Mathf.Sign(f);
	}

	public static bool Contains(this IntRect rect, IntVector2 pos, bool incl = true) // Cmon joar
	{
		if (incl) return pos.x >= rect.left && pos.x <= rect.right && pos.y >= rect.bottom && pos.y <= rect.top;
		return pos.x > rect.left && pos.x < rect.right && pos.y > rect.bottom && pos.y < rect.top;
	}

	public static Vector2 ToCardinals(this Vector2 dir)
	{
		return new Vector2(Vector2.Dot(Vector2.right, dir).Abs() > 0.707 ? Vector2.Dot(Vector2.right, dir).Sign() : 0, Vector2.Dot(Vector2.up, dir).Abs() > 0.707 ? Vector2.Dot(Vector2.up, dir).Sign() : 0f);
	}
#pragma warning restore 1591
}
