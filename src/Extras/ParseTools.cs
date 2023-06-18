namespace RegionKit.Extras;

public static class ParseTools
{
	/// <summary>
	/// Attempts to parse enum value from a string, in a non-throwing fashion.
	/// </summary>
	/// <typeparam name="T">Enum type</typeparam>
	/// <param name="str">Source string</param>
	/// <param name="result">out-result.</param>
	/// <returns>Whether parsing was successful.</returns>
	public static bool TryParseEnum<T>(string str, out T? result)
		where T : Enum
	{
		Array values = Enum.GetValues(typeof(T));
		foreach (T val in values)
		{
			if (str == val.ToString())
			{
				result = val;
				return true;
			}
		}
		result = default;
		return false;
	}
	/// <summary>
	/// Attempts to parse a vector4 from string; expected format is "x;y;z;w", z or w may be absent.
	/// </summary>
	public static bool TryParseVec4(string str, out Vector4 vec)
	{
		string[] spl;
		Vector4 vecres = default;
		bool vecparsed = false;
		if ((spl = System.Text.RegularExpressions.Regex.Split(str, "\\s*;\\s*")).Length is 2 or 3 or 4)
		{
			vecparsed = true;
			for (int i = 0; i < spl.Length; i++)
			{
				if (!float.TryParse(spl[i], out float val)) vecparsed = false;
				vecres[i] = val;
			}
		}
		vec = vecres;
		return vecparsed;
	}
}