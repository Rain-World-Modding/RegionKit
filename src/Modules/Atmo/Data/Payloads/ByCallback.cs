using RegionKit.Modules.Atmo.Helpers;
using System.Reflection;

namespace RegionKit.Modules.Atmo.Data.Payloads;

/// <summary>
/// A callback driven payload, with support for both getters and setters. Uses <see cref="FakeProp{T}"/> to group functions together.
/// </summary>
public struct ByCallback : IArgPayload {
	/// <summary>
	/// <see cref="I32"/> property backing.
	/// </summary>
	public FakeProp<int> prop_I32;
	/// <summary>
	/// <see cref="F32"/> property backing.
	/// </summary>
	public FakeProp<float> prop_F32;
	/// <summary>
	/// <see cref="Bool"/> property backing.
	/// </summary>
	public FakeProp<bool> prop_Bool;
	/// <summary>
	/// <see cref="Str"/> property backing.
	/// </summary>
	public FakeProp<string> prop_Str;
	/// <summary>
	/// <see cref="Vec"/> property backing
	/// </summary>
	public FakeProp<Vector4> prop_Vec;
	/// <summary>
	/// Creates a new instance with given prop backings.
	/// </summary>
	public ByCallback(
		FakeProp<int>? prop_I32 = null,
		FakeProp<float>? prop_F32 = null,
		FakeProp<bool>? prop_Bool = null,
		FakeProp<string>? prop_Str = null,
		FakeProp<Vector4>? prop_Vec = null) {
		this.prop_I32 = prop_I32 ?? new(null, null);
		this.prop_F32 = prop_F32 ?? new(null, null);
		this.prop_Bool = prop_Bool ?? new(null, null);
		this.prop_Str = prop_Str ?? new(null, null);
		this.prop_Vec = prop_Vec ?? new(null, null);
	}
	/// <summary>
	/// Wraps a callback-payload around another arbitrary <see cref="IArgPayload"/>
	/// </summary>
	/// <param name="wrap"></param>
	/// <exception cref="InvalidOperationException"></exception>
	public ByCallback(IArgPayload wrap) {
		prop_I32 = null!;
		prop_F32 = null!;
		prop_Bool = null!;
		prop_Str = null!;
		prop_Vec = null!;
		Type t = wrap.GetType();
		PropertyInfo? prop;
		string[] req = new[] { "F32", "I32", "Bool", "Str", "Vec" };
		foreach (string propname in req) {
			if ((prop = t.GetPropertyAllContexts(propname)) is null) throw new InvalidOperationException("Property missing!");
			switch (propname) {
			case "F32": {
				prop_F32 = new(prop, wrap);
				break;
			}
			case "I32": {
				prop_I32 = new(prop, wrap);
				break;
			}
			case "Str": {
				prop_Str = new(prop, wrap);
				break;
			}
			case "Bool": {
				prop_Bool = new(prop, wrap);
				break;
			}
			case "Vec": {
				prop_Vec = new(prop, wrap);
				break;
			}
			default:
				break;
			} //bind fakeprops to realprops somewhere here
		}
		prop_I32 ??= new(null, null);
		prop_F32 ??= new(null, null);
		prop_Bool ??= new(null, null);
		prop_Str ??= new(null, null);
		prop_Vec ??= new(null, null);
	}

	/// <inheritdoc/>
	public string Raw {
		get => string.Empty;
		set { }
	}
	/// <inheritdoc/>
	public float F32 {
		get => prop_F32.a?.Invoke() ?? 0f;
		set => prop_F32.b?.Invoke(value);
	}
	/// <inheritdoc/>
	public int I32 {
		get => prop_I32.a?.Invoke() ?? 0;
		set => prop_I32.b?.Invoke(value);
	}
	/// <inheritdoc/>
	public string Str {
		get => prop_Str.a?.Invoke() ?? string.Empty;
		set => prop_Str.b?.Invoke(value);
	}
	/// <inheritdoc/>
	public bool Bool {
		get => prop_Bool.a?.Invoke() ?? false;
		set => prop_Bool.b?.Invoke(value);
	}
	/// <inheritdoc/>
	public ArgType DataType => ArgType.OTHER;
	/// <inheritdoc/>
	public Vector4 Vec {
		get => prop_Vec.a?.Invoke() ?? default;
		set => prop_Vec.b?.Invoke(value);
	}
	/// <inheritdoc/>
	public void GetEnum<T>(out T? value) where T : Enum {
		if (!RealUtils.TryParseEnum(Str, out value)) {
			value = (T)Convert.ChangeType(I32, typeof(T));
		};
	}
	/// <inheritdoc/>
	public void SetEnum<T>(in T value) where T : Enum {
		Str = value.ToString();
		I32 = (int)Convert.ChangeType(value, typeof(int));
	}
	/// <inheritdoc/>
	public void GetExtEnum<T>(out T? value) where T : ExtEnumBase {
		if (ExtEnumBase.TryParse(typeof(T), Str, false, out ExtEnumBase res)) {
			value = (T)res;
		}
		else {
			var ent = ExtEnumBase.GetExtEnumType(typeof(T)).GetEntry(I32);
			value = ent is null ? null : (T)ExtEnumBase.Parse(typeof(T), ent, true);
		}
	}
	/// <inheritdoc/>
	public void SetExtEnum<T>(in T value) where T : ExtEnumBase {
		I32 = value?.Index ?? -1;
		Str = value?.ToString() ?? "";
	}
}
