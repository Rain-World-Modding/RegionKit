namespace RegionKit.Modules.Machinery.V2;

public interface IOscillationProvider
{
	public int Tag { get; }
	public OscillationParams OscillationForNew();
	internal class Default : IOscillationProvider
	{
		public readonly static Default one = new();
		public int Tag => int.MinValue;

		public OscillationParams OscillationForNew() => new OscillationParams(0f, 1f, 0.5f, 0f, Mathf.Sin);
	}
}
