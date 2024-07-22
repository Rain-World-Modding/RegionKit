namespace RegionKit.Modules.Atmo.Data;

public abstract class Arg
{
	public Arg(string? raw)
	{
		_raw = raw;
	}


	private string? _raw;

	public string? Name;
	public virtual string? Raw => _raw;

	public abstract string String { get; }
	public abstract bool Bool { get; }
	public abstract int Int { get; }
	public abstract float Float { get; }
	public abstract Vector4 Vector { get; }



	public int SecAsFrames => (int)(Float * 40f);
	public T? GetExtEnum<T>() where T : ExtEnumBase
	{
		if (ExtEnumBase.TryParse(typeof(T), String, false, out ExtEnumBase res) && res is T t)
		{ return t; }

		else
		{
			var ent = ExtEnumBase.GetExtEnumType(typeof(T)).GetEntry(Int);
			return ent is null ? default! : (T)ExtEnumBase.Parse(typeof(T), ent, true);
		}
	}

	public T? GetEnum<T>() where T : Enum
	{
		Array values = Enum.GetValues(typeof(T));
		foreach (T val in values)
		{
			if (String == val.ToString())
			{
				return val;
			}
		}

		return (T)Convert.ChangeType(String, Enum.GetUnderlyingType(typeof(T)));
	}


	public static explicit operator string(Arg arg) => arg.String;
	public static implicit operator Arg(string s) => new StaticArg(s);

	public static explicit operator bool(Arg arg) => arg.Bool;
	public static implicit operator Arg(bool s) => new StaticArg(s.ToString());

	public static explicit operator int(Arg arg) => arg.Int;
	public static implicit operator Arg(int s) => new StaticArg(s.ToString());

	public static explicit operator float(Arg arg) => arg.Float;
	public static implicit operator Arg(float s) => new StaticArg(s.ToString());

	public static explicit operator Vector4(Arg arg) => arg.Vector;
	public static implicit operator Arg(Vector4 s) => new StaticArg(s.ToString());

}

public class StaticArg : Arg
{
	readonly string _string = "";
	readonly bool _bool = false;
	readonly int _int = 0;
	readonly float _float = 0f;
	readonly Vector4 _vector = new();

	public override string String => _string;
	public override bool Bool => _bool;
	public override int Int => _int;
	public override float Float => _float;
	public override Vector4 Vector => _vector;

	public StaticArg(string raw) : base(raw)
	{
		_string = raw;

		if (Conversion.TryFloatFromString(raw, out float f)) { _float = f; }
		if (Conversion.TryIntFromString(raw, out int i)) { _int = i; }
		if (Conversion.TryBoolFromString(raw, out bool b)) { _bool = b; }
		if (Conversion.TryVecFromString(raw, out Vector4 v)) { _vector = v; }
	}
}

public class CallbackArg : Arg, IEnumerable
{
	public CallbackArg() : base(null)
	{
	}

	public delegate T Getter<T>();

	private Getter<string>? _string = null;
	private Getter<bool>? _bool = null;
	private Getter<int>? _int = null;
	private Getter<float>? _float = null;
	private Getter<Vector4>? _vector = null;

	public override string? Raw => String;
	public override string String => _string?.Invoke() ?? GetCoerced<string>() ?? "";
	public override bool Bool => _bool?.Invoke() ?? GetCoerced<bool>();
	public override int Int => _int?.Invoke() ?? GetCoerced<int>();
	public override float Float => _float?.Invoke() ?? GetCoerced<float>();
	public override Vector4 Vector => _vector?.Invoke() ?? GetCoerced<Vector4>();

	private T GetCoerced<T>()
	{
		T value = default!;
		foreach (Type type in Conversion.conversionOrder<T>())
		{
			if (type == typeof(string) && _string != null)
			{ Conversion.ConvertFrom(_string(), out value); break; }

			if (type == typeof(bool) && _bool != null)
			{ Conversion.ConvertFrom(_bool(), out value); break; }

			if (type == typeof(int) && _int != null)
			{ Conversion.ConvertFrom(_int(), out value); break; }

			if (type == typeof(float) && _float != null)
			{ Conversion.ConvertFrom(_float(), out value); break; }

			if (type == typeof(Vector4) && _vector != null)
			{ Conversion.ConvertFrom(_vector(), out value); break; }
		}

		return value;
	}

	public void Add(Getter<string> getter) => _string = getter;
	public void Add(Getter<bool> getter) => _bool = getter;
	public void Add(Getter<int> getter) => _int = getter;
	public void Add(Getter<float> getter) => _float = getter;
	public void Add(Getter<Vector4> getter) => _vector = getter;

	public IEnumerator GetEnumerator()
	{
		throw new NotImplementedException();
	}
}

public class RWCallbackArg : Arg, IEnumerable
{
	public World world;
	public RWCallbackArg(World world) : base(null)
	{
		this.world = world;
	}

	public delegate T Getter<T>(World world);

	private Getter<string>? _string = null;
	private Getter<bool>? _bool = null;
	private Getter<int>? _int = null;
	private Getter<float>? _float = null;
	private Getter<Vector4>? _vector = null;

	public override string? Raw => String;
	public override string String => _string?.Invoke(world) ?? GetCoerced<string>() ?? "";
	public override bool Bool => _bool?.Invoke(world) ?? GetCoerced<bool>();
	public override int Int => _int?.Invoke(world) ?? GetCoerced<int>();
	public override float Float => _float?.Invoke(world) ?? GetCoerced<float>();
	public override Vector4 Vector => _vector?.Invoke(world) ?? GetCoerced<Vector4>();

	private T GetCoerced<T>()
	{
		T value = default!;
		foreach (Type type in Conversion.conversionOrder<T>())
		{
			if (type == typeof(string) && _string != null)
			{ Conversion.ConvertFrom(_string(world), out value); break; }

			if (type == typeof(bool) && _bool != null)
			{ Conversion.ConvertFrom(_bool(world), out value); break; }

			if (type == typeof(int) && _int != null)
			{ Conversion.ConvertFrom(_int(world), out value); break; }

			if (type == typeof(float) && _float != null)
			{ Conversion.ConvertFrom(_float(world), out value); break; }

			if (type == typeof(Vector4) && _vector != null)
			{ Conversion.ConvertFrom(_vector(world), out value); break; }
		}

		return value;
	}

	public void Add(Getter<string> getter) => _string = getter;
	public void Add(Getter<bool> getter) => _bool = getter;
	public void Add(Getter<int> getter) => _int = getter;
	public void Add(Getter<float> getter) => _float = getter;
	public void Add(Getter<Vector4> getter) => _vector = getter;

	public IEnumerator GetEnumerator()
	{
		throw new NotImplementedException();
	}
}
