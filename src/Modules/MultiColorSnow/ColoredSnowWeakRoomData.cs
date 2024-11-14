namespace RegionKit.Modules.MultiColorSnow;

using System.Runtime.CompilerServices;

public class ColoredSnowWeakRoomData
{
	private static readonly ConditionalWeakTable<Room, ColoredSnowWeakRoomData> weakData = new ConditionalWeakTable<Room, ColoredSnowWeakRoomData>();

	public List<ColoredSnowSourceUAD> snowSources = new List<ColoredSnowSourceUAD>();
	public Dictionary<int, ColoredSnowGroupUAD> snowPalettes = new Dictionary<int, ColoredSnowGroupUAD>();
	public ColoredSnowDrawable? snowObject;
	public bool snow;

	public static ColoredSnowWeakRoomData GetData(Room obj)
	{
		return weakData.GetOrCreateValue(obj);
	}
}

