namespace RegionKit.Extras;

public static class WeakDictTools
{

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
}