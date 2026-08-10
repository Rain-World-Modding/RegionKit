using System.Runtime.CompilerServices;

namespace RegionKit.Modules.MoonStuff
{
	internal static class _CWTs
	{
		static readonly ConditionalWeakTable<Region, RegionData> RegionCWT = new ConditionalWeakTable<Region, RegionData>();
		static readonly ConditionalWeakTable<SpotLight, SpotLightData> SpotLightCWT = new ConditionalWeakTable<SpotLight, SpotLightData>();
		static readonly ConditionalWeakTable<LightSource, LightSourceData> LightSourceCWT = new ConditionalWeakTable<LightSource, LightSourceData>();
		static readonly ConditionalWeakTable<LightBeam, LightBeamData> LightBeamCWT = new ConditionalWeakTable<LightBeam, LightBeamData>();

		public static RegionData MoonRegionData(this Region self) => RegionCWT.GetOrCreateValue(self);
		public static SpotLightData MoonSpotLightData(this SpotLight self) => SpotLightCWT.GetOrCreateValue(self);
		public static LightSourceData MoonLightSourceData(this LightSource self) => LightSourceCWT.GetOrCreateValue(self);
		public static LightBeamData MoonLightBeamData(this LightBeam self) => LightBeamCWT.GetOrCreateValue(self);

		public class RegionData
		{
			public HSLColor CrystalColor;

			public float OESphereHue;

			public bool hideTimer;
		}

		public class SpotLightData
		{
			public bool On;
		}

		public class LightSourceData
		{
			public bool On;
		}

		public class LightBeamData
		{
			public bool On;
		}
	}
}
