namespace RegionKit.Modules.Machinery.V2;

public class POMOscillationProvider : ManagedData, IOscillationProvider
{
	[IntegerField("00tag", -100, 100, 0, displayName: "tag")]
	public int tag;
	[EnumField<OscillationMode>("01mode", OscillationMode.Sinal, displayName: "Operation mode")]
	public OscillationMode opmode;

	[FloatField("02amp", 0f, 60f, 20f, increment: 1f, ManagedFieldWithPanel.ControlType.slider, displayName: "Amplitude")]
	public float amplitude = 20f;
	[FloatField("03phase", -5f, 5f, 0f, control: ManagedFieldWithPanel.ControlType.slider, displayName: "Phase")]
	public float phase = 0f;
	[FloatField("04frq", 0.05f, 2f, 1f, 0.05f, ManagedFieldWithPanel.ControlType.slider, displayName: "Frequency")]
	public float frequency = 1f;
	[FloatField ("05base", -40f, 40f, 0f, 0.1f, ManagedFieldWithPanel.ControlType.slider, displayName: "Base value")]
	public float baseValue;
	public POMOscillationProvider(PlacedObject owner) : base(owner, null)
	{
	}
	public int Tag => tag;
	public OscillationParams OscillationForNew()
	{
		return new(baseValue, amplitude, frequency, phase, _Module.__defaultOscillators[opmode]);
	}
}