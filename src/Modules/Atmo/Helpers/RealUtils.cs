using RegionKit.Modules.Atmo.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static UnityEngine.Mathf;

namespace RegionKit.Modules.Atmo.Helpers
{
	internal static class RealUtils
	{
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
			LogDebug($"{str} is vector : {vecparsed}");
			vec = vecres;
			return vecparsed;
		}
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

		internal static readonly Dictionary<char, char> __literalEscapes = new()
	{
		{ 'q', '\'' },
		{ 't', '\t' },
		{ 'n', '\n' }
	};
		internal static string ApplyEscapes(this string str)
		{
			StringBuilder res = new(str.Length);
			int slashrow = 0;
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];
				if (c == '\\')
				{
					slashrow++;
				}
				else if (slashrow > 0)
				{
					if (!__literalEscapes.TryGetValue(c, out char escaped))
					{
						res.Append('\\', slashrow);
						slashrow = 0;
						continue;
					}
					if (slashrow % 2 is 0)
					{
						res.Append('\\', slashrow / 2);
						res.Append(c);
					}
					else
					{
						res.Append('\\', Max((slashrow - 1) / 2, 0));
						res.Append(escaped);
					}
					slashrow = 0;
				}
				else
				{
					res.Append(c);
					slashrow = 0;
				}
			}
			return res.ToString();
		}
		public static float Deviate(
		this float start,
		float mDev,
		float minRes = float.NegativeInfinity,
		float maxRes = float.PositiveInfinity)
		{
			return Mathf.Clamp(Mathf.Lerp(start - mDev, start + mDev, UnityEngine.Random.value), minRes, maxRes);
		}
	}
}
