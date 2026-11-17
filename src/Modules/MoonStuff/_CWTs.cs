using System.Runtime.CompilerServices;

namespace RegionKit.Modules.MoonStuff
{
	internal static class _CWTs
	{
		private static readonly RKConditionalWeakTable<Region, RegionData> RegionCWT = new();
		private static readonly RKConditionalWeakTable<LightSource, FlickerData> LightSourceCWT = new();
		private static readonly RKConditionalWeakTable<LightBeam, FlickerData> LightBeamCWT = new();
		private static readonly RKConditionalWeakTable<IFlickerable, FlickerData> FlickerableCWT = new();
		private static bool usingFlickerData = false;

		public static RegionData MoonRegionData(this Region self)
		{
			return RegionCWT.GetOrCreateValue(self);
		}

		public static void InitMoonLightSourceData(this LightSource self)
		{
			FlickerData data = LightSourceCWT.GetOrCreateValue(self);
			data.On = true;
			data.LastOn = true;
			usingFlickerData = true;
		}
		public static bool TryGetMoonLightSourceData(this LightSource self, out FlickerData data)
		{
			data = null!;
			return usingFlickerData && LightSourceCWT.TryGetValue(self, out data);
		}

		public static void InitMoonLightBeamData(this LightBeam self)
		{
			FlickerData data = LightBeamCWT.GetOrCreateValue(self);
			data.On = true;
			data.LastOn = true;
			usingFlickerData = true;
		}
		public static bool TryGetMoonLightBeamData(this LightBeam self, out FlickerData data)
		{
			data = null!;
			return usingFlickerData && LightBeamCWT.TryGetValue(self, out data);
		}

		public static void InitMoonFlickerableData(this IFlickerable self)
		{
			FlickerData data = FlickerableCWT.GetOrCreateValue(self);
			data.On = true;
			data.LastOn = true;
			usingFlickerData = true;
		}
		public static bool TryGetMoonFlickerableData(this IFlickerable self, out FlickerData data)
		{
			data = null!;
			return usingFlickerData && FlickerableCWT.TryGetValue(self, out data);
		}

		public class RegionData
		{
			public HSLColor CrystalColor;

			public float OESphereHue;

			public bool hideTimer;
		}

		public class FlickerData
		{
			public bool On;
			public bool LastOn;

			public void UpdateOn(bool value)
			{
				LastOn = On;
				On = value;
			}

			public float Alpha(float timeStacker)
			{
				float t;
				if (On && !LastOn)
				{
					t = timeStacker;
				}
				else if (!On && LastOn)
				{
					t = 1f - timeStacker;
				}
				else
				{
					t = On ? 1f : 0f;
				}

				return t;
			}
		}
	}
}
