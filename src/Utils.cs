using System.Reflection;
using System.Text;
using static UnityEngine.Mathf;

namespace RegionKit;
/// <summary>
/// contains general purpose utility methods
/// </summary>
internal static partial class Utils
{
	#region fields
	// /// <summary>
	// /// Unsafe string allocator
	// /// </summary>
	// public static readonly Func<int, string> stralloc =
	// 	GetFn<Func<int, string>>(null, methodof<string>("InternalAllocateStr", BF_ALL_CONTEXTS_STATIC)!)!;
	#endregion
	#region collections
	/// <summary>
	/// Attempts getting an item at specified index; if none found, uses default value.
	/// </summary>
	/// <typeparam name="T">List item type</typeparam>
	/// <param name="arr">List in question</param>
	/// <param name="index">Target index</param>
	/// <param name="def">Default value</param>
	/// <returns></returns>
	public static T AtOr<T>(this IList<T> arr, int index, T def)
	{
		BangBang(arr, nameof(arr));
		if (index >= arr.Count || index < 0) return def;
		return arr[index];
	}
	/// <summary>
	/// Attempts to get a char at a specified position.
	/// </summary>
	public static char? Get(this string str, int index)
	{
		BangBang(str, nameof(str));
		if (index >= str.Length || index < 0) return null;
		return str[index];
	}
	/// <summary>
	/// Attempts to get a char at a specified position.
	/// </summary>
	public static char? Get(this StringBuilder sb, int index)
	{
		BangBang(sb, nameof(sb));
		if (index >= sb.Length || index < 0) return null;
		return sb[index];
	}
	/// <summary>
	/// Tries getting a dictionary item by specified key; if none found, adds one using specified callback and returns the new item.
	/// </summary>
	/// <typeparam name="Tkey">Dictionary keys</typeparam>
	/// <typeparam name="Tval">Dictionary values</typeparam>
	/// <param name="dict">Dictionary to get items from</param>
	/// <param name="key">Key to look up</param>
	/// <param name="defval">Default value callback. Executed if item is not found; its return is added to the dictionary, then returned from the extension method.</param>
	/// <returns>Resulting item.</returns>
	public static Tval EnsureAndGet<Tkey, Tval>(
		this IDictionary<Tkey, Tval> dict,
		Tkey key,
		Func<Tval> defval)
	{
		BangBang(dict, nameof(dict));
		if (key is not ValueType) BangBang(key, nameof(key));
		if (dict.TryGetValue(key, out Tval oldVal)) { return oldVal; }
		else
		{
			Tval def = defval();
			dict.Add(key, def);
			return def;
		}
	}
	/// <summary>
	/// Shifts contents of a BitArray one position to the right.
	/// </summary>
	/// <param name="arr">Array in question</param>
	public static void RightShift(this System.Collections.BitArray arr)
	{
		for (int i = arr.Count - 2; i >= 0; i--)
		{
			arr[i + 1] = arr[i];//arr.Set(i + 1, arr.Get(i));//[i + 1] = arr[i];
		}
		arr[0] = false;
	}
	/// <summary>
	/// For a specified key, checks if a value is present. If yes, updates the value, otherwise adds the value.
	/// </summary>
	/// <typeparam name="Tk">Keys type</typeparam>
	/// <typeparam name="Tv">Values type</typeparam>
	/// <param name="dict">Dictionary in question</param>
	/// <param name="key">Key to look up</param>
	/// <param name="val">Value to set</param>
	public static void Set<Tk, Tv>(this Dictionary<Tk, Tv> dict, Tk key, Tv val)
	{
		if (dict.ContainsKey(key)) dict[key] = val;
		else dict.Add(key, val);
	}
	internal static bool SetKey<tKey, tValue>(this IDictionary<tKey, tValue> dict, tKey key, tValue val)
	{
		if (dict == null) throw new ArgumentNullException();
		try
		{
			if (!dict.ContainsKey(key)) dict.Add(key, val);
			else dict[key] = val;
			return true;
		}
		catch
		{
			return false;
		}

	}
	internal static bool IndexInRange(this object[] arr, int index) => index > -1 && index < arr.Length;
	internal static T? RandomOrDefault<T>(this T[] arr)
		where T : notnull
	{
		var res = default(T);
		if (arr.Length > 0) return arr[RNG.Range(0, arr.Length)];
		return res;
	}
	public static T? RandomOrDefault<T>(this List<T> l)
	{
		if (l.Count == 0) return default;
		//var R = new System.Random(l.GetHashCode());
		return l[RNG.Range(0, l.Count)];
	}

