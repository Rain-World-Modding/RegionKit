namespace RegionKit.Modules.MultiColorSnow;
public class ColoredSnowWeakRoomData
{
	private static readonly RKConditionalWeakTable<Room, ColoredSnowWeakRoomData> weakData = new();

	public List<ColoredSnowSourceUAD> snowSources = new List<ColoredSnowSourceUAD>();
	public Dictionary<int, ColoredSnowGroupUAD> snowPalettes = new Dictionary<int, ColoredSnowGroupUAD>();
	public ColoredSnowDrawable? snowObject;
	public bool snow;

	public static ColoredSnowWeakRoomData GetData(Room obj)
	{
		return weakData.GetValue(obj, _ => new());
	}
}

