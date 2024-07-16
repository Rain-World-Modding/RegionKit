using RegionKit.Modules.Atmo.Helpers;

namespace RegionKit.Modules.Atmo.Data.Payloads;

/// <summary>
/// A callback driven payload, with support for both getters and setters.
/// </summary>
public struct ByCallback<T> : IArgPayload
{
	public delegate T Getter();
	public delegate void Setter(T value);

	private (Getter, Setter) FakeProp { get; set; }

	private T Value
	{
		get => FakeProp.Item1.Invoke();
		set => FakeProp.Item2.Invoke(value);
	}

	public ByCallback(Getter getter, Setter setter) 
	{
		FakeProp = (getter, setter);
	}
	/// <summary>
	/// Wraps a callback-payload around another arbitrary <see cref="IArgPayload"/>
	/// *Please don't actually use this constructor*
	/// </summary>
	/// <param name="wrap"></param>
	public ByCallback(IArgPayload wrap)
	{
		T getter()
		{
			if (wrap.F32 is T f) return f;
			if (wrap.I32 is T i) return i;
			if (wrap.Str is T s) return s;
			if (wrap.Bool is T b) return b;
			if (wrap.Vec is T v) return v;
			return default!;
		}

		void setter(T value)
		{
			if (value is float f) wrap.F32 = f;
			if (value is int i) wrap.I32 = i;
			if (value is string s) wrap.Str = s;
			if (value is bool b) wrap.Bool = b;
			if (value is Vector4 v) wrap.Vec = v;
		}

		FakeProp = (getter, setter);
	}

	/// <inheritdoc/>
	public string Raw 
	{
		get => string.Empty;
		set { }
	}
	/// <inheritdoc/>
	public float F32
	{
		get => Value is float t ? t : 0f;
		set { if (value is T t) Value = t; }
	}
	/// <inheritdoc/>
	public int I32 
	{
		get => Value is int t ? t : 0;
		set { if (value is T t) Value = t; }
	}
	/// <inheritdoc/>
	public string Str 
	{
		get => Value is string t ? t : string.Empty;
		set { if (value is T t)	Value = t; }
	}
	/// <inheritdoc/>
	public bool Bool 
	{
		get => Value is bool t ? t : false;
		set { if (value is T t) Value = t; }
	}
	/// <inheritdoc/>
	public ArgType DataType => ArgType.OTHER;
	/// <inheritdoc/>
	public Vector4 Vec 
	{
		get => Value is Vector4 t ? t : default;
		set { if (value is T t) Value = t; }
	}
	/// <inheritdoc/>
	public void GetEnum<E>(out E? value) where E : Enum 
	{
		if (!RealUtils.TryParseEnum(Str, out value)) 
		{
			value = (E)Convert.ChangeType(I32, typeof(E));
		};
	}
	/// <inheritdoc/>
	public void SetEnum<E>(in E value) where E : Enum 
	{
		Str = value.ToString();
		I32 = (int)Convert.ChangeType(value, typeof(int));
	}
	/// <inheritdoc/>
	public void GetExtEnum<E>(out E? value) where E : ExtEnumBase 
	{
		if (ExtEnumBase.TryParse(typeof(E), Str, false, out ExtEnumBase res)) 
		{
			value = (E)res;
		}
		else 
		{
			var ent = ExtEnumBase.GetExtEnumType(typeof(E)).GetEntry(I32);
			value = ent is null ? null : (E)ExtEnumBase.Parse(typeof(E), ent, true);
		}
	}
	/// <inheritdoc/>
	public void SetExtEnum<E>(in E value) where E : ExtEnumBase 
	{
		I32 = value?.Index ?? -1;
		Str = value?.ToString() ?? "";
	}
}
