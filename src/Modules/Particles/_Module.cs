using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RegionKit.Modules.Particles;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Particles")]
internal static class _Module
{
	internal static bool __appliedOnce = false;
	internal static readonly Dictionary<string, PVisualState> __namedPresets = new();

	internal static void Enable()
	{
		if (!__appliedOnce)
		{
			RegisterEmptyObjectType<V1.ParticleVisualCustomizer, ManagedRepresentation>("ParticleVisualCustomizer", RK_POM_CATEGORY);
			RegisterEmptyObjectType<V1.PresetParticleVisualCustomizer, ManagedRepresentation>("ParticleVisualPreset", RK_POM_CATEGORY);
			RegisterEmptyObjectType<V1.ParticleBehaviourProvider.WavinessProvider, ManagedRepresentation>("ParticleWaviness", RK_POM_CATEGORY);
			RegisterEmptyObjectType<V1.ParticleBehaviourProvider.SpinProvider, ManagedRepresentation>("ParticleSpin", RK_POM_CATEGORY);
			RegisterEmptyObjectType<V1.ParticleBehaviourProvider.PlainModuleRegister, ManagedRepresentation>("GenericPBMDispenser", RK_POM_CATEGORY);
			RegisterManagedObject<V1.RoomParticleSystem, V1.RectParticleSpawnerData, ManagedRepresentation>("RectParticleSpawner", RK_POM_CATEGORY);
			RegisterManagedObject<V1.RoomParticleSystem, V1.OffscreenSpawnerData, ManagedRepresentation>("OffscreenParticleSpawner", RK_POM_CATEGORY);
			RegisterManagedObject<V1.RoomParticleSystem, V1.WholeScreenSpawnerData, ManagedRepresentation>("WholeScreenSpawner", RK_POM_CATEGORY);
		}
		__appliedOnce = true;

		//IO.File.WriteAllText(AssetManager.ResolveFilePath("exampleParticle.json"), Newtonsoft.Json.JsonConvert.SerializeObject(PVisualState.Blank, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new IgnoreShit() }));
		__namedPresets.Clear();
		bool foundany = false;
		foreach (string file in AssetManager.ListDirectory("assets/regionkit/particlepresets", false, true))
		{
			IO.FileInfo fi = new(file);
			if (fi.Extension == ".json")
			{
				try
				{
					__logger.LogMessage($"Deserializing particle preset {fi.Name}...");
					var PVS = Newtonsoft.Json.JsonConvert.DeserializeObject<PVisualState>(IO.File.ReadAllText(file));
					__namedPresets.Add(fi.Name[..^5], PVS);
					foundany = true;
				}
				catch (Exception ex){
					__logger.LogError($"RKParticles Could not deserialize a particle tag preset {fi.Name} {ex}");
				}
			}
		}
		if (!foundany) { 

			__logger.LogWarning("Found no particle presets");
		}


	}
	internal static void Disable()
	{

	}

	private class IgnoreShit : Newtonsoft.Json.Serialization.DefaultContractResolver
	{
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var props = base.CreateProperties(type, memberSerialization);
			if (type == typeof(Color)){
				props = props.Where(x => x.PropertyName is "r" or "g" or "b" or "a").ToList();
			}
			return props;
		}
	}
}
