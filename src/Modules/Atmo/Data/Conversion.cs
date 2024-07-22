using RegionKit.Modules.Atmo.Helpers;

namespace RegionKit.Modules.Atmo.Data;

public static class Conversion
{
	public static IEnumerable<Type> conversionOrder<T>()
	{
		if (typeof(T) == typeof(float))
		{
			return new List<Type>() { typeof(int), typeof(string), typeof(bool), typeof(Vector4) };
		}
		if (typeof(T) == typeof(int))
		{
			return new List<Type>() { typeof(float), typeof(string), typeof(bool), typeof(Vector4) };
		}
		if (typeof(T) == typeof(bool))
		{
			return new List<Type>() { typeof(string), typeof(float), typeof(int), typeof(Vector4) };
		}
		if (typeof(T) == typeof(Vector4))
		{
			return new List<Type>() { typeof(string), typeof(float), typeof(int), typeof(bool) };
		}
		if (typeof(T) == typeof(string))
		{
			return new List<Type>() { typeof(Vector4), typeof(float), typeof(int), typeof(bool) };
		}

		return new List<Type>();
	}

	public static T ConvertFrom<T, U>(U value, out T result)
	{
		result = value switch
		{
			bool b => CoerceBoolToType<T>(b),
			int i => CoerceIntToType<T>(i),
			float f => CoerceFloatToType<T>(f),
			string s => CoerceStringToType<T>(s),
			Vector4 v => CoerceVecToType<T>(v),
			_ => default!
		};
		return result;
	}

	public static bool TryBoolFromString(string value, out bool result)
	{
		if (bool.TryParse(value, out var b))
		{ result = b; return true; }

		if (int.TryParse(value, out var i))
		{ result = i != 0; return true; }

		result = default;
		return false;
	}

	public static bool TryFloatFromString(string value, out float result)
	{
		if (float.TryParse(value, out var f))
		{ result = f; return true; }

		result = default;
		return false;
	}

	public static bool TryIntFromString(string value, out int result)
	{
		if (int.TryParse(value, out var i))
		{ result = i; return true; }

		result = default;
		return false;
	}

	public static bool TryVecFromString(string value, out Vector4 result)
	{
		if (RealUtils.TryParseVec4(value, out var i))
		{ result = i; return true; }

		if (float.TryParse(value, out var f))
		{ result = new(f, 0f, 0f, 0f); return true; }

		result = default;
		return false;
	}
	public static bool TryStringFromString(string value, out string result)
	{
		result = value; return true;
	}

	internal static T CoerceIntToType<T>(int i)
	{
		T result = default!;
		object r = result switch
		{
			bool => i != 0,
			int => i,
			float => (float)i,
			string => i.ToString(),
			Vector4 => new Vector4(i, 0f, 0f, 0f),
			_ => i
		};
		if (r is T t) { return t; }
		return result;
	}
	internal static T CoerceFloatToType<T>(float f)
	{
		T result = default!;
		object r = result switch
		{
			bool => f != 0f,
			int => (int)f,
			float => f,
			string => f.ToString(),
			Vector4 => new Vector4(f, 0f, 0f, 0f),
			_ => f
		};
		if (r is T t) { return t; }
		return result;
	}
	internal static T CoerceBoolToType<T>(bool b)
	{
		T result = default!;
		object r = result switch
		{
			bool => b,
			int => b ? 1 : 0,
			float => b ? 1f : 0f,
			string => b.ToString(),
			Vector4 => new Vector4(b ? 1f : 0f, 0f, 0f, 0f),
			_ => b
		};
		if (r is T t) { return t; }
		return result;
	}
	internal static T CoerceVecToType<T>(Vector4 v)
	{
		T result = default!;
		object r = result switch
		{
			bool => v.magnitude != 0f,
			int => (int)v.magnitude,
			float => (float)v.magnitude,
			string => $"{v.x};{v.y};{v.z};{v.w}",
			Vector4 => v,
			_ => v
		};
		if (r is T t) { return t; }
		return result;
	}

	internal static T CoerceStringToType<T>(string s)
	{
		T result = default!;
		object r = result switch
		{
			bool => (string s) => { TryBoolFromString(s, out bool b); return b; },
			int => (string s) => { TryIntFromString(s, out int i); return i; },
			float => (string s) => { TryFloatFromString(s, out float f); return f; },
			Vector4 => (string s) => { TryVecFromString(s, out Vector4 v); return v; },
			string => s,
			_ => default!
		};
		if (r is T t) { return t; }
		return result;
	}
}



