namespace RegionKit.Modules.Particles.V2;

public interface IParticleZone
{
	IEnumerable<IntVector2> SelectedTiles {get;}
	int Group { get; }
	PlacedObject Owner { get; }
}
