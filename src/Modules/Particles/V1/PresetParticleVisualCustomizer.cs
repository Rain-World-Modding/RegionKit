namespace RegionKit.Modules.Particles.V1;
/// <summary>
/// Dispenses particle visuals from file presets
/// </summary>
public class PresetParticleVisualCustomizer : ManagedData, IParticleVisualProvider
{
	/// <summary>
	/// List of tags, separated by commas
	/// </summary>
	[StringField("tags", "default", "Preset tags")]
	public string presetTags = "default";
	///<inheritdoc/>
	[Vector2Field("p2", 40f, 0f, Vector2Field.VectorReprType.circle)]
	public Vector2 p2;
	///<inheritdoc/>
	public PresetParticleVisualCustomizer(PlacedObject owner) : base(owner, null)
	{

	}
	///<inheritdoc/>
	public Vector2 P2 => p2;
	///<inheritdoc/>
	public PlacedObject Owner => owner;
	///<inheritdoc/>
	public ParticleVisualState DataForNew()
	{
		ParticleVisualState state;
		if (_Module.TryFindPreset(presetTags, out state)) return state;
		return ParticleVisualState.Blank;
	}
}