	public static IEnumerable<int> Indices(this IList list)
	{
		int ct = list.Count;
		for (int i = 0; i < ct; i++)
		{
			yield return i;
			//if (ct != list.Count) throw new InvalidOperationException("List was modified");
		}
	}

	/// <summary>
	/// Creates a looped version of a selected enumerator.
	/// </summary>
	/// <param name="collection">Subject enumerator</param>
	/// <typeparam name="T">Type of item</typeparam>
	/// <returns>A yielder that wraps a collection and returns all its elements, repeating endlessly</returns>
	public static IEnumerable<T> Loop<T>(this IEnumerable<T> collection)
	{
		IEnumerator<T> en;
	START_:;
		en = collection.GetEnumerator();
		while (en.MoveNext()) yield return en.Current;
		goto START_;
	}
	public static void AddMultiple<TKey, TValue>(this Dictionary<TKey, TValue> dict, TValue value, params TKey[] keys)
	{
		dict.AddMultiple(value, ieKeys: keys);
	}

	public static void AddMultiple<TKey, TValue>(this Dictionary<TKey, TValue> dict, TValue value, IEnumerable<TKey> ieKeys)
	{
		foreach (TKey key in ieKeys)
		{
			dict.SetKey(key, value);
		}
	}
	#endregion collections
	#region weakdicts
	//moved from M4rbleL1ne's ConditionalEffects
	public static void RemoveWeak<T>(Dictionary<WeakReference, T> dict, RoomSettings.RoomEffect key)
	{
		WeakReference? weakKey = null;
		foreach (var pair in dict)
			if (pair.Key.IsAlive && pair.Key.Target == key)
			{
				weakKey = pair.Key;
				break;
			}
		if (weakKey != null)
			dict.Remove(weakKey);
	}
	public static bool TryGetWeak<T>(Dictionary<WeakReference, T> dict, RoomSettings.RoomEffect key, out T? value)
	{
		foreach (var pair in dict)
			if (pair.Key.IsAlive && pair.Key.Target == key)
			{
				value = pair.Value;
				return true;
			}
		value = default;
		return false;
	}
	public static T GetWeak<T>(Dictionary<WeakReference, T> dict, RoomSettings.RoomEffect key)
	{
		foreach (var pair in dict)
			if (pair.Key.IsAlive && pair.Key.Target == key)
				return pair.Value;
		throw new KeyNotFoundException("Could not get from weak list");
	}
	public static void SetWeak<T>(Dictionary<WeakReference, T> dict, RoomSettings.RoomEffect key, T value)
	{
		foreach (var pair in dict)
			if (pair.Key.IsAlive && pair.Key.Target == key)
			{
				dict[pair.Key] = value;
				return;
			}
		dict[new WeakReference(key)] = value;
	}
	#endregion
	#region refl flag templates
	/// <summary>
	/// Binding flags for all normal contexts.
	/// </summary>
	public const BindingFlags BF_ALL_CONTEXTS
		= BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.Instance
		| BindingFlags.Static;
	/// <summary>
	/// Binding flags for all instance members regardless of visibility.
	/// </summary>
	public const BindingFlags BF_ALL_CONTEXTS_INSTANCE
		= BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.Instance;
	/// <summary>
	/// Binding flags for all static members regardless of visibility.
	/// </summary>
	public const BindingFlags BF_ALL_CONTEXTS_STATIC
		= BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.Static;
	/// <summary>
	/// Binding flags for all constructors.
	/// </summary>
	public const BindingFlags BF_ALL_CONTEXTS_CTOR
		= BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.CreateInstance;
	#endregion
	#region refl helpers
	/// <summary>
	/// Gets a method regardless of visibility.
	/// </summary>
	/// <param name="self">Type to get methods from</param>
	/// <param name="name">Name of the method</param>
	/// <returns></returns>
	public static MethodInfo? GetMethodAllContexts(
		this Type self,
		string name)
	{
		return self.GetMethod(name, BF_ALL_CONTEXTS);
	}

