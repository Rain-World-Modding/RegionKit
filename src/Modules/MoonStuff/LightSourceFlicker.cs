using RegionKit.Modules.MoonIO;
using static RegionKit.Modules.MoonStuff.LightSourceFlickerType;
using Random = UnityEngine.Random;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class LightSourceFlicker : UpdatableAndDeletable
	{
		public PlacedObject placedObject;
		public float Chance => (placedObject.data as LightSourceFlickerData).Chance;
		public int MinFrequency => (int)((placedObject.data as LightSourceFlickerData).FrequencyMin * 100);
		public int MaxFrequency => (int)((placedObject.data as LightSourceFlickerData).FrequencyMax * 100);
		public float Rad => (placedObject.data as LightSourceFlickerData).Rad.magnitude;
		public bool Local => (placedObject.data as LightSourceFlickerData).Local;
		public int Type => (placedObject.data as LightSourceFlickerData).Type;
		public int Type2 => (placedObject.data as LightSourceFlickerData).Type2;
		public bool Synced => (placedObject.data as LightSourceFlickerData).Synced;
		public bool LastSync;

		public List<LightSource> FlickerLights = new List<LightSource>();
		public List<SpotLight> FlickerSpotLights = new List<SpotLight>();
		public List<LightBeam> FlickerLightBeams = new List<LightBeam>();

		public int FlickerCountdown;
		public LightSourceFlicker(PlacedObject placedObject, Room room) : base()
		{
			this.placedObject = placedObject;
		}

		public void Flicker(int t, int i)
		{
			if (t == 0)
			{
				_CWTs.MoonLightSourceData(FlickerLights[i]).On = true;
				FlickerLights.RemoveAt(i);
			}
			else if (t == 1)
			{
				_CWTs.MoonSpotLightData(FlickerSpotLights[i]).On = true;
				FlickerSpotLights.RemoveAt(i);
			}
			else if (t == 2)
			{
				_CWTs.MoonLightBeamData(FlickerLightBeams[i]).On = true;
				FlickerLightBeams.RemoveAt(i);
			}
		}

		public void UpdateLights()
		{
			for (int i = 0; i < FlickerLights.Count; i++)
			{
				if (Synced != LastSync || (Local && !Custom.DistLess(placedObject.pos, FlickerLights[i].pos, Rad)))
				{
					_CWTs.MoonLightSourceData(FlickerLights[i]).On = true;
					FlickerLights.RemoveAt(i);
				}
			}

			for (int i = 0; i < FlickerSpotLights.Count; i++)
			{
				if (Synced != LastSync || (Local && !Custom.DistLess(placedObject.pos, FlickerSpotLights[i].placedObject.pos, Rad)))
				{
					_CWTs.MoonSpotLightData(FlickerSpotLights[i]).On = true;
					FlickerSpotLights.RemoveAt(i);
				}
			}

			for (int i = 0; i < FlickerLightBeams.Count; i++)
			{
				if (Synced != LastSync || (Local && !Custom.DistLess(placedObject.pos, FlickerLightBeams[i].placedObject.pos, Rad)))
				{
					_CWTs.MoonLightBeamData(FlickerLightBeams[i]).On = true;
					FlickerLightBeams.RemoveAt(i);
				}
			}
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			UpdateLights();

			if (FlickerCountdown > 0)
			{
				FlickerCountdown--;
				return;
			}

			FlickerCountdown = Random.Range(MinFrequency, MaxFrequency);

			if (!Synced || Random.value >= Chance)
			{

				for (int l = 0; l < room.lightSources.Count; l++)
				{
					if (!Local || Custom.DistLess(placedObject.pos, room.lightSources[l].pos, Rad))
					{
						if (Type2 == 2 || (Type2 == 0 && !room.lightSources[l].flat) || (Type2 == 1 && room.lightSources[l].flat))
						{
							if (Type == 2 || (Type == 0 && !room.lightSources[l].fadeWithSun) || (Type == 1 && room.lightSources[l].fadeWithSun))
							{
								if (Synced || Random.value < Chance)
								{
									if (FlickerLights.Count > 0 && FlickerLights.Contains(room.lightSources[l]))
									{
										Flicker(0, FlickerLights.IndexOf(room.lightSources[l]));
									}
									else
									{
										_CWTs.MoonLightSourceData(room.lightSources[l]).On = false;
										FlickerLights.Add(room.lightSources[l]);
									}
								}
							}
						}
					}
				}

				for (int l = 0; l < room.cosmeticLightSources.Count; l++)
				{
					if (!Local || Custom.DistLess(placedObject.pos, room.cosmeticLightSources[l].pos, Rad))
					{
						if (Type2 == 2 || (Type2 == 0 && !room.cosmeticLightSources[l].flat) || (Type2 == 1 && room.cosmeticLightSources[l].flat))
						{
							if (Type == 2 || (Type == 0 && !room.cosmeticLightSources[l].fadeWithSun) || (Type == 1 && room.cosmeticLightSources[l].fadeWithSun))
							{
								if (Synced || Random.value < Chance)
								{
									if (FlickerLights.Count > 0 && FlickerLights.Contains(room.cosmeticLightSources[l]))
									{
										Flicker(0, FlickerLights.IndexOf(room.cosmeticLightSources[l]));
									}
									else
									{
										_CWTs.MoonLightSourceData(room.cosmeticLightSources[l]).On = false;
										FlickerLights.Add(room.cosmeticLightSources[l]);
									}
								}
							}
						}
					}
				}

				for (int l = 0; l < room.drawableObjects.Count; l++)
				{
					if (room.drawableObjects[l] is SpotLight light && (!Local || Custom.DistLess(placedObject.pos, light.placedObject.pos, Rad)))
					{
						if (Synced || Random.value < Chance)
						{
							if (FlickerSpotLights.Count > 0 && FlickerSpotLights.Contains(light))
							{
								Flicker(1, FlickerSpotLights.IndexOf(light));
							}
							else
							{
								_CWTs.MoonSpotLightData(light).On = false;
								FlickerSpotLights.Add(light);
							}
						}
					}
					else if (room.drawableObjects[l] is LightBeam lightbeam && (!Local || Custom.DistLess(placedObject.pos, lightbeam.placedObject.pos, Rad)))
					{
						if (Type == 2 || (Type == 0 && !(lightbeam.placedObject.data as LightBeam.LightBeamData).sun) || (Type == 1 && (lightbeam.placedObject.data as LightBeam.LightBeamData).sun))
						{
							if (Synced || Random.value < Chance)
							{
								if (FlickerLightBeams.Count > 0 && FlickerLightBeams.Contains(lightbeam))
								{
									Flicker(2, FlickerLightBeams.IndexOf(lightbeam));
								}
								else
								{
									_CWTs.MoonLightBeamData(lightbeam).On = false;
									FlickerLightBeams.Add(lightbeam);
								}
							}
						}
					}
				}
			}

			LastSync = Synced;
		}
	}
}
