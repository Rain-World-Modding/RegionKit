namespace RegionKit.Modules.Particles.V2;
/// <summary>
/// Acts as a spawn zone for V2 particle systems. Implementors should be PlacedObjectData
/// </summary>
public interface IParticleZone
{
	/// <summary>
	/// Tiles selected by the instance
	/// </summary>
	IEnumerable<IntVector2> SelectedTiles { get; }
	/// <summary>
	/// Group that tags the instance
	/// </summary>
	/// <value></value>
	int Group { get; }
	/// <summary>
	/// PlacedObject this instance belongs to
	/// </summary>
	PlacedObject Owner { get; }
}
