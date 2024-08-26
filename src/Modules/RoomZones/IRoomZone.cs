namespace RegionKit.Modules.RoomZones;

public interface IRoomZone
{
	public Collider2D Collider { get; }
	public IEnumerable<IntVector2> AffectedTiles { get; }
	public bool PointInZone(Vector2 point);
	public int Tag { get; }
}
