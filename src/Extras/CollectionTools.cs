namespace RegionKit.Extras;

public static class CollectionTools
{
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
	public static char? Get(this System.Text.StringBuilder sb, int index)
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
	internal static bool TryAdd<Tk, Tv>(this IDictionary<Tk, Tv> dict, Tk key, Tv val)
	{
		if (dict.ContainsKey(key)) return false;
		dict[key] = val;
		return true;
	}
	// internal static bool TryRemove<Tk, Tv>(this IDictionary<Tk, Tv> dict, Tk key)
	// {
	// 	if (!dict.ContainsKey(key)) return false;
	// 	dict.Remove(key);
	// 	return true;
	// }
	internal static bool IndexInRange<T>(this T[] arr, int index) => index > -1 && index < arr.Length;

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
	public static IEnumerable<int> Range(int bound)
	{
		for (int i = 0; i < bound; i++) yield return i;
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
	public static List<T> AddRangeReturnSelf<T>(this List<T>? self, IEnumerable<T> range)
	{
		if (self == null) self = new List<T>();
		self.AddRange(range);
		return self;
	}
}