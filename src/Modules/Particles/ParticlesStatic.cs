using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace RegionKit.Particles
{
	internal static class ParticlesStatic
	{
		internal static void Enable()
		{
			if (!AppliedOnce)
			{
				RegisterMPO();
			}
			AppliedOnce = true;
		}
		internal static bool AppliedOnce = false;
		internal static void RegisterMPO()
		{
			RegisterEmptyObjectType<ParticleVisualCustomizer, ManagedRepresentation>("ParticleVisualCustomizer");
			RegisterEmptyObjectType<ParticleBehaviourProvider.WavinessProvider, ManagedRepresentation>("ParticleWaviness");
			RegisterEmptyObjectType<ParticleBehaviourProvider.SpinProvider, ManagedRepresentation>("ParticleSpin");
			RegisterEmptyObjectType<ParticleBehaviourProvider.PlainModuleRegister, ManagedRepresentation>("GenericPBMDispenser");
			RegisterManagedObject<RoomParticleSystem, RectParticleSpawnerData, ManagedRepresentation>("RectParticleSpawner");
			RegisterManagedObject<RoomParticleSystem, OffscreenSpawnerData, ManagedRepresentation>("OffscreenParticleSpawner");
			RegisterManagedObject<RoomParticleSystem, WholeScreenSpawnerData, ManagedRepresentation>("WholeScreenSpawner");
		}
		internal static void Disable()
		{

		}
	}
}
