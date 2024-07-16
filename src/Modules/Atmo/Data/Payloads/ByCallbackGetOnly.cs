using RegionKit.Modules.Atmo.Helpers;

namespace RegionKit.Modules.Atmo.Data.Payloads;

/// <summary>
/// Simple read-only callback-driven <see cref="IArgPayload"/>
/// </summary>
public struct ByCallbackGetOnly : IArgPayload
{
	/// <summary>
	/// Callback to get int value.
	/// </summary>
	public Func<int>? getI32;
	/// <summary>
	/// Callback to get float value.
	/// </summary>
	public Func<float>? getF32;
	/// <summary>
	/// Callback to get bool value.
	/// </summary>
	public Func<bool>? getBool;
	/// <summary>
	/// Callback to get string value.
	/// </summary>
	public Func<string>? getStr;
	/// <summary>
	/// Callback to get vector value.
	/// </summary>
	public Func<Vector4>? getVec;
	/// <summary>
	/// Operation is not supported.
	/// </summary>
	public string Raw
	{
		get => string.Empty;
		set { }
	}
	/// <summary>
	/// Float value of the instance. Read-only.
	/// </summary>
	public float F32
	{
		get => getF32?.Invoke() ?? 0f;
		set { }
	}
	/// <summary>
	/// Int value of the instance. Read-only.
	/// </summary>
	public int I32
	{
		get => getI32?.Invoke() ?? 0;
		set { }
	}
	/// <summary>
	/// String value of the instance. Read-only.
	/// </summary>
	public string Str
	{
		get => getStr?.Invoke() ?? string.Empty;
		set { }
	}
	/// <summary>
	/// Bool value of the instance. Read-only.
	/// </summary>
	public bool Bool
	{
		get => getBool?.Invoke() ?? false;
		set { }
	}
	/// <summary>
	/// Vector value of the instance. Read-only.
	/// </summary>
	public Vector4 Vec
	{
		get => getVec?.Invoke() ?? default;
		set { }
	}
	/// <summary>
	/// Type of the instance. Read-only.
	/// </summary>
	public ArgType DataType
		=> ArgType.OTHER;

	/// <summary>
	/// Attempts to get value of the instance as an enum
	/// </summary>
	public void GetEnum<T>(out T? value) where T : Enum
	{
		if (!RealUtils.TryParseEnum(Str, out value))
		{
			value = (T)Convert.ChangeType(I32, typeof(T));
		};
	}
	/// <summary>
	/// Attempts to get value of the instance as an ExtEnum
	/// </summary>
	public void GetExtEnum<T>(out T? value) where T : ExtEnumBase
	{
		if (ExtEnumBase.TryParse(typeof(T), Str, false, out ExtEnumBase res))
		{
			value = (T)res;
		}
		else
		{
			var ent = ExtEnumBase.GetExtEnumType(typeof(T)).GetEntry(I32);
			value = ent is null ? null : (T)ExtEnumBase.Parse(typeof(T), ent, true);
		}
	}

	/// <summary>
	/// Does not do anything.
	/// </summary>
	public void SetEnum<T>(in T value) where T : Enum
	{
		return;
	}
	/// <summary>
	/// Does not do anything.
	/// </summary>
	public void SetExtEnum<T>(in T value) where T : ExtEnumBase
	{

	}

	/// <inheritdoc/>
	public override string ToString()
	{
		return Str;
	}
}
