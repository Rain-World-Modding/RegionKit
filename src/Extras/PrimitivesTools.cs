using static UnityEngine.Mathf;

namespace RegionKit.Extras;

public static class PrimitivesTools
{
	/// <summary>
	/// Creates an <see cref="IntRect"/> from two corner points.
	/// </summary>
	/// <param name="p1"></param>
	/// <param name="p2"></param>
	/// <returns></returns>
	public static IntRect ConstructIR(IntVector2 p1, IntVector2 p2)
	{
		Vector4 vec = new Color();
		return new(Min(p1.x, p2.x), Min(p1.y, p2.y), Max(p1.x, p2.x), Max(p1.y, p2.y));
	}

	/// <summary>
	/// Creates a Color from a vector and makes sure its alpha is not zero.
	/// </summary>
	/// <param name="vec">Vector4 to check</param>
	public static Color ToOpaqueCol(in this Vector4 vec)
		=> vec.w is not 0f ? vec : new(vec.x, vec.y, vec.z, 1f);
	/// <summary>
	/// Converts Vector2 to IntVector2. If converting between world and tile coordinates, you should use <see cref="Room"/>.GetTilePosition() instead.
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public static IntVector2 ToIntVector2(this Vector2 vec) => new((int)vec.x, (int)vec.y);
	public static void ClampToNormal(ref this Color self)
	{
		self.r = Clamp01(self.r);
		self.g = Clamp01(self.g);
		self.b = Clamp01(self.b);
		self.a = Clamp01(self.a);
	}
	public static Color Deviation(this Color self, Color dev)
	{
		var res = new Color();
		for (int i = 0; i < 4; i++)
		{
			res[i] = ClampedFloatDeviation(self[i], dev[i]);
		}
		return res;
	}
	public static string[] SplitAndRemoveEmpty(this string str, string separator) => str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
}
