using static UnityEngine.Mathf;

namespace RegionKit.Extras;

public static class RngTools {
	/// <summary>
	/// Returns a random deviation from start position, up to mDev in both directions. Clamps to given bounds if provided.
	/// </summary>
	/// <param name="start">Center of the spread.</param>
	/// <param name="mDev">Maximum deviation.</param>
	/// <param name="minRes">Result lower bound.</param>
	/// <param name="maxRes">Result upper bound.</param>
	/// <returns>The resulting value.</returns>
	public static int ClampedIntDeviation(
		int start,
		int mDev,
		int minRes = int.MinValue,
		int maxRes = int.MaxValue)
	{
		return IntClamp(UnityEngine.Random.Range(start - mDev, start + mDev), minRes, maxRes);
	}

	/// <summary>
	/// Returns a random deviation from start position, up to mDev in both directions. Clamps to given bounds if provided.
	/// </summary>
	/// <param name="start">Center of the spread.</param>
	/// <param name="mDev">Maximum deviation.</param>
	/// <param name="minRes">Result lower bound.</param>
	/// <param name="maxRes">Result upper bound.</param>
	/// <returns>The resulting value.</returns>
	public static float ClampedFloatDeviation(
		float start,
		float mDev,
		float minRes = float.NegativeInfinity,
		float maxRes = float.PositiveInfinity)
	{
		return Clamp(Lerp(start - mDev, start + mDev, UnityEngine.Random.value), minRes, maxRes);
	}

	/// <summary>
	/// Gives you a random sign.
	/// </summary>
	/// <returns>1f or -1f on a coinflip.</returns>
	public static float RandSign()
	{
		return UnityEngine.Random.value > 0.5f ? -1f : 1f;
	}

	/// <summary>
	/// Performs a random lerp between two 2d points.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	/// <returns>Resulting vector.</returns>
	public static Vector2 V2RandLerp(Vector2 a, Vector2 b)
	{
		return Vector2.Lerp(a, b, UnityEngine.Random.value);
	}

	/// <summary>
	/// Clamps a color to acceptable values.
	/// </summary>
	/// <param name="bcol"></param>
	/// <returns></returns>
	public static Color Clamped(this ref Color bcol)
	{
		return new(Clamp01(bcol.r), Clamp01(bcol.g), Clamp01(bcol.b));
	}
	/// <summary>
	/// Performs a channelwise random deviation on a color.
	/// </summary>
	/// <param name="bcol">base</param>
	/// <param name="dbound">deviations</param>
	/// <param name="clamped">whether to clamp the result to reasonable values</param>
	/// <returns>resulting colour</returns>
	public static Color RandDev(this Color bcol, Color dbound, bool clamped = true)
	{
		Color res = default;
		for (int i = 0; i < 3; i++) res[i] = bcol[i] + (dbound[i] * UnityEngine.Random.Range(-1f, 1f));
		return clamped ? res.Clamped() : res;
	}
	internal static T? RandomOrDefault<T>(this T[] arr)
		where T : notnull
	{
		var res = default(T);
		if (arr.Length > 0) return arr[UnityEngine.Random.Range(0, arr.Length)];
		return res;
	}
	public static T? RandomOrDefault<T>(this List<T> l)
	{
		if (l.Count == 0) return default;
		//var R = new System.Random(l.GetHashCode());
		return l[UnityEngine.Random.Range(0, l.Count)];
	}
}