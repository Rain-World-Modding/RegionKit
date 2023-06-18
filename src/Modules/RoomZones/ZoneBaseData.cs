namespace RegionKit.Modules.RoomZones;

public abstract class ZoneBaseData : ManagedData
{
	[IntegerField("00tag", 0, 100, 0, displayName: "tag")]
	public int tag;
	public ZoneBaseData(PlacedObject owner, ManagedField[] additionalFields) : base(owner, additionalFields)
	{
	}
}