	/// <summary>
	/// Gets
	/// </summary>
	/// <param name="self"></param>
	/// <param name="name"></param>
	/// <returns></returns>
	public static PropertyInfo? GetPropertyAllContexts(
		this Type self,
		string name)
	{
		return self.GetProperty(name, BF_ALL_CONTEXTS);
	}

	/// <summary>
	/// Returns autoimplemented property backing field name
	/// </summary>
	public static string BackingFieldName(string propname)
	{
		return $"<{propname}>k__BackingField";
	}

	/// <summary>
	/// Looks up methodinfo from T, defaults to <see cref="BF_ALL_CONTEXTS_INSTANCE"/>
	/// </summary>
	/// <typeparam name="T">Target type</typeparam>
	/// <param name="mname">Method name</param>
	/// <param name="context">Binding flags, default private+public+instance</param>
	/// <returns></returns>
	public static MethodInfo? methodof<T>(
		string mname,
		BindingFlags context = BF_ALL_CONTEXTS_INSTANCE)
	{
		return typeof(T).GetMethod(mname, context);
	}
	/// <summary>
	/// Looks up methodinfo from t, defaults to <see cref="BF_ALL_CONTEXTS_STATIC"/>
	/// </summary>
	/// <param name="t">Target type</param>
	/// <param name="mname">Method name</param>
	/// <param name="context">Binding flags, default private+public+static</param>
	/// <returns></returns>
	public static MethodInfo? methodof(
		Type t,
		string mname,
		BindingFlags context = BF_ALL_CONTEXTS_STATIC)
	{
		return t.GetMethod(mname, context);
	}

	/// <summary>
	/// Gets constructorinfo from T. no cctors by default.
	/// </summary>
	/// <typeparam name="T">Type to look at</typeparam>
	/// <param name="context">Binding flags. Does not include static constructors by default.</param>
	/// <param name="pms">Constructor parameter types.</param>
	/// <returns></returns>
	public static ConstructorInfo? ctorof<T>(
		BindingFlags context = BF_ALL_CONTEXTS_CTOR,
		params Type[] pms)
	{
		return typeof(T).GetConstructor(context, null, pms, null);
	}

	/// <summary>
	/// Gets constructorinfo from T.
	/// </summary>
	/// <typeparam name="T">Type to look at.</typeparam>
	/// <param name="pms">Constructor parameter types.</param>
	/// <returns></returns>
	public static ConstructorInfo? ctorof<T>(params Type[] pms)
	{
		return typeof(T).GetConstructor(pms);
	}

