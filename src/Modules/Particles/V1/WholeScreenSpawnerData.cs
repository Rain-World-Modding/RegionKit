namespace RegionKit.Modules.Particles.V1;
public class WholeScreenSpawnerData : ParticleSystemData
{
	[IntegerField("Margin", 0, 30, 1, displayName: "Margin")]
	public int Margin;
	[BooleanField("nosolid", true, displayName: "Skip solid tiles")]
	public bool AirOnly;

	public WholeScreenSpawnerData(PlacedObject owner) : base(owner, null)
	{

	}
	protected override List<IntVector2> GetSuitableTiles(Room rm)
	{
		var r = ConstructIR(new IntVector2(-Margin, -Margin), new IntVector2(rm.TileWidth + Margin, rm.TileHeight + Margin))
			.ReturnTiles()
			.ToList();
		if (AirOnly)
		{
			for (int i = r.Count() - 1; i > -1; i--)
			{
				if (rm.GetTile(r[i]).Solid) r.RemoveAt(i);
			}
		}
		return r;
	}
}
