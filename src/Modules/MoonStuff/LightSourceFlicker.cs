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
		public LightSourceFlickerData.SunlightType Type => (placedObject.data as LightSourceFlickerData).Type;
		public LightSourceFlickerData.LightSourceType Type2 => (placedObject.data as LightSourceFlickerData).Type2;
		public bool Synced => (placedObject.data as LightSourceFlickerData).Synced;
		public bool LastSync;

		public List<LightSource> FlickerLights = new List<LightSource>();
		public List<LightBeam> FlickerLightBeams = new List<LightBeam>();
		public List<IFlickerable> Flickerables = new List<IFlickerable>();

		private bool hasCheckedForLights = false;

		public int FlickerCountdown;
		public LightSourceFlicker(PlacedObject placedObject, Room room) : base()
		{
			this.placedObject = placedObject;
		}

		public void CheckForLights()
		{
			FlickerLights.Clear();
			FlickerLightBeams.Clear();
			Flickerables.Clear();

			List<LightSource> allLights = [];
			foreach (UpdatableAndDeletable uad in room.updateList)
			{
				if (uad is LightSource lightSource)
				{
					allLights.Add(lightSource);
					lightSource.InitMoonLightSourceData();
				}
				else if (uad is LightBeam lightBeam)
				{
					FlickerLightBeams.Add(lightBeam);
					lightBeam.InitMoonLightBeamData();
				}
				else if (uad is IFlickerable flickerable)
				{
					Flickerables.Add(flickerable);
					flickerable.InitMoonFlickerableData();
				}
			}

			foreach (PlacedObject pObj in room.roomSettings.placedObjects)
			{
				if (ValidLightSourceObject(pObj.type))
				{
					foreach (LightSource light in allLights)
					{
						if (light.pos == pObj.pos)
						{
							FlickerLights.Add(light);
							break;
						}
					}
				}
			}

			static bool ValidLightSourceObject(PlacedObject.Type type)
			{
				return type == PlacedObject.Type.LightSource
					|| type == PlacedObject.Type.LightFixture
					|| type == Objects._Enums.ColouredLightSource;
			}
		}

		public void UpdateLights(bool doFlicker)
		{
			bool syncedFlicker = Random.value < Chance;
			foreach (LightSource lightSource in FlickerLights)
			{
				if (OperateOnLightSource(lightSource) && lightSource.TryGetMoonLightSourceData(out _CWTs.FlickerData flickerData))
				{
					flickerData.UpdateOn(!doFlicker || (Synced ? syncedFlicker : Random.value >= Chance));
				}
			}
			foreach (LightBeam lightBeam in FlickerLightBeams)
			{
				if (OperateOnLightBeam(lightBeam) && lightBeam.TryGetMoonLightBeamData(out _CWTs.FlickerData flickerData))
				{
					flickerData.UpdateOn(!doFlicker || (Synced ? syncedFlicker : Random.value >= Chance));
				}
			}
			foreach (IFlickerable flickerable in Flickerables)
			{
				if (OperateOnFlickerable(flickerable) && flickerable.TryGetMoonFlickerableData(out _CWTs.FlickerData flickerData))
				{
					flickerData.UpdateOn(!doFlicker || (Synced ? syncedFlicker : Random.value >= Chance));
				}
			}

			bool OperateOnLightSource(LightSource lightSource)
			{
				return (!Local || Vector2.Distance(lightSource.pos, placedObject.pos) < Rad)
					&& (lightSource.fadeWithSun == (Type == LightSourceFlickerData.SunlightType.Sun) || Type == LightSourceFlickerData.SunlightType.All)
					&& (lightSource.flat == (Type2 == LightSourceFlickerData.LightSourceType.Flat) || Type2 == LightSourceFlickerData.LightSourceType.All);
			}

			bool OperateOnLightBeam(LightBeam lightBeam)
			{
				var lightBeamData = (lightBeam.placedObject.data as LightBeam.LightBeamData)!;
				return (!Local || Vector2.Distance(lightBeam.placedObject.pos, placedObject.pos) < Rad)
					&& (lightBeamData.sun == (Type == LightSourceFlickerData.SunlightType.Sun) || Type == LightSourceFlickerData.SunlightType.All);
			}

			bool OperateOnFlickerable(IFlickerable flickerable)
			{
				return (!Local || Vector2.Distance(flickerable.CheckPosition, placedObject.pos) < Rad)
					&& (flickerable.SunlightType == Type || Type == LightSourceFlickerData.SunlightType.All || flickerable.SunlightType == LightSourceFlickerData.SunlightType.All)
					&& (flickerable.LightSourceType == Type2 || Type2 == LightSourceFlickerData.LightSourceType.All || flickerable.LightSourceType == LightSourceFlickerData.LightSourceType.All);
			}
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			// Check if needed
			if (!hasCheckedForLights)
			{
				hasCheckedForLights = true;
				CheckForLights();
			}

			// Flicker
			bool flicker = false;
			if (FlickerCountdown > 0)
			{
				FlickerCountdown--;
			}
			else
			{
				flicker = true;
				FlickerCountdown = Random.Range(MinFrequency, MaxFrequency);
			}

			UpdateLights(flicker);

			// Sync
			LastSync = Synced;
		}
	}
}
