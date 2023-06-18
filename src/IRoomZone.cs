namespace RegionKit;

public interface IRoomZone
{
	public UnityEngine.Collider2D Collider { get; }
	public IEnumerable<IntVector2> AffectedTiles { get; }
	public bool PointInZone(Vector2 point);
	public int Tag { get; }
}