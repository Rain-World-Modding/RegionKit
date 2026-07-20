
using DevInterface;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.BackgroundBuilder;

public class DayNightSceneSettings : PositionedDevUINode, IDevUISignals
{
	public const int rows = 3;
	public DayNightSceneSettings(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
	{
		if (RoomSettings.BackgroundData().sceneData is not Data.DayNightSceneData data)
		{
			return;
		}

		Vector2 ppos = new Vector2(0f, (rows - 1) * 20);

		subNodes.Add(new TimeOfDaySettings(owner, "Day", this, ppos, "day", data.daySky, data.atmosphereColor, data.multiplyColor));
		ppos.y -= 20f;
		subNodes.Add(new TimeOfDaySettings(owner, "Dusk", this, ppos, "dusk", data.duskSky, data.duskAtmosphereColor, data.duskMultiplyColor));
		ppos.y -= 20f;
		subNodes.Add(new TimeOfDaySettings(owner, "Night", this, ppos, "night", data.nightSky, data.nightAtmosphereColor, data.nightMultiplyColor));
	}

	public class TimeOfDaySettings : PositionedDevUINode
	{
		public TimeOfDaySettings(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string name, string defaultValue, Color atmo, Color multiply) : base(owner, IDstring, parentNode, pos)
		{
			Vector2 ppos = new Vector2(0f, 0f);
			subNodes.Add(new DevUILabel(owner, name + "Label", this, ppos, 40f, name));
			ppos.x += 44f;
			subNodes.Add(new BackgroundPage.ElementAssetSelectButton(owner, name + "Image", this, ppos, 136f, defaultValue, name + " image", BackgroundPage.ElementAssetSelectPanel.SelectGroup.Illustrations));
			ppos.x += 140f;
			subNodes.Add(new RGBSelectButton(owner, name + "Atmo", this, ppos, 60f, "", atmo, name + " Atmosphere Color"));
			ppos.x += 64f;
			subNodes.Add(new RGBSelectButton(owner, name + "Mult", this, ppos, 60f, "", multiply, name + " Multiply Color"));
		}
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (RoomSettings.BackgroundData().sceneData is not Data.DayNightSceneData data)
		{ return; }

		if (sender is BackgroundPage.ElementAssetSelectButton button)
		{
			string asset = button.actualValue;
			if (button.parentNode.IDstring == "Day")
				data.daySky = asset;
			if (button.parentNode.IDstring == "Dusk")
				data.duskSky = asset;
			if (button.parentNode.IDstring == "Night")
				data.nightSky = asset;
		}

		if (sender is RGBSelectButton rgbButton)
		{
			Color color = rgbButton.actualValue;
			if (rgbButton.IDstring.EndsWith("Atmo"))
			{

				if (rgbButton.parentNode.IDstring == "Day")
					data.atmosphereColor = color;
				if (rgbButton.parentNode.IDstring == "Dusk")
					data.duskAtmosphereColor = color;
				if (rgbButton.parentNode.IDstring == "Night")
					data.nightAtmosphereColor = color;
			}
			if (rgbButton.IDstring.EndsWith("Mult"))
			{

				if (rgbButton.parentNode.IDstring == "Day")
					data.multiplyColor = color;
				if (rgbButton.parentNode.IDstring == "Dusk")
					data.duskMultiplyColor = color;
				if (rgbButton.parentNode.IDstring == "Night")
					data.nightMultiplyColor = color;
			}
		}

		//RoomSettings.BackgroundData().sceneData = data;
	}
}

public class AboveCloudsUINode : Panel, IDevUISignals
{
	public const int shortRows = 13;
	public const int fullRows = 20;
	public AboveCloudsUINode(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(316f, shortRows * 20 + 10), "Above Clouds View Settings")
	{
		MakeSubnodes(false);
	}

	public void MakeSubnodes(bool fullList)
	{
		foreach (var s in subNodes)
		{
			s.ClearSprites();
		}
		subNodes.Clear();

		if (RoomSettings.BackgroundData().sceneData is not Data.AboveCloudsView_SceneData data)
		{
			Custom.LogWarning("isn't acv");
			return;
		}

		var rows = fullList ? fullRows : shortRows;

		this.size.y = rows * 20 + 10;
		this.Refresh();

		Vector2 ppos = new Vector2(5f, (rows - 1) * 20 + 5);

		if (fullList)
		{
			subNodes.Add(new GenericSlider(owner, "windDir", this, ppos, "Wind Dir", true, 100f, data.windDir, stringWidth: 32) { defaultValue = 1f, minValue = -1f, maxValue = 1f });
			ppos.y -= 20f;
		}

		subNodes.Add(new GenericSlider(owner, "startAltitude", this, ppos, "startAltitude", true, 100f, data.startAltitude / 20f, stringWidth: 32) { defaultValue = 2000, minValue = 0, maxValue = 5000 });
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "endAltitude", this, ppos, "endAltitude", true, 100f, data.endAltitude / 20f, stringWidth: 32) { defaultValue = 3140, minValue = 0, maxValue = 5000 });
		ppos.y -= 20f;

