namespace RegionKit.Modules.Machinery.V2;

public interface IVisualsProvider
{
	public PartVisuals VisualsForNew();
	public int Tag { get; }

	internal class Default : IVisualsProvider
	{
		public int Tag => int.MinValue;

		public PartVisuals VisualsForNew()
		{
			return new PartVisuals(
				"Circle20",
				"basic",
				ContainerCodes.Midground,
				Color.white,
				1f,
				1f,
				1f,
				0.5f,
				0.5f,
				0f);
		}
	}
}
