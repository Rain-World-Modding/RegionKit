namespace RegionKit.Modules.RoomZones;

public class CircleZoneData : ZoneBaseData
{
	[BackedByField("01p2")]
	public Vector2 p2;
	public CircleZoneData(PlacedObject owner)
		: base(owner, new ManagedField[] { new Vector2Field("01p2", new(20f, 20f), Vector2Field.VectorReprType.circle) })
	{
	}
}
