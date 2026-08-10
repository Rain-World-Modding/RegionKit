using System.Text.RegularExpressions;
using DevInterface;

#nullable disable

namespace RegionKit.Modules.IO
{
	public class IOLightSourceType : ManagedObjectType
	{
		public IOLightSourceType() : base("IO LightSource", _Enums.DevObjectCategories.MoonsStuffIO.value, null, typeof(IOLightSourceData), typeof(IOLightSourceRepresentation))
		{
		}

		public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
		{
			return new IOLightSource(placedObject, room);
		}

		public class IOLightSourceData : IOType.IOData
		{
#pragma warning disable 0649
			[BackedByField("Size")]
			public Vector2 Size;
#pragma warning restore 0649

			private static ManagedField[] SizeField = new ManagedField[] {
					new Vector2Field("Size", new Vector2(0, 60), Vector2Field.VectorReprType.circle),
			};

			public bool StartOn;

			public bool flat;

			public bool nightLight;

			public bool fadeWithSun;

			public float strength;

			public float blinkRate;

			public PlacedObject.LightSourceData.ColorType colorType;

			public PlacedObject.LightSourceData.BlinkType blinkType;

			public IOLightSourceData(PlacedObject owner) : base(owner, SizeField)
			{
				StartOn = true;
				flat = false;
				nightLight = false;
				fadeWithSun = false;
				strength = 1.0f;
				blinkRate = 0.0f;
				colorType = PlacedObject.LightSourceData.ColorType.Environment;
				blinkType = PlacedObject.LightSourceData.BlinkType.None;

				NeedsControlPanel = true;
			}

			public override string ToString()
			{
				return
					base.ToString()
					+ "~" + StartOn
					 + "~" + flat
					  + "~" + nightLight
					   + "~" + fadeWithSun
						+ "~" + strength
						 + "~" + blinkRate
						  + "~" + colorType
						   + "~" + blinkType;
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] arr = Regex.Split(s, "~");
				try
				{
					StartOn = bool.Parse(arr[base.FieldsWhenSerialized + 0]);
					flat = bool.Parse(arr[base.FieldsWhenSerialized + 1]);
					nightLight = bool.Parse(arr[base.FieldsWhenSerialized + 2]);
					fadeWithSun = bool.Parse(arr[base.FieldsWhenSerialized + 3]);
					strength = float.Parse(arr[base.FieldsWhenSerialized + 4]);
					blinkRate = float.Parse(arr[base.FieldsWhenSerialized + 5]);
					colorType = ExtEnum<PlacedObject.LightSourceData.ColorType>.Parse(typeof(ExtEnum<PlacedObject.LightSourceData.ColorType>), arr[base.FieldsWhenSerialized + 6], true) as PlacedObject.LightSourceData.ColorType;
					blinkType = ExtEnum<PlacedObject.LightSourceData.BlinkType>.Parse(typeof(ExtEnum<PlacedObject.LightSourceData.BlinkType>), arr[base.FieldsWhenSerialized + 7], true) as PlacedObject.LightSourceData.BlinkType;
				}
				catch { }
			}
		}

		public class IOLightSourceRepresentation : IOType.IORepresentation, IDevUISignals
		{
			public class LightControlSlider : Slider
			{
				public LightControlSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title)
					: base(owner, IDstring, parentNode, pos, title, inheritButton: false, 110f)
				{
				}

				public override void Update()
				{
					base.Update();
					//Debug.Log(((parentNode as IOLightSourceRepresentation).pObj.data as IOLightSourceData).strength);
				}

				public override void Refresh()
				{
					base.Refresh();
					float num = 0f;
					string iDstring = IDstring;
					if (iDstring != null && iDstring == "Strength_Slider")
					{
						num = ((parentNode.parentNode as IOLightSourceRepresentation).pObj.data as IOLightSourceData).strength;
					}

					base.NumberText = (int)(num * 100f) + "%";
					RefreshNubPos(num);
				}

				public override void NubDragged(float nubPos)
				{
					string iDstring = IDstring;
					if (iDstring != null && iDstring == "Strength_Slider")
					{
						((parentNode.parentNode as IOLightSourceRepresentation).pObj.data as IOLightSourceData).strength = nubPos;
					}

					parentNode.parentNode.Refresh();
					Refresh();
				}
			}

			public class RateControlSlider : Slider
			{
				public RateControlSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title)
					: base(owner, IDstring, parentNode, pos, title, inheritButton: false, 110f)
				{
				}

				public override void Refresh()
				{
					base.Refresh();
					float num = 0f;
					if (IDstring == "BlinkRate_Slider")
					{
						num = ((parentNode.parentNode as IOLightSourceRepresentation).pObj.data as IOLightSourceData).blinkRate;
					}

					base.NumberText = (int)(num * 100f) + "%";
					RefreshNubPos(num);
				}

