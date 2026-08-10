#nullable disable

namespace RegionKit.Modules.MoonIO
{
	public class IOLightSource : IOObject
	{
		public LightSource lightSource;

		public bool On;

		public IOLightSource(PlacedObject placedObject, Room room) : base(placedObject)
		{
			this.room = room;

			lightSource = new LightSource(placedObject.pos, true, Color.white, this);
			room.AddObject(lightSource);

			On = (placedObject.data as IOLightSourceType.IOLightSourceData).StartOn;
		}

		public override void ReciveInput(_Enums.InputType type)
		{
			base.ReciveInput(type);

			if (type == _Enums.InputType.Input_On)
			{
				On = true;
				SendOutput(_Enums.OutputType.Output_On);
			}

			if (type == _Enums.InputType.Input_Off)
			{
				On = false;
				SendOutput(_Enums.OutputType.Output_Off);
			}

			if (type == _Enums.InputType.Input_Toggle)
			{
				On = !On;
				SendOutput(_Enums.OutputType.Output_Toggle);
			}
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			lightSource.rad = (placedObject.data as IOLightSourceType.IOLightSourceData).Size.magnitude;

			lightSource.setPos = placedObject.pos;
			lightSource.setRad = (placedObject.data as IOLightSourceType.IOLightSourceData).Size.magnitude;
			lightSource.setAlpha = (placedObject.data as IOLightSourceType.IOLightSourceData).strength * (On ? 1f : 0f);
			lightSource.fadeWithSun = (placedObject.data as IOLightSourceType.IOLightSourceData).fadeWithSun;
			lightSource.colorFromEnvironment = (placedObject.data as IOLightSourceType.IOLightSourceData).colorType == PlacedObject.LightSourceData.ColorType.Environment;
			lightSource.flat = (placedObject.data as IOLightSourceType.IOLightSourceData).flat;
			lightSource.effectColor = Math.Max(-1, (int)(placedObject.data as IOLightSourceType.IOLightSourceData).colorType - 2);

			if ((placedObject.data as IOLightSourceType.IOLightSourceData).colorType == PlacedObject.LightSourceData.ColorType.White)
			{
				lightSource.color = Color.white;
			}

			if ((placedObject.data as IOLightSourceType.IOLightSourceData).blinkType == PlacedObject.LightSourceData.BlinkType.Fade)
			{
				lightSource.setBlinkProperties(PlacedObject.LightSourceData.BlinkType.Fade, (placedObject.data as IOLightSourceType.IOLightSourceData).blinkRate);
			}
			else if ((placedObject.data as IOLightSourceType.IOLightSourceData).blinkType == PlacedObject.LightSourceData.BlinkType.Flash)
			{
				lightSource.setBlinkProperties(PlacedObject.LightSourceData.BlinkType.Flash, (placedObject.data as IOLightSourceType.IOLightSourceData).blinkRate);
			}
			else if ((placedObject.data as IOLightSourceType.IOLightSourceData).blinkType == PlacedObject.LightSourceData.BlinkType.None)
			{
				lightSource.setBlinkProperties(PlacedObject.LightSourceData.BlinkType.None, (placedObject.data as IOLightSourceType.IOLightSourceData).blinkRate);
			}
		}
	}
}
