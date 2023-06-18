namespace RegionKit.Extras;

public static class LanguageHacks {

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
}