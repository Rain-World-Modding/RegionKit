namespace RegionKit.Modules.RoomZones;

public class RectZone : ZoneBase<BoxCollider2D, RectZoneData>
{
	private (Vector2, Vector2) _c_posandsize;
	public RectZone(Room rm, PlacedObject owner) : base(rm, owner)
	{

	}
	public override void Update(bool eu)
	{
		base.Update(eu);
	}

	protected override void BuildTileCache()
	{
		for (float i = _owner.pos.x; i < _owner.pos.x + _Data.p2.x; i += 20f)
		{
			for (float j = _owner.pos.y; j < _owner.pos.y + _Data.p2.y; j += 20f)
			{
				Room.Tile tile = room.GetTile(new Vector2(i, j));
				_c_affectedTiles.Add(new(tile.X, tile.Y));
			}
		}
	}

	protected override void SyncColliderToData()
	{
		_collider.size = _Data.p2;
		_collider.transform.position = _owner.pos + _collider.size / 2f;
		
	}

	protected override void BringCacheToCurrent()
	{
		_c_posandsize = ((Vector2)_collider.transform.position, _collider.size);
	}

	protected override bool NeedToUpdateTileCache
		=> _c_posandsize != ((Vector2)_collider.transform.position, _collider.size);
}
