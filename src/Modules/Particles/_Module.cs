﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RegionKit.Modules.Particles;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Particles")]
internal static class _Module
{
	internal const string PARTICLES_POM_CATEGORY = RK_POM_CATEGORY + "-Particles";
	internal static readonly Dictionary<string, ParticleVisualState> __namedPresets = new();

	internal static void Setup()
	{
		RegisterEmptyObjectType<V1.ParticleVisualCustomizer, ManagedRepresentation>("ParticleVisualCustomizer", PARTICLES_POM_CATEGORY);
		RegisterEmptyObjectType<V1.PresetParticleVisualCustomizer, ManagedRepresentation>("ParticleVisualPreset", PARTICLES_POM_CATEGORY);
		RegisterEmptyObjectType<V1.ParticleBehaviourProvider.WavinessProvider, ManagedRepresentation>("ParticleWaviness", PARTICLES_POM_CATEGORY);
		RegisterEmptyObjectType<V1.ParticleBehaviourProvider.SpinProvider, ManagedRepresentation>("ParticleSpin", PARTICLES_POM_CATEGORY);
		RegisterEmptyObjectType<V1.ParticleBehaviourProvider.PlainModuleRegister, ManagedRepresentation>("GenericPBMDispenser", PARTICLES_POM_CATEGORY);
		RegisterManagedObject<V1.RoomParticleSystem, V1.RectParticleSpawnerData, ManagedRepresentation>("RectParticleSpawner", PARTICLES_POM_CATEGORY);
		RegisterManagedObject<V1.RoomParticleSystem, V1.OffscreenSpawnerData, ManagedRepresentation>("OffscreenParticleSpawner", PARTICLES_POM_CATEGORY);
		RegisterManagedObject<V1.RoomParticleSystem, V1.WholeScreenSpawnerData, ManagedRepresentation>("WholeScreenSpawner", PARTICLES_POM_CATEGORY);
	}
	internal static void Enable()
	{
		__namedPresets.Clear();
		bool foundany = false;
		foreach (string file in AssetManager.ListDirectory("assets/regionkit/particlepresets", false, true))
		{
			System.IO.FileInfo fi = new(file);
			if (fi.Extension == ".json")
			{
				try
				{
					LogMessage($"Deserializing particle preset {fi.Name}...");
					var PVS = Newtonsoft.Json.JsonConvert.DeserializeObject<ParticleVisualState>(System.IO.File.ReadAllText(file));
					__namedPresets.Add(fi.Name[..^5], PVS);
					foundany = true;
				}
				catch (Exception ex)
				{
					LogError($"RKParticles Could not deserialize a particle tag preset {fi.Name} {ex}");
				}
			}
		}
		if (!foundany)
		{

			LogfixWarning("Found no particle presets");
		}


	}
	internal static void Disable()
	{

	}
	internal static bool TryFindPreset(string tags, out ParticleVisualState state)
	{
		return _Module.__namedPresets.TryGetValue(System.Text.RegularExpressions.Regex.Split(tags, "\\s*,\\s*").RandomOrDefault() ?? "default", out state);
	}
	private class IgnoreShit : Newtonsoft.Json.Serialization.DefaultContractResolver
	{
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var props = base.CreateProperties(type, memberSerialization);
			if (type == typeof(Color))
			{
				props = props.Where(x => x.PropertyName is "r" or "g" or "b" or "a").ToList();
			}
			return props;
		}
	}
}