		subNodes.Add(new DevUILabel(owner, "daylabel", this, ppos, 110f, "Day-Night Settings"));
		ppos.y -= 20f * (DayNightSceneSettings.rows);
		subNodes.Add(new DayNightSceneSettings(owner, "daynightnode", this, ppos));
		ppos.y -= 20f;
		subNodes.Add(new DevUILabel(owner, "cloudLabel", this, ppos, 110f, "Cloud Settings"));
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "cloudsStartDepth", this, ppos, "StartDepth", true, 100f, data.cloudsStartDepth, stringWidth: 32) { defaultValue = 5, minValue = 0, maxValue = 200 });
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "cloudsEndDepth", this, ppos, "EndDepth", true, 100f, data.cloudsEndDepth, stringWidth: 32) { defaultValue = 40, minValue = 0, maxValue = 200 });
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "distantCloudsEndDepth", this, ppos, "DistantEndDepth", true, 100f, data.distantCloudsEndDepth, stringWidth: 32) { defaultValue = 200, minValue = 0, maxValue = 1000 });
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "closeCloudsCount", this, ppos, "CloseCount", true, 100f, data.cloudsCount, stringWidth: 32) { defaultValue = 7, minValue = 0, maxValue = 20 });
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "distantCloudsCount", this, ppos, "DistantCount", true, 100f, data.distantCloudsCount, stringWidth: 32) { defaultValue = 11, minValue = 0, maxValue = 50 });
		ppos.y -= 20f;

		if (fullList)
		{
			subNodes.Add(new GenericSlider(owner, "overrideYStart", this, ppos, "overrideYStart", true, 100f, data.overrideYStart, stringWidth: 32) { defaultValue = 0, minValue = -5000, maxValue = 5000 });
			ppos.y -= 20f;
			subNodes.Add(new GenericSlider(owner, "overrideYEnd", this, ppos, "overrideYEnd", true, 100f, data.overrideYEnd, stringWidth: 32) { defaultValue = 0, minValue = -5000, maxValue = 5000 });
			ppos.y -= 20f;

			subNodes.Add(new GenericSlider(owner, "curveDepth", this, ppos, "curveDepth", true, 100f, data.curveCloudDepth, stringWidth: 32) { defaultValue = 1, minValue = 0, maxValue = 50, displayRounding = 2 });
			ppos.y -= 20f;

			subNodes.Add(new DevUILabel(owner, "cloudLabel", this, ppos, 110f, "Fog Settings"));
			ppos.y -= 20f;
			subNodes.Add(new GenericSlider(owner, "fogStartAltitude", this, ppos, "startAltitude", true, 100f, data.startFogAltitude / 20f, stringWidth: 32) { defaultValue = 2000, minValue = 0, maxValue = 5000 });
			ppos.y -= 20f;
			subNodes.Add(new GenericSlider(owner, "fogEndAltitude", this, ppos, "endAltitude", true, 100f, data.endFogAltitude / 20f, stringWidth: 32) { defaultValue = 3140, minValue = 0, maxValue = 5000 });
			ppos.y -= 20f;

		}

		subNodes.Add(new Button(owner, "resize", this, ppos, 110f, fullList ? "Less Settings" : "More Settings"));
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (type == DevUISignalType.ButtonClick && sender.IDstring == "resize" && sender is Button button)
		{
			MakeSubnodes(button.Text == "More Settings");
			return;
		}

		if (RoomSettings.BackgroundData().sceneData is not Data.AboveCloudsView_SceneData data)
		{ return; }

		if (type == GenericSlider.SliderUpdate)
		{
			if (sender is GenericSlider slider)
			{
				switch (sender.IDstring)
				{
					case "windDir":
						data.windDir = slider.actualValue;
						break;

					case "startAltitude":
						data.startAltitude = slider.actualValue * 20f;
						break;

					case "endAltitude":
						data.endAltitude = slider.actualValue * 20f;
						break;

					case "cloudsStartDepth":
						data.cloudsStartDepth = (int)slider.actualValue;
						break;

					case "cloudsEndDepth":
						data.cloudsEndDepth = (int)slider.actualValue;
						break;

					case "distantCloudsEndDepth":
						data.distantCloudsEndDepth = (int)slider.actualValue;
						break;

					case "closeCloudsCount":
						data.cloudsCount = (int)slider.actualValue;
						break;

					case "distantCloudsCount":
						data.distantCloudsCount = (int)slider.actualValue;
						break;

					case "overrideYStart":
						data.overrideYStart = (int)slider.actualValue;
						break;

					case "overrideYEnd":
						data.overrideYEnd = (int)slider.actualValue;
						break;

					case "curveDepth":
						data.curveCloudDepth = (int)slider.actualValue;
						break;

					case "fogAltitudeStart":
						data.startFogAltitude = (int)slider.actualValue * 20f;
						break;

					case "fogAltitudeEnd":
						data.endFogAltitude = (int)slider.actualValue * 20f;
						break;
				}
			}
		}


		if (data.redoClouds && data.Scene != null)
		{
			data.RedoClouds(data.Scene);
			data.redoClouds = false;
		}

		//RoomSettings.BackgroundData().sceneData = data;
	}
}

