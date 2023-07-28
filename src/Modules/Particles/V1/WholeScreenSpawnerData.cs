namespace RegionKit.Modules.Particles.V1;
/// <summary>
/// Spawns particles everywhere on the screen
/// </summary>
public class WholeScreenSpawnerData : ParticleSystemData
{
	/// <summary>
	/// Number of additional tiles on all sides (in case room geometry is smaller than visual size)
	/// </summary>
	[IntegerField("Margin", 0, 30, 1, displayName: "Margin")]
	public int margin;
	/// <summary>
	/// Do not spawn in solid tiles
	/// </summary>
	[BooleanField("nosolid", true, displayName: "Skip solid tiles")]
	public bool airOnly;
	///<inheritdoc/>
	public WholeScreenSpawnerData(PlacedObject owner) : base(owner, new())
	{

	}
	///<inheritdoc/>
	protected override List<IntVector2> GetSuitableTiles(Room rm)
	{
		var r = ConstructIR(new IntVector2(-margin, -margin), new IntVector2(rm.TileWidth + margin, rm.TileHeight + margin))
			.ReturnTiles()
			.ToList();
		if (airOnly)
		{
			for (int i = r.Count() - 1; i > -1; i--)
			{
				if (rm.GetTile(r[i]).Solid) r.RemoveAt(i);
			}
		}
		return r;
	}
}
