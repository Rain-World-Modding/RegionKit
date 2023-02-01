
namespace RegionKit.Modules.Particles;

internal interface IParticleVisualProvider
{
	//PVisualState GetNewForParticle(GenericParticle)
	ParticleVisualState DataForNew();
	Vector2 P2 { get; }
	PlacedObject Owner { get; }
	//int ApplyOrder{get;}
}
