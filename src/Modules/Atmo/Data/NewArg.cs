using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RegionKit.Modules.Atmo.Data;

public class NewArg : IEnumerable
{
	public NewArg()
	{
		Raw = "";
	}

	public NewArg(string? name, string raw)
	{
		_Name = name;
		Raw = raw;
		Add(raw);
		SetAllConsts(raw);

		CONSTANT = true;
	}

	private bool CONSTANT = false;

	private string? _raw;

	private string? _Name;

	public string? Name
	{
		get => _Name;
		set => _Name = value;
	}

	public string Raw
	{
		get
		{
			if (_raw != null) return _raw;
			if (dict.Count > 0)
			{
				if (dict.TryGetValue(typeof(string), out var v) && v.Value is string s)
					return s;
				var obj = dict.ElementAt(0).Value.Value;
				if (obj != null)
				{
					return obj.ToString();
				}
			}
			return "";
		}
		set
		{
			_raw = value;
		}
	}
	public int SecAsFrames => (int)(GetValue<float>() * 40f);


	Dictionary<Type, IFakeProperty> dict = new();

	public T GetValue<T>()
	{
		TryGetValue(out T v); return v;
	}

	public bool TryGetValue<T>(out T result, bool coerced = true)
	{
		if (dict.TryGetValue(typeof(T), out var prop) && prop.Value is T t) 
		{
			result = t;
			return true;
		}

		if (coerced && TryGetCoerced<T>(out var r))
		{
			result = r;
			if (CONSTANT) Add(r); //store it for later if the variable will never change
			return true;
		}

		result = default!;
		return false;
	}

	#region coersion
	private void SetAllConsts(string value)
	{
		if (!dict.ContainsKey(typeof(string)))
		{ this.Add(value); }

		if (!dict.ContainsKey(typeof(float)) && Conversion.TryFloatFromString(value, out float f))
		{ this.Add(f); }

		if (!dict.ContainsKey(typeof(int)) && Conversion.TryIntFromString(value, out int i))
		{ this.Add(i); }

		if (!dict.ContainsKey(typeof(bool)) && Conversion.TryBoolFromString(value, out bool b))
		{ this.Add(b); }

		if (!dict.ContainsKey(typeof(Vector4)) && Conversion.TryVecFromString(value, out Vector4 v))
		{ this.Add(v); }
	}

	private bool TryGetCoerced<T>(out T value)
	{
		if (typeof(T).IsEnum)
		{
			typeof(T).GetEnumUnderlyingType();
			GetEnum(out value!);
			return true;
		}

		if (typeof(T).IsExtEnum())
		{
			GetExtEnum(out value!);
			return true;
		}

		foreach (Type type in Conversion.conversionOrder<T>())
		{
			if (type == typeof(bool) && TryGetValue(out bool b, false))
			{ Conversion.ConvertFrom(b, out value); return true; }

			if (type == typeof(int) && TryGetValue(out int i, false))
			{ Conversion.ConvertFrom(i, out value); return true; }

			if (type == typeof(float) && TryGetValue(out float f, false))
			{ Conversion.ConvertFrom(f, out value); return true; }

			if (type == typeof(string) && TryGetValue(out string s, false))
			{ Conversion.ConvertFrom(s, out value); return true; }

			if (type == typeof(Vector4) && TryGetValue(out Vector4 v, false))
			{ Conversion.ConvertFrom(v, out value); return true; }
		}

		value = default!;
		return false;
	}

	private void GetExtEnum<T>(out T? value)
	{
		value = default!;
		if (!typeof(T).IsExtEnum()) return; 

		if (ExtEnumBase.TryParse(typeof(T), GetValue<string>(), false, out ExtEnumBase res) && res is T t)
		{ value = t; }

		else
		{
			var ent = ExtEnumBase.GetExtEnumType(typeof(T)).GetEntry(GetValue<int>());
			value = ent is null ? default! : (T)ExtEnumBase.Parse(typeof(T), ent, true);
		}
	}