				public override void NubDragged(float nubPos)
				{
					if (IDstring == "BlinkRate_Slider")
					{
						((parentNode.parentNode as IOLightSourceRepresentation).pObj.data as IOLightSourceData).blinkRate = nubPos;
					}

					parentNode.parentNode.Refresh();
					Refresh();
				}
			}

			public Button StartingState;

			public PlacedObject placedObject;

			public IOLightSourceRepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
			{
				panel.size = new Vector2(250f, 125f);
				placedObject = pObj;

				float o = 40f;

				panel.subNodes.Add(new LightControlSlider(owner, "Strength_Slider", this.panel, new Vector2(5f, 65f + o), "Strength: "));
				panel.subNodes.Add(new Button(owner, "Color_Button", this.panel, new Vector2(5f, 45f + o), 110f, (pObj.data as IOLightSourceData).colorType.ToString()));
				panel.subNodes.Add(new Button(owner, "Fade_With_Sun_Button", this.panel, new Vector2(125f, 45f + o), 60f, (pObj.data as IOLightSourceData).fadeWithSun ? "Sun" : "Static"));
				panel.subNodes.Add(new Button(owner, "Flat_Button", this.panel, new Vector2(190f, 45f + o), 55f, (pObj.data as IOLightSourceData).flat ? "Flat: ON" : "Flat: OFF"));
				panel.subNodes.Add(new RateControlSlider(owner, "BlinkRate_Slider", this.panel, new Vector2(5f, 25f + o), "Blink Rate: "));
				panel.subNodes.Add(new Button(owner, "BlinkType_Button", this.panel, new Vector2(5f, 5f + o), 110f, (pObj.data as IOLightSourceData).blinkType.ToString()));
				panel.subNodes.Add(new Button(owner, "NightLight_Button", this.panel, new Vector2(125f, 5f + o), 120f, (pObj.data as IOLightSourceData).nightLight ? "Night Only" : "Always On"));

				panel.subNodes.Add(StartingState = new Button(this.owner, "StartingState", this.panel, new Vector2(5f, 25f), 240, "Start: On"));

				IOButton.size.x = panel.size.x - 10f;
				IOButton.fSprites[0].scaleX = panel.size.x - 10f;

				IOPanel.pos = new Vector2(panel.size.x + 10f, 5f);
			}

			public override void Refresh()
			{
				base.Refresh();
				StartingState.Text = "Start: " + ((placedObject.data as IOLightSourceData).StartOn ? "On" : "Off");
			}

			public override void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				base.Signal(type, sender, message);

				if (sender.IDstring == "StartingState")
				{
					(placedObject.data as IOLightSourceData).StartOn = !(placedObject.data as IOLightSourceData).StartOn;
				}

				IOLightSourceData lightSourceData = placedObject.data as IOLightSourceData;
				switch (sender.IDstring)
				{
					case "Color_Button":
						if ((int)lightSourceData.colorType >= ExtEnum<PlacedObject.LightSourceData.ColorType>.values.Count - 1)
						{
							lightSourceData.colorType = new PlacedObject.LightSourceData.ColorType(ExtEnum<PlacedObject.LightSourceData.ColorType>.values.GetEntry(0));
						}
						else
						{
							lightSourceData.colorType = new PlacedObject.LightSourceData.ColorType(ExtEnum<PlacedObject.LightSourceData.ColorType>.values.GetEntry(lightSourceData.colorType.Index + 1));
						}

						(sender as Button).Text = lightSourceData.colorType.ToString();
						break;
					case "Fade_With_Sun_Button":
						lightSourceData.fadeWithSun = !lightSourceData.fadeWithSun;
						(sender as Button).Text = (lightSourceData.fadeWithSun ? "Sun" : "Static");
						break;
					case "Flat_Button":
						lightSourceData.flat = !lightSourceData.flat;
						(sender as Button).Text = (lightSourceData.flat ? "Flat: ON" : "FLAT: OFF");
						break;
					case "BlinkType_Button":
					{
						int num = lightSourceData.blinkType.Index + 1;
						if (num >= ExtEnum<PlacedObject.LightSourceData.BlinkType>.values.Count)
						{
							num = 0;
						}

						lightSourceData.blinkType = new PlacedObject.LightSourceData.BlinkType(ExtEnum<PlacedObject.LightSourceData.BlinkType>.values.GetEntry(num));
						(sender as Button).Text = lightSourceData.blinkType.ToString();
						break;
					}
					case "NightLight_Button":
						lightSourceData.nightLight = !lightSourceData.nightLight;
						(sender as Button).Text = ((!lightSourceData.nightLight) ? "Always On" : "Night Only");
						break;
				}
			}
		}
	}
}