public class RoofTopUINode : Panel, IDevUISignals
{
	public const int shortRows = 13;
	public RoofTopUINode(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(316f, shortRows * 20 + 10), "Rooftop View Settings")
	{
		MakeSubnodes();
	}

	Button centerButton;

	public void MakeSubnodes()
	{
		foreach (var s in subNodes)
		{
			s.ClearSprites();
		}
		subNodes.Clear();

		if (RoomSettings.BackgroundData().sceneData is not Data.RoofTopView_SceneData data)
		{
			Custom.LogWarning("isn't rtv");
			return;
		}

		var rows = shortRows;

		this.size.y = rows * 20 + 10;
		this.Refresh();

		Vector2 ppos = new Vector2(5f, (rows - 1) * 20 + 5);

		subNodes.Add(centerButton = new Button(owner, "center", this, ppos, 110f, CenterButtonText(data)));
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "XOrigin", this, ppos, "XOrigin", true, 100f, (data.origin?.x / 20f) ?? 0f, stringWidth: 32) { minValue = -5000, maxValue = 5000, defaultValue = 0f });

		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "YOrigin", this, ppos, "YOrigin", true, 100f, (data.origin?.y / 20f) ?? 0f, stringWidth: 32) { minValue = -5000, maxValue = 5000, defaultValue = 0f });
		ppos.y -= 20f;

		subNodes.Add(new GenericSlider(owner, "floorLevel", this, ppos, "floorLevel", true, 100f, data.floorLevel, stringWidth: 32) { defaultValue = 1f, minValue = -1f, maxValue = 1f });
		ppos.y -= 20f;

		subNodes.Add(new DevUILabel(owner, "daylabel", this, ppos, 110f, "Day-Night Settings"));
		ppos.y -= 20f * (DayNightSceneSettings.rows);
		subNodes.Add(new DayNightSceneSettings(owner, "daynightnode", this, ppos));
		ppos.y -= 20f;

		subNodes.Add(new DevUILabel(owner, "rubbleLabel", this, ppos, 110f, "Rubble Settings"));
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "rubbleCount", this, ppos, "Count", true, 100f, data.rubbleCount, stringWidth: 32) { defaultValue = 16, minValue = 0, maxValue = 100, valueRounding = 0 });
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "rubbleStartDepth", this, ppos, "startDepth", true, 100f, data.rubbleStartDepth, stringWidth: 32) { defaultValue = 1.5f, minValue = 0, maxValue = 200 });
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "rubbleEndDepth", this, ppos, "endDepth", true, 100f, data.rubbleEndDepth, stringWidth: 32) { defaultValue = 8f, minValue = 0, maxValue = 200 });
		ppos.y -= 20f;
		subNodes.Add(new GenericSlider(owner, "rubbleCurveDepth", this, ppos, "curveDepth", true, 100f, data.curveRubbleDepth, stringWidth: 32) { defaultValue = 1.5f, minValue = 0, maxValue = 50, displayRounding = 2 });
		ppos.y -= 20f;
	}

	string CenterButtonText(Data.RoofTopView_SceneData sceneData) => sceneData.origin == null ? "Fixed Camera" : "Free Camera";

	public void ToggleOrigin()
	{
		if (RoomSettings.BackgroundData().sceneData is not Data.RoofTopView_SceneData data)
		{ return; }

		if (data.origin == null)
			data.origin = new Vector2();
		else
			data.origin = null;

		centerButton.Text = CenterButtonText(data);
		centerButton.Refresh();
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (RoomSettings.BackgroundData().sceneData is not Data.RoofTopView_SceneData data)
		{ return; }

		if (sender == centerButton)
		{
			if (data.origin == null)
				data.origin = new Vector2();
			else
				data.origin = null;

			centerButton.Text = CenterButtonText(data);
			centerButton.Refresh();
		}

		if (type == GenericSlider.SliderUpdate)
		{
			if (sender is GenericSlider slider)
			{
				switch (sender.IDstring)
				{
					case "XOrigin":
						if (data.origin == null)
							ToggleOrigin();

						data.origin = new Vector2(slider.actualValue * 20f, data.origin!.Value.y);
						break;

					case "YOrigin":
						if (data.origin == null)
							ToggleOrigin();

						data.origin = new Vector2(data.origin!.Value.x, slider.actualValue * 20f);
						break;

					case "floorLevel":
						data.floorLevel = slider.actualValue;
						break;

					case "rubbleCount":
						data.rubbleCount = (int)slider.actualValue;
						break;

					case "rubbleStartDepth":
						data.rubbleStartDepth = (int)slider.actualValue;
						break;

					case "rubbleEndDepth":
						data.rubbleEndDepth = (int)slider.actualValue;
						break;

					case "rubbleCurveDepth":
						data.curveRubbleDepth = (int)slider.actualValue;
						break;
				}
			}
		}


		if (data.redoRubble && data.Scene != null)
		{
			data.RedoRubble(data.Scene);
			data.redoRubble = false;
		}

		//RoomSettings.BackgroundData().sceneData = data;
	}
}


