using DevInterface;

namespace RegionKit.Modules.Objects
{
	internal class BGFlatLightRepresentation : ResizeableObjectRepresentation
	{
		public BGFlatLight.Data Data => (pObj.data as BGFlatLight.Data)!;

		private readonly BGFlatLightPanel panel;
		private readonly FSprite panelLine;

		public BGFlatLightRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, "BG Flat Light", true)
		{
			fSprites.Add(panelLine = new FSprite("pixel") { anchorY = 0f });
			subNodes.Add(panel = new BGFlatLightPanel(owner, IDstring, this, Data.panelPos));
			owner.placedObjectsContainer.AddChild(panelLine);


			var light = owner.room.updateList.FirstOrDefault(x => x is BGFlatLight light && light.pObj.pos == pObj.pos);
			if (light == null)
			{
				owner.room.AddObject(new BGFlatLight(pObj));
			}
		}

		public override void Refresh()
		{
			base.Refresh();

			Data.panelPos = panel.pos;

			panelLine.SetPosition(absPos + new Vector2(0.01f, 0.01f));
			panelLine.scaleY = Data.panelPos.magnitude;
			panelLine.rotation = Custom.AimFromOneVectorToAnother(absPos, panel.absPos);
		}

		private class BGFlatLightPanel : Panel, IDevUISignals
		{
			public BGFlatLightRepresentation Rep => (parentNode as BGFlatLightRepresentation)!;
			public BGFlatLight.Data Data => (Rep.pObj.data as BGFlatLight.Data)!;

			private readonly Cycler modeCycler, displayCycler;
			private readonly BGFlatLightSlider strengthSlider;
			private BGFlatLightSlider redSlider, greenSlider, blueSlider;

			private BGFlatLight.ColorMode lastMode;

			public BGFlatLightPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 65f), "BG Flat Light")
			{
				subNodes.Add(modeCycler = new Cycler(owner, "BGFlatLight_Mode_Cycler", this, new Vector2(5f, 45f), 240f, "Color mode: ", BGFlatLight.ColorMode.values.entries));
				subNodes.Add(displayCycler = new Cycler(owner, "BGFlatLight_Display_Cycler", this, new Vector2(5f, 25f), 240f, "Display mode: ", BGFlatLight.DisplayMode.values.entries));
				subNodes.Add(strengthSlider = new BGFlatLightSlider(SliderType.Strength, owner, "BGFlatLight_Slider_Strength", this, new Vector2(5f, 5f), "Strength:"));

				modeCycler.currentAlternative = Math.Max(Data.colorMode.index, 0);
                modeCycler.Text = modeCycler.baseName + modeCycler.alternatives[modeCycler.currentAlternative];
                displayCycler.currentAlternative = Math.Max(Data.displayMode.index, 0);
                displayCycler.Text = displayCycler.baseName + displayCycler.alternatives[displayCycler.currentAlternative];

				// These get added in Refresh
				redSlider = null!;
				greenSlider = null!;
				blueSlider = null!;
				lastMode = null!;

				Refresh();
			}

			public override void Refresh()
			{
				// Update data
				Data.colorMode = new BGFlatLight.ColorMode(BGFlatLight.ColorMode.values.entries[Mathf.Clamp(modeCycler.currentAlternative, 0, BGFlatLight.ColorMode.values.Count)], false);
				Data.displayMode = new BGFlatLight.DisplayMode(BGFlatLight.DisplayMode.values.entries[Mathf.Clamp(displayCycler.currentAlternative, 0, BGFlatLight.DisplayMode.values.Count)], false);

				// Update sliders
				if (lastMode != BGFlatLight.ColorMode.CustomColor && Data.colorMode == BGFlatLight.ColorMode.CustomColor)
				{
					redSlider = new BGFlatLightSlider(SliderType.Red, owner, "BGFlatLight_Slider_R", this, new Vector2(5f, 105f), "Red:");
					greenSlider = new BGFlatLightSlider(SliderType.Green, owner, "BGFlatLight_Slider_G", this, new Vector2(5f, 85f), "Green:");
					blueSlider = new BGFlatLightSlider(SliderType.Blue, owner, "BGFlatLight_Slider_B", this, new Vector2(5f, 65f), "Blue:");
					subNodes.Add(redSlider);
					subNodes.Add(greenSlider);
					subNodes.Add(blueSlider);
					size = new Vector2(250f, 125f);
				}
				else if (lastMode == BGFlatLight.ColorMode.CustomColor && Data.colorMode != BGFlatLight.ColorMode.CustomColor)
				{
					redSlider.ClearSprites();
					greenSlider.ClearSprites();
					blueSlider.ClearSprites();
					subNodes.Remove(redSlider);
					subNodes.Remove(greenSlider);
					subNodes.Remove(blueSlider);
					redSlider = null!;
					greenSlider = null!;
					blueSlider = null!;
					size = new Vector2(250f, 65f);
				}
				lastMode = Data.colorMode;

				// Need to refresh after in case of update
				base.Refresh();
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				Refresh();
			}

			private class BGFlatLightSlider : Slider
			{
				private readonly SliderType type;

				private BGFlatLight.Data Data => (parentNode as BGFlatLightPanel)!.Data;
				private BGFlatLightRepresentation Rep => (parentNode as BGFlatLightPanel)!.Rep;

				public BGFlatLightSlider(SliderType type, DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title, false, 110f)
				{
					this.type = type;
				}

				public override void Refresh()
				{
					base.Refresh();

					float num = type switch
					{
						SliderType.Strength => Data.strength,
						SliderType.Red => Data.r,
						SliderType.Green => Data.g,
						SliderType.Blue => Data.b,
						_ => throw new NotImplementedException(),
					};
					NumberText = num.ToString("0.000");
					RefreshNubPos(num);
				}

				public override void NubDragged(float nubPos)
				{
					switch (type)
					{
					case SliderType.Strength: Data.strength = nubPos; break;
					case SliderType.Red: Data.r = nubPos; break;
					case SliderType.Green: Data.g = nubPos; break;
					case SliderType.Blue: Data.b = nubPos; break;
					}

					Rep.Refresh();
					Refresh();
				}
			}

			private enum SliderType
			{
				Strength,
				Red,
				Green,
				Blue
			}
		}
	}
}
