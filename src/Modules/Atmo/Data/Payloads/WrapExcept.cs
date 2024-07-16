using RegionKit.Modules.Atmo.Helpers;

namespace RegionKit.Modules.Atmo.Data.Payloads;
/// <summary>
/// Wraps another <see cref="IArgPayload"/>, rerouting selected properties to something different.
/// </summary>
/// <typeparam name="T">Type of value</typeparam>
/// <typeparam name="TW">Type of wrapped item</typeparam>
public struct WrapExcept<T, TW> : IArgPayload where TW : IArgPayload 
{
	/// <summary>
	/// Wrapped object
	/// </summary>
	public readonly TW wrapped;

	public delegate T Getter();
	public delegate void Setter(T value);

	private (Getter, Setter) FakeProp { get; set; }

	private T Value
	{
		get => FakeProp.Item1.Invoke();
		set => FakeProp.Item2.Invoke(value);
	}

	public WrapExcept(TW wrap, Getter getter, Setter setter) 
	{
		wrapped = wrap;
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
		get => Value is float t ? t : wrapped.F32;
		set { if (value is T t) Value = t; }
	}
	/// <inheritdoc/>
	public int I32
	{
		get => Value is int t ? t : wrapped.I32;
		set { if (value is T t) Value = t; }
	}
	/// <inheritdoc/>
	public string Str
	{
		get => Value is string t ? t : wrapped.Str;
		set { if (value is T t) Value = t; }
	}
	/// <inheritdoc/>
	public bool Bool
	{
		get => Value is bool t ? t : wrapped.Bool;
		set { if (value is T t) Value = t; }
	}
	/// <inheritdoc/>
	public Vector4 Vec
	{
		get => Value is Vector4 t ? t : wrapped.Vec;
		set { if (value is T t) Value = t; }
	}
	/// <inheritdoc/>
	public void GetEnum<TE>(out TE? value) where TE : Enum 
	{
		if (!RealUtils.TryParseEnum(Str, out value)) 
		{
			value = (TE)Convert.ChangeType(I32, typeof(TE));
		};
	}
	/// <inheritdoc/>
	public void SetEnum<TE>(in TE value) where TE : Enum 
	{
		Str = value.ToString();
		I32 = (int)Convert.ChangeType(value, typeof(int));
	}
	/// <inheritdoc/>
	public void GetExtEnum<E>(out E? value) where E : ExtEnumBase 
	{
		if (ExtEnumBase.TryParse(typeof(T), Str, false, out ExtEnumBase res)) 
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
	/// <inheritdoc/>
	public ArgType DataType => ArgType.OTHER;
}
