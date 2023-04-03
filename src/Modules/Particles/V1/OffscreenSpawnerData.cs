namespace RegionKit.Modules.Particles.V1;
/// <summary>
/// Spawns particles outside screen borders
/// </summary>
public class OffscreenSpawnerData : ParticleSystemData
{
	#pragma warning disable 1591
	[IntegerField("margin", 0, 30, 1)]
	public int margin;
	[BooleanField("nosolid", true, displayName: "Skip solid tiles")]
	public bool AirOnly;
	#pragma warning restore 1591
	///<inheritdoc/>
	public OffscreenSpawnerData(PlacedObject owner) : base(owner, new())
	{
	}

	private Vector2 _c_dir;
	///<inheritdoc/>
	protected override void UpdateTilesetCacheValidity()
	{
		base.UpdateTilesetCacheValidity();
		_c_dir = base.GetValue<Vector2>("sdBase");
	}
	///<inheritdoc/>
	protected override bool AreaNeedsRefresh => base.AreaNeedsRefresh && _c_dir == base.GetValue<Vector2>("sdBase");
	///<inheritdoc/>
	protected override List<IntVector2> GetSuitableTiles(Room rm)
	{
		var res = new List<IntVector2>();
		var rb = new IntRect(0 - margin, 0 - margin, rm.Width + margin, rm.Height + margin);
		var dropVector = GetValue<Vector2>("sdBase");
		//var row = new List<IntVector2>();
		//var column = new List<IntVector2>();
		int ys = (dropVector.y > 0) ? rb.bottom : rb.top;
		int xs = (dropVector.x > 0) ? rb.left : rb.right;
		for (int x = rb.left; x < rb.right; x++)
		{
			var r = new IntVector2(x, ys);
			if (!rm.GetTile(r).Solid || !AirOnly) res.Add(r);
		}
		for (int y = rb.bottom; y < rb.top; y++)
		{
			var r = new IntVector2(xs, y);
			if (!rm.GetTile(r).Solid || !AirOnly) res.Add(r);
		}
		return res;
	}
}
