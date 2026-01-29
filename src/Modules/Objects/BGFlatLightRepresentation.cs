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

			private readonly Cycler modeCycler, cloudCycler;
			private readonly BGFlatLightSlider strengthSlider;
			private BGFlatLightSlider redSlider, greenSlider, blueSlider;

			private BGFlatLight.Mode lastMode;

			public BGFlatLightPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 65f), "BG Flat Light")
			{
				subNodes.Add(modeCycler = new Cycler(owner, "BGFlatLight_Mode_Cycler", this, new Vector2(5f, 45f), 240f, "Color mode: ", BGFlatLight.Mode.values.entries));
				subNodes.Add(cloudCycler = new Cycler(owner, "BGFlatLight_Cloud_Cycler", this, new Vector2(5f, 25f), 240f, "Cloud mode: ", ["NO", "YES"]));
				subNodes.Add(strengthSlider = new BGFlatLightSlider(SliderType.Strength, owner, "BGFlatLight_Slider_Strength", this, new Vector2(5f, 5f), "Strength:"));

				modeCycler.currentAlternative = Math.Max(Data.mode.index, 0);
                modeCycler.Text = modeCycler.baseName + modeCycler.alternatives[modeCycler.currentAlternative];
                cloudCycler.currentAlternative = Data.cloudMode ? 1 : 0;
                cloudCycler.Text = cloudCycler.baseName + modeCycler.alternatives[cloudCycler.currentAlternative];

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
				Data.mode = new BGFlatLight.Mode(BGFlatLight.Mode.values.entries[Mathf.Clamp(modeCycler.currentAlternative, 0, BGFlatLight.Mode.values.Count)], false);
				Data.cloudMode = cloudCycler.currentAlternative == 1;

				// Update sliders
				if (lastMode != BGFlatLight.Mode.CustomColor && Data.mode == BGFlatLight.Mode.CustomColor)
				{
					redSlider = new BGFlatLightSlider(SliderType.Red, owner, "BGFlatLight_Slider_R", this, new Vector2(5f, 105f), "Red:");
					greenSlider = new BGFlatLightSlider(SliderType.Green, owner, "BGFlatLight_Slider_G", this, new Vector2(5f, 85f), "Green:");
					blueSlider = new BGFlatLightSlider(SliderType.Blue, owner, "BGFlatLight_Slider_B", this, new Vector2(5f, 65f), "Blue:");
					subNodes.Add(redSlider);
					subNodes.Add(greenSlider);
					subNodes.Add(blueSlider);
					size = new Vector2(250f, 125f);
				}
				else if (lastMode == BGFlatLight.Mode.CustomColor && Data.mode != BGFlatLight.Mode.CustomColor)
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
				lastMode = Data.mode;

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
