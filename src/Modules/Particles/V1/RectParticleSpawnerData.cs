namespace RegionKit.Modules.Particles.V1;
/// <summary>
/// SpawnerData for tossing in particles in a rectangular area
/// </summary>
public class RectParticleSpawnerData : ParticleSystemData
{
	[Vector2Field("effRect", 40f, 40f, Vector2Field.VectorReprType.rect)]
	Vector2 RectBounds;
	///<inheritdoc/>
	public RectParticleSpawnerData(PlacedObject owner) : base(owner, null)
	{

	}
	//cached second point for areaNeedsRefresh
	private Vector2 _c_rectBounds;
	///<inheritdoc/>
	protected override bool AreaNeedsRefresh => base.AreaNeedsRefresh && _c_rectBounds == RectBounds;
	///<inheritdoc/>
	protected override void UpdateTilesetCacheValidity()
	{
		base.UpdateTilesetCacheValidity();
		_c_rectBounds = RectBounds;
	}
	///<inheritdoc/>
	protected override List<IntVector2> GetSuitableTiles(Room rm)
	{
		//c_RB = RectBounds;
		var res = new List<IntVector2>();
		IntVector2 orpos = (owner.pos / 20f).ToIntVector2();
		IntVector2 bounds = ((owner.pos + RectBounds) / 20f).ToIntVector2();
		IntRect area = IntRect.MakeFromIntVector2(orpos);
		area.ExpandToInclude(bounds);
		for (int x = area.left; x < area.right; x++)
		{
			for (int y = area.bottom; y < area.top; y++)
			{
				res.Add(new IntVector2(x, y));
			}
		}
		//PetrifiedWood.WriteLine(Json.Serialize(res));
		return res;
	}
}