	/// <summary>
	/// Takes fieldinfo from T, defaults to <see cref="BF_ALL_CONTEXTS_INSTANCE"/>
	/// </summary>
	/// <typeparam name="T">Target type</typeparam>
	/// <param name="name">Field name</param>
	/// <param name="context">Context, default private+public+instance</param>
	/// <returns></returns>
	public static FieldInfo? fieldof<T>(string name, BindingFlags context = BF_ALL_CONTEXTS_INSTANCE)
	{
		return typeof(T).GetField(name, context);
	}
	/// <summary>
	/// Yields all loaded assemblies with names matching a given regex.
	/// </summary>
	/// <param name="pattern">Regular expression to filter assemblies</param>
	/// <returns>A yield ienumerable with results</returns>
	public static IEnumerable<Assembly> FindAssemblies(string pattern)
	{
		Assembly[] lasms = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = lasms.Length - 1; i > -1; i--)
			if (REG.Regex.Match(lasms[i].FullName, pattern).Success) yield return lasms[i];
	}
	/// <summary>
	/// Force clones an object instance through reflection
	/// </summary>
	/// <typeparam name="T">Object type</typeparam>
	/// <param name="from">Source object</param>
	/// <param name="to">Destination object</param>
	/// <param name="context">Specifies context of fields to be cloned</param>
	public static void CloneInstance<T>(
		T from,
		T to,
		BindingFlags context = BF_ALL_CONTEXTS_INSTANCE)
	{
		Type tt = typeof(T);
		foreach (FieldInfo field in tt.GetFields(context))
		{
			if (field.IsStatic) continue;
			field.SetValue(to, field.GetValue(from), context, null, System.Globalization.CultureInfo.CurrentCulture);
		}
	}
	/// <summary>
	/// Cleans up static reference members in a type.
	/// </summary>
	/// <param name="t">Target type</param>
	public static (List<string>, List<string>) CleanupStatic(this Type t)
	{
		List<string> success = new();
		List<string> failure = new();

		foreach (FieldInfo field in t.GetFields(BF_ALL_CONTEXTS_STATIC))
			if (!field.FieldType.IsValueType)
			{
				string fullname = $"{t.FullName}.{field.Name}";
				try
				{
					field.SetValue(null, null, BF_ALL_CONTEXTS_STATIC, null, System.Globalization.CultureInfo.CurrentCulture);
					success.Add(fullname);
				}
				catch (Exception ex)
				{
					failure.Add(fullname + $" (exception: {ex.Message})");
				}
			}
		foreach (Type nested in t.GetNestedTypes(BF_ALL_CONTEXTS_STATIC))
		{
			var res = nested.CleanupStatic();
			success.AddRange(res.Item1);
			failure.AddRange(res.Item2);
		}
		return (success, failure);
	}

	/// <summary>
	/// Generic wrapper for <see cref="Delegate.CreateDelegate(Type, object, MethodInfo)"/>.
	/// </summary>
	/// <typeparam name="T">Delegate type</typeparam>
	/// <param name="inst">Instance. Set to null to treat method as static.</param>
	/// <param name="method">Target method.</param>
	/// <returns>Resulting delegate; null if failure.</returns>
	public static T? GetFn<T>(object? inst, MethodInfo method)
		where T : MulticastDelegate
	{
		BangBang(method, nameof(method));
		return (T)Delegate.CreateDelegate(typeof(T), inst, method);
	}
	/// <summary>
	/// Gets an internal char setter for given string.
	/// </summary>
	/// <param name="s"></param>
	/// <returns></returns>
	public static Action<int, char> Fn_SetChar(this string s)
	{
		MethodInfo method = methodof<string>("InternalSetChar", BF_ALL_CONTEXTS_INSTANCE)!;
		return GetFn<Action<int, char>>(s, method)!;
	}
	#endregion
	#region randomization extensions
	/// <summary>
	/// Returns a random deviation from start position, up to mDev in both directions. Clamps to given bounds if provided.
	/// </summary>
	/// <param name="start">Center of the spread.</param>
	/// <param name="mDev">Maximum deviation.</param>
	/// <param name="minRes">Result lower bound.</param>
	/// <param name="maxRes">Result upper bound.</param>
	/// <returns>The resulting value.</returns>
	public static int ClampedIntDeviation(
		int start,
		int mDev,
		int minRes = int.MinValue,
		int maxRes = int.MaxValue)
	{
		return IntClamp(RNG.Range(start - mDev, start + mDev), minRes, maxRes);
	}

	/// <summary>
	/// Returns a random deviation from start position, up to mDev in both directions. Clamps to given bounds if provided.
	/// </summary>
	/// <param name="start">Center of the spread.</param>
	/// <param name="mDev">Maximum deviation.</param>
	/// <param name="minRes">Result lower bound.</param>
	/// <param name="maxRes">Result upper bound.</param>
	/// <returns>The resulting value.</returns>
	public static float ClampedFloatDeviation(
		float start,
		float mDev,
		float minRes = float.NegativeInfinity,
		float maxRes = float.PositiveInfinity)
	{
		return Clamp(Lerp(start - mDev, start + mDev, RNG.value), minRes, maxRes);
	}

	/// <summary>
	/// Gives you a random sign.
	/// </summary>
	/// <returns>1f or -1f on a coinflip.</returns>
	public static float RandSign()
	{
		return RNG.value > 0.5f ? -1f : 1f;
	}

	/// <summary>
	/// Performs a random lerp between two 2d points.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	/// <returns>Resulting vector.</returns>
	public static Vector2 V2RandLerp(Vector2 a, Vector2 b)
	{
		return Vector2.Lerp(a, b, RNG.value);
	}

	/// <summary>
	/// Clamps a color to acceptable values.
	/// </summary>
	/// <param name="bcol"></param>
	/// <returns></returns>
	public static Color Clamped(this Color bcol)
	{
		return new(Clamp01(bcol.r), Clamp01(bcol.g), Clamp01(bcol.b));
	}
	/// <summary>
	/// Performs a channelwise random deviation on a color.
	/// </summary>
	/// <param name="bcol">base</param>
	/// <param name="dbound">deviations</param>
	/// <param name="clamped">whether to clamp the result to reasonable values</param>
	/// <returns>resulting colour</returns>
	public static Color RandDev(this Color bcol, Color dbound, bool clamped = true)
	{
		Color res = default;
		for (int i = 0; i < 3; i++) res[i] = bcol[i] + (dbound[i] * RNG.Range(-1f, 1f));
		return clamped ? res.Clamped() : res;
	}
	#endregion
	#region misc bs
	public static IEnumerable<int> Range(int bound)
	{
		for (int i = 0; i < bound; i++) yield return i;
	}
	public static T ParseExtEnum<T>(string str, bool ignoreCase = false)
		where T : ExtEnumBase
		=> (T)ExtEnumBase.Parse(typeof(T), str, ignoreCase);
	public static bool TryParseExtEnum<T>(string str, bool ignoreCase, out T val)
		where T : ExtEnumBase
	{
		var res = ExtEnumBase.TryParse(typeof(T), str, false, out ExtEnumBase o_val);
		val = (T)o_val;
		return res;
	}

	public static bool TryParseExtEnum_Example<T>(string value, bool ignoreCase, out T? result)
		where T : ExtEnum<T>
	{
		Type enumType = typeof(T);
		result = null;
		if (enumType == null || value == null || !enumType.IsExtEnum()) return false;
		value = value.Trim();
		if (value.Length == 0) return false;
		int resindex = -1;
		ExtEnumType extEnumType = ExtEnumBase.GetExtEnumType(enumType);
		StringComparer comp = ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture;

		for (int i = 0; i < extEnumType.entries.Count; i++)
		{
			if (comp.Compare(extEnumType.entries[i], value) == 0)
			{
				value = extEnumType.entries[i];
				resindex = i;
				result = (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
				result.index = resindex;
				result.value = value;
				result.localVersion = ExtEnum<T>.valuesVersion;
				//result.enumType = typeof(T);
				return true;
			}
		}
		//entry not found
		return false;
	}

	public static T Zero<T>()
		=> (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
	/// <summary>
	/// Attempts to parse enum value from a string, in a non-throwing fashion.
	/// </summary>
	/// <typeparam name="T">Enum type</typeparam>
	/// <param name="str">Source string</param>
	/// <param name="result">out-result.</param>
	/// <returns>Whether parsing was successful.</returns>
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
	/// <summary>
	/// Attempts to parse a vector4 from string; expected format is "x;y;z;w", z or w may be absent.
	/// </summary>
	public static bool TryParseVec4(string str, out Vector4 vec)
	{
		string[] spl;
		Vector4 vecres = default;
		bool vecparsed = false;
		if ((spl = REG.Regex.Split(str, "\\s*;\\s*")).Length is 2 or 3 or 4)
		{
			vecparsed = true;
			for (int i = 0; i < spl.Length; i++)
			{
				if (!float.TryParse(spl[i], out float val)) vecparsed = false;
				vecres[i] = val;
			}
		}
		vec = vecres;
		return vecparsed;
	}
	/// <summary>
	/// Joins two strings with a comma and a space.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	public static string JoinWithComma(string x, string y)
	{
		return $"{x}, {y}";
	}
	/// <summary>
	/// Stitches a given collection with, returns an empty string if empty.
	/// </summary>
	/// <param name="coll"></param>
	/// <param name="aggregator">Aggregator function. <see cref="JoinWithComma"/> by default.</param>
	/// <returns>Resulting string.</returns>
	public static string Stitch(
		this IEnumerable<string> coll,
		Func<string, string, string>? aggregator = null)
	{
		BangBang(coll, nameof(coll));
		return coll is null || coll.Count() is 0 ? string.Empty : coll.Aggregate(aggregator ?? JoinWithComma);
	}
	/// <summary>
	/// Creates an <see cref="IntRect"/> from two corner points.
	/// </summary>
	/// <param name="p1"></param>
	/// <param name="p2"></param>
	/// <returns></returns>
	public static IntRect ConstructIR(IntVector2 p1, IntVector2 p2)
	{
		Vector4 vec = new Color();
		return new(Min(p1.x, p2.x), Min(p1.y, p2.y), Max(p1.x, p2.x), Max(p1.y, p2.y));
	}
	/// <summary>
	/// <see cref="IO.Path.Combine"/> but params.
	/// </summary>
	/// <param name="parts"></param>
	/// <returns></returns>
	public static string CombinePath(params string[] parts)
	{
		return parts.Aggregate(IO.Path.Combine);
	}
	/// <summary>
	/// Current RainWorld instance. Uses Unity lookup, may be slow.
	/// </summary>
	public static RainWorld CRW
		=> UnityEngine.Object.FindObjectOfType<RainWorld>();
	/// <summary>
	/// Gets a <see cref="StaticWorld"/> template object by type.
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	public static CreatureTemplate GetCreatureTemplate(CreatureTemplate.Type t)
	{
		return StaticWorld.creatureTemplates[(int)t];
	}
	/// <summary>
	/// Finds specified subprocess in ProcessManager (looks at both mainloop and side processes).
	/// </summary>
	/// <typeparam name="T">Type of subprocess.</typeparam>
	/// <param name="manager">must not be null.</param>
	/// <returns>Found subprocess; null if none.</returns>
	public static T? FindSubProcess<T>(this ProcessManager manager)
		where T : MainLoopProcess
	{
		BangBang(manager, nameof(manager));
		if (manager.currentMainLoop is T tmain) return tmain;
		foreach (MainLoopProcess sideprocess in manager.sideProcesses) if (sideprocess is T tside) return tside;
		return null;
	}
	/// <summary>
	/// Attempts to find an <see cref="UpdatableAndDeletable"/> of specified type
	/// </summary>
	public static T? FindUAD<T>(this Room rm)
	{
		BangBang(rm, nameof(rm));
		for (int i = 0; i < rm.updateList.Count; i++)
		{
			if (rm.updateList[i] is T t) return t;
		}
		return default;
	}
	/// <summary>
	/// Yields all <see cref="UpdatableAndDeletable"/>s of specified type.
	/// </summary>
	public static IEnumerable<T> FindAllUAD<T>(this Room rm)
	{
		for (int i = 0; i < rm.updateList.Count; i++)
		{
			if (rm.updateList[i] is T t) yield return t;
		}
	}
	/// <summary>
	/// Gets bytes from ER of an assembly.
	/// </summary>
	/// <param name="resname">name of the resource</param>
	/// <param name="casm">target assembly. If unspecified, RK asm</param>
	/// <returns>resulting byte array</returns>
	public static byte[]? ResourceBytes(string resname, Assembly? casm = null)
	{
		if (resname is null) throw new ArgumentNullException("can not get with a null name");
		casm ??= Assembly.GetExecutingAssembly();
		IO.Stream? str = casm.GetManifestResourceStream(resname);
		byte[]? bf = str is null ? null : new byte[str.Length];
		str?.Read(bf, 0, (int)str.Length);
		return bf;
	}
	/// <summary>
	/// Gets an ER of an assembly and returns it as string. Default encoding is UTF-8
	/// </summary>
	/// <param name="resname">Name of ER</param>
	/// <param name="enc">Encoding. If none is specified, UTF-8</param>
	/// <param name="casm">assembly to get resource from. If unspecified, RK asm.</param> 
	/// <returns>Resulting string. If none is found, <c>null</c> </returns>
	public static string? ResourceAsString(string resname, Encoding? enc = null, Assembly? casm = null)
	{
		enc ??= Encoding.UTF8;
		casm ??= Assembly.GetExecutingAssembly();
		try
		{
			byte[]? bf = ResourceBytes(resname, casm);
			return bf is null ? null : enc.GetString(bf);
		}
		catch (Exception ee) { __logger.LogError($"Error getting ER: {ee}"); return null; }
	}
	/// <summary>
	/// Deconstructs a KeyValuePair.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TVal"></typeparam>
	/// <param name="kvp"></param>
	/// <param name="k"></param>
	/// <param name="v"></param>
	public static void Deconstruct<TKey, TVal>(this KeyValuePair<TKey, TVal> kvp, out TKey k, out TVal v)
	{
		k = kvp.Key;
		v = kvp.Value;
	}
	/// <summary>
	/// Throws <see cref="System.ArgumentNullException"/> if item is null.
	/// </summary>
	public static void BangBang(object? item, string name = "???")
	{
		if (item is null) throw new ArgumentNullException(name);
	}
	/// <summary>
	/// Throws <see cref="System.InvalidOperationException"/> if item is not null.
	/// </summary>
	public static void AntiBang(object? item, string name)
	{
		if (item is not null) throw new InvalidOperationException($"{name} is not null");
	}
	/// <summary>
	/// Creates a Color from a vector and makes sure its alpha is not zero.
	/// </summary>
	public static Color ToOpaqueCol(in this Vector4 vec)
		=> vec.w is not 0f ? vec : new(vec.x, vec.y, vec.z, 1f);
	public static IntVector2 ToIntVector2(this Vector2 vec) => new((int)vec.x, (int)vec.y);
	public static Vector2 ToVector2(this IntVector2 ivec) => new(ivec.x, ivec.y);
	public static IEnumerable<IntVector2> ReturnTiles(this IntRect ir)
	{
		for (int i = ir.left; i <= ir.right; i++)
		{
			for (int j = ir.bottom; j <= ir.top; j++)
			{
				yield return new(i, j);
			}
		}
	}


#if RK

	public static void ClampToNormal(this Color self)
	{
		self.r = Clamp01(self.r);
		self.g = Clamp01(self.g);
		self.b = Clamp01(self.b);
		self.a = Clamp01(self.a);
	}
	public static List<T> AddRangeReturnSelf<T>(this List<T> self, IEnumerable<T> range)
	{
		if (self == null) self = new List<T>();
		self.AddRange(range);
		return self;
	}
	public static Color Deviation(this Color self, Color dev)
	{
		var res = new Color();
		for (int i = 0; i < 4; i++)
		{
			res[i] = ClampedFloatDeviation(self[i], dev[i]);
		}
		return res;
	}

	public static FContainer ReturnFContainer(this RoomCamera rcam, ContainerCodes cc)
		=> rcam.ReturnFContainer(cc.ToString());
	public static string[] SplitAndRemoveEmpty(this string str, string separator) => str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

	#region refl extensions
	/// <summary>
	/// cleans up all non valuetype fields in a type. for realm cleanups
	/// </summary>
	/// <param name="t"></param>
	internal static void CleanUpStatic(this Type t)
	{
		foreach (var fld in t.GetFields(BF_ALL_CONTEXTS_STATIC))
		{
			try
			{
				if (fld.FieldType.IsValueType || fld.IsLiteral) continue;
				fld.SetValue(null, default);
			}
			catch { }

		}
		foreach (var child in t.GetNestedTypes()) child.CleanUpStatic();
	}

	#endregion
#endif
	#endregion
}