	private void GetEnum<T>(out T? value)
	{
		value = default!;
		if (!typeof(T).IsEnum) return;

		Array values = Enum.GetValues(typeof(T));
		foreach (T val in values)
		{
			if (GetValue<string>() == val.ToString())
			{
				value = val;
				return;
			}
		}

		value = (T)Convert.ChangeType(GetValue<int>(), Enum.GetUnderlyingType(typeof(T)));
	}
	#endregion

	#region syntax magic
	public IEnumerator GetEnumerator() => dict.GetEnumerator();

	public void Add<T>(T value) => Add(new StaticValue<T>(value));
	public void Add<T>(Callback<T>.Getter? getter = null, Callback<T>.Setter? setter = null) => Add(new Callback<T>(getter, setter));
	public void Add<T>(Callback<T> fakeProp) => Add(typeof(T), fakeProp);
	public void Add<T>(BackingField<T> backingField) => Add(typeof(T), backingField);
	public void Add<T>(StaticValue<T> staticValue)
	{
		if (dict.Count == 0) CONSTANT = true;
		Add(typeof(T), staticValue);
	}
	public void Add(Type t, IFakeProperty prop) => dict[t] = prop;

	public static explicit operator string(NewArg arg) => arg.GetValue<string>();
	public static implicit operator NewArg(string s) => new() { s };

	public static explicit operator int(NewArg arg) => arg.GetValue<int>();
	public static implicit operator NewArg(int s) => new() { s };

	public static explicit operator float(NewArg arg) => arg.GetValue<float>();
	public static implicit operator NewArg(float s) => new() { s };

	public static explicit operator bool(NewArg arg) => arg.GetValue<bool>();
	public static implicit operator NewArg(bool s) => new() { s };

	public static explicit operator Vector4(NewArg arg) => arg.GetValue<Vector4>();
	public static implicit operator NewArg(Vector4 s) => new() { s };

	#endregion
}

#region FakeProperties
public interface IFakeProperty { abstract object? Value { get; set; } }

public struct Callback<T> : IFakeProperty
{
	public Callback(Getter? getter = null, Setter? setter = null)
	{
		FakeProp = (getter, setter);
	}

	public delegate T Getter();
	public delegate void Setter(T value);

	private (Getter?, Setter?) FakeProp { get; set; }

	private T GenericValue
	{
		get => FakeProp.Item1 is not null? FakeProp.Item1.Invoke() : default!;
		set => FakeProp.Item2?.Invoke(value);
	}
	object? IFakeProperty.Value
	{ 
		get => GenericValue; 
		set { if (value is T t) GenericValue = t; } 
	}

}
public struct RWCallback<T> : IFakeProperty
{
	public RWCallback(World world, Getter? getter = null, Setter? setter = null)
	{
		FakeProp = (getter, setter);
		this.world = world;
	}

	public delegate T Getter(World world);
	public delegate void Setter(World world, T value);

	private (Getter?, Setter?) FakeProp { get; set; }

	private T GenericValue
	{
		get => FakeProp.Item1 is not null ? FakeProp.Item1.Invoke(world) : default!;
		set => FakeProp.Item2?.Invoke(world, value);
	}
	object? IFakeProperty.Value
	{
		get => GenericValue;
		set { if (value is T t) GenericValue = t; }
	}

	private World world;
}

public struct BackingField<T> : IFakeProperty
{
	public BackingField(T genericValue)
	{ GenericValue = genericValue; }

	private T GenericValue;

	object? IFakeProperty.Value
	{
		get => GenericValue;
		set { if (value is T t) GenericValue = t; }
	}
}
public struct StaticValue<T> : IFakeProperty
{
	public StaticValue(T genericValue)
	{ GenericValue = genericValue; }

	private readonly T GenericValue;

	object? IFakeProperty.Value
	{
		get => GenericValue;
		set { }
	}
}
#endregion
