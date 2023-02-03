namespace RegionKit.Modules.Particles.V2;

public class ParticleRectZone : ManagedData, IParticleZone
{
	[IntVector2Field("p2", 3, 3, IntVector2Field.IntVectorReprType.rect)]
	public IntVector2 p2;
	[IntegerField("group", -99, 99, 0, ManagedFieldWithPanel.ControlType.arrows, "Group")]
	public int group;
	public ParticleRectZone(PlacedObject owner) : base(owner, null)
	{
	}
	public PlacedObject Owner => owner;
	public IEnumerable<IntVector2> SelectedTiles
	{
		get
		{
			//todo: check if works right
			IntVector2
				P1 = (owner.pos / 20f).ToIntVector2(),
				P2 = p2;
			for (int i = Math.Min(P1.x, P2.x); i < Math.Max(P1.x, P2.x); i++)
			{
				for (int j = Math.Min(P1.y, P2.y); j < Math.Max(P1.y, P2.y); j++)
				{
					yield return new(i, j);
				}
			}
		}
	}
	public int Group => group;
}
