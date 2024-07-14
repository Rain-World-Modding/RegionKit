namespace RegionKit.Modules.Atmo.Helpers;
/// <summary>
/// Replaces ValueTuple with some semblance of named field functionality. Names are used when comparing!
/// </summary>
/// <typeparam name="T1">Type of left item</typeparam>
/// <typeparam name="T2">Type of right item</typeparam>
/// <param name="a">Left item</param>
/// <param name="b">Right item</param>
/// <param name="name">Name of the instance</param>
/// <param name="nameA">Name of the left item</param>
/// <param name="nameB">Name of the right item</param>
public record VT<T1, T2>(
	T1 a,
	T2 b,
	string name,
	string nameA,
	string nameB) {
	/// <summary>
	/// Creates a new instance, using default names.
	/// </summary>
	/// <param name="_a">Left item</param>
	/// <param name="_b">Right item</param>
	public VT(T1 _a, T2 _b) : this(_a, _b, defName ?? "VT", defAName ?? "a", defBName ?? "b") {
	}
	/// <inheritdoc/>
	public override string ToString() {
		return $"{name} {{ {nameA} = {a}, {nameB} = {b} }}";
	}

	/// <summary>
	/// Default name for instances. "VT" if null
	/// </summary>
	public static string? defName { get; private set; }
	/// <summary>
	/// Default name for left items. "a" if null
	/// </summary>
	public static string? defAName { get; private set; }
	/// <summary>
	/// Default name for right items. "b" if null
	/// </summary>
	public static string? defBName { get; private set; }
	/// <summary>
	/// Compares two instances.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public virtual bool Equals(VT<T1, T2> other) {
		return name == other.name
				&& Equals(a, other.a)
				&& Equals(b, other.b);
	}

	/// <inheritdoc/>
	public override int GetHashCode() {
		return (a?.GetHashCode() ?? 0) ^ (b?.GetHashCode() ?? 0);
	}

	/// <summary>
	/// Use this as a shorthand for creating several instances with similar names. Not thread safe (but that's okay because basically nothing in RW is)
	/// </summary>
	public struct Names : IDisposable {
		/// <summary>
		/// Sets name defaults to specified values.
		/// </summary>
		/// <param name="defname">default instance name</param>
		/// <param name="defaname">default left item name</param>
		/// <param name="defbname">default right item name</param>
		public Names(string defname, string defaname, string defbname) {
			defName = defname;
			defAName = defaname;
			defBName = defbname;
		}
		/// <summary>
		/// Resets the static default names.
		/// </summary>
		public void Dispose() {
			defName = null;
			defAName = null;
			defBName = null;
		}
	}
}
