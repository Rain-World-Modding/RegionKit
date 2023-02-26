
namespace RegionKit.Modules.Particles;

internal interface IParticleVisualProvider
{
	//PVisualState GetNewForParticle(GenericParticle)
	ParticleVisualState StateForNew();
	Vector2 P2 { get; }
	PlacedObject Owner { get; }
	//int ApplyOrder{get;}
	internal class PlaceholderProv : IParticleVisualProvider
	{
		internal static PlaceholderProv instance = new();
		public Vector2 P2 => default;

		public PlacedObject Owner => null!;

		public ParticleVisualState StateForNew()
		{
			return new ParticleVisualState("SkyDandelion", "Basic", ContainerCodes.Foreground, Color.white, Color.white, 0.5f, 45f, 15f, 0f, false, 1f);
		}
	}
}
