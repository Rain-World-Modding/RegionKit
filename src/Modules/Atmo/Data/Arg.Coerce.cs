using RegionKit.Modules.Atmo.Helpers;
using static RegionKit.Modules.Atmo.Atmod;

using RegionKit.Modules.Atmo.Data.Payloads;

namespace RegionKit.Modules.Atmo.Data;

public sealed partial class Arg
{
	/// <summary>
	/// Tries to coerce a given value into other things in <see cref="IArgPayload"/> in the same way <see cref="Arg"/> does. If it's not one of the supported types, takes value's <see cref="ToString()"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="value"></param>
	/// <returns></returns>
	public static PlainReadonly Coerce<T>(in T value)
	{
		if (value == null) throw new ArgumentNullException("value");
		bool boo; string str; int i32; float f32; Vector4 vec;
		switch (value)
		{
		case string s: str = s; __Coerce_Str(in s, out i32, out f32, out boo, out vec, out _); break;
		case int i: i32 = i; __Coerce_I32(in i, out str, out f32, out boo, out vec); break;
		case float f: f32 = f; __Coerce_F32(f, out str, out i32, out boo, out vec); break;
		case bool b: boo = b; __Coerce_Bool(b, out str, out i32, out f32, out vec); break;
		case Vector4 v: vec = v; __Coerce_Vec(v, out str, out i32, out f32, out boo); break;
		default:
			VerboseLog($"Value {value} of type {typeof(T).FullName} is not a supported type; trying to use its ToString instead");
			str = value.ToString(); __Coerce_Str(str, out i32, out f32, out boo, out vec, out _);
			break;
		}
		return new(str, i32, f32, boo, vec);
	}
	/// <summary>
	/// Coerces a string value into all other values stored by a <see cref="IArgPayload"/>.
	/// </summary>
	/// <param name="s">input</param>
	/// <param name="i">output</param>
	/// <param name="f">output</param>
	/// <param name="b">output</param>
	/// <param name="v">output</param>
	/// <param name="parsedAsVec">Whether a string form of a vector was recognized</param>
	internal static void __Coerce_Str(in string s, out int i, out float f, out bool b, out Vector4 v, out bool parsedAsVec)
	{
		if (parsedAsVec = RealUtils.TryParseVec4(s, out var _v))
		{
			__Coerce_Vec(_v, out _, out i, out f, out b);
			v = _v;
		}
		else
		{
			if (trueStrings.Contains(s.ToLower()))
			{
				b = true;
				f = 1f;
				i = 1;
			}
			else if (falseStrings.Contains(s.ToLower()))
			{
				b = false;
				f = 0f;
				i = 0;
			}
			else
			{
				float.TryParse(s, out f);
				if (!int.TryParse(s, out i))
				{
					i = (int)f;
				}
				b = i != 0;
			}
			v = default;
		}
	}
	/// <summary>
	/// Coerces an int value into all other values stored by <see cref="IArgPayload"/>
	/// </summary>
	/// <param name="i">input</param>
	/// <param name="s">output</param>
	/// <param name="f">output</param>
	/// <param name="b">output</param>
	/// <param name="v">output</param>
	internal static void __Coerce_I32(in int i, out string s, out float f, out bool b, out Vector4 v)
	{
		s = i.ToString();
		f = i;
		b = i != 0;
		v = default;
	}
	/// <summary>
	/// Coerces a float value into all other values stored by a <see cref="IArgPayload"/>
	/// </summary>
	/// <param name="f">input</param>
	/// <param name="s">output</param>
	/// <param name="i">output</param>
	/// <param name="b">output</param>
	/// <param name="v">output</param>
	internal static void __Coerce_F32(in float f, out string s, out int i, out bool b, out Vector4 v)
	{
		i = (int)f;
		s = f.ToString();
		b = f != 0;
		v = default;
	}
	/// <summary>
	/// Coerces a bool value into all other values stored by a <see cref="IArgPayload"/>
	/// </summary>
	/// <param name="b">input</param>
	/// <param name="s">output</param>
	/// <param name="i">output</param>
	/// <param name="f">output</param>
	/// <param name="v">output</param>
	internal static void __Coerce_Bool(in bool b, out string s, out int i, out float f, out Vector4 v)
	{
		s = b.ToString();
		i = b ? 1 : 0;
		f = b ? 1 : 0;
		v = default;
	}
	/// <summary>
	/// Coerces a vector value into all other values 
	/// </summary>
	/// <param name="v"></param>
	/// <param name="s"></param>
	/// <param name="i"></param>
	/// <param name="f"></param>
	/// <param name="b"></param>
	internal static void __Coerce_Vec(in Vector4 v, out string s, out int i, out float f, out bool b)
	{
		f = v.magnitude;
		i = (int)f;
		b = i != 0f;
		s = $"{v.x};{v.y};{v.z};{v.w}";
	}

}
