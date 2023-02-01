namespace RegionKit.Modules.Particles.V1;

public class PresetParticleVisualCustomizer : ManagedData, IParticleVisualProvider
{
	[StringField("tags", "default", "Preset tags")]
	public string presetTags = "default";
	[Vector2Field("p2", 40f, 0f, Vector2Field.VectorReprType.circle)]
	public Vector2 p2;

	public PresetParticleVisualCustomizer(PlacedObject owner) : base(owner, null)
	{

	}

	public Vector2 P2 => p2;

	public PlacedObject Owner => owner;

	public ParticleVisualState DataForNew()
	{
		ParticleVisualState state;
		if (_Module.TryFindPreset(presetTags, out state)) return state;
		return ParticleVisualState.Blank;
	}
}
