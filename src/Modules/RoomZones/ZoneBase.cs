namespace RegionKit.Modules.RoomZones;

public abstract class ZoneBase<TC, TD> : UpdatableAndDeletable, IRoomZone
	where TC : UnityEngine.Collider2D
	where TD : ZoneBaseData
{
	protected PlacedObject _owner;
	protected TD _Data => (TD)_owner.data;
	protected GameObject _collider_holder;
	protected TC _collider;
	protected readonly List<IntVector2> _c_affectedTiles = new();

	public ZoneBase(Room rm, PlacedObject owner)
	{
		room = rm;
		_owner = owner;
		_collider_holder = new GameObject($"{this}-colliderholder");
		_collider = _collider_holder.AddComponent<TC>();
		_Module.colliderHolders.Add(_collider_holder);
	}
	~ZoneBase()
	{
		_Module.colliderHolders.Remove(_collider_holder);
		UnityEngine.GameObject.Destroy(_collider_holder);

	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		SyncColliderToData();
		if (NeedToUpdateTileCache)
		{
			__logger.LogDebug("rebuilding tilecache");
			_c_affectedTiles.Clear();
			BuildTileCache();
		}
		BringCacheToCurrent();
	}
	protected abstract bool NeedToUpdateTileCache { get; }
	protected abstract void BuildTileCache();
	protected abstract void SyncColliderToData();
	protected abstract void BringCacheToCurrent();

	public virtual Collider2D Collider => _collider;

	public virtual IEnumerable<IntVector2> AffectedTiles => _c_affectedTiles.AsReadOnly();

	public virtual int Tag
		=> _Data.tag;

	public virtual bool PointInZone(Vector2 point)
		=> _collider.OverlapPoint(point);

}
