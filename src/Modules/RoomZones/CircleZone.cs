namespace RegionKit.Modules.RoomZones;

public class CircleZone : ZoneBase<UnityEngine.CircleCollider2D, CircleZoneData>
{
	private (Vector2, float) _c_posandrad;
	public CircleZone(Room rm, PlacedObject owner) : base(rm, owner)
	{
	}

	protected override bool NeedToUpdateTileCache
		=> _c_posandrad != ((Vector2)_collider.transform.position, _collider.radius);

	protected override void BringCacheToCurrent()
	{
		_c_posandrad = ((Vector2)_collider.transform.position, _collider.radius);
	}

	protected override void BuildTileCache()
	{
		var center = (Vector2)_collider.bounds.center;

		for (float i = center.x - _collider.radius; i < center.x + _collider.radius; i += 20)
		{
			for (float j = center.y - _collider.radius; j < center.y + _collider.radius; j += 20)
			{
				Vector2 pt = new(i, j);
				Room.Tile tile = room.GetTile(pt);
				if (DistLess(pt, center, _collider.radius)) _c_affectedTiles.Add(new(tile.X, tile.Y));
			}
		}
		//throw new NotImplementedException();
	}

	protected override void SyncColliderToData()
	{
        _collider.transform.position = _owner.pos;
        _collider.radius = _Data.p2.magnitude;
		
	}
}
