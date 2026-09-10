using System.Text.RegularExpressions;
using DevInterface;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class LightSourceFlickerType : ManagedObjectType
	{
		public LightSourceFlickerType() : base(_Enums.MoonLightSourceFlicker, Objects._Enums.DecorationsCategory, null, typeof(LightSourceFlickerData), typeof(LightSourceFlickerRepresentation))
		{

		}

		public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
		{
			return new LightSourceFlicker(placedObject, room);
		}

		public class LightSourceFlickerData : ManagedData
		{
#pragma warning disable 0649
			[FloatField("Chance", 0f, 1f, 0.5f, 0.05f, displayName: "Chance:")]
			public float Chance;

			[FloatField("FrequencyMin", 0f, 1f, 0.5f, 0.01f, displayName: "Min Frequency:")]
			public float FrequencyMin;

			[FloatField("FrequencyMax", 0f, 1f, 0.5f, 0.01f, displayName: "Max Frequency:")]
			public float FrequencyMax;

#pragma warning restore 0649

			public bool Local;

			public SunlightType Type;

			public LightSourceType Type2;

			public Vector2 Rad;

			public bool Synced;

			public LightSourceFlickerData(PlacedObject owner) : base(owner, null)
			{
				this.Local = false;
				this.Type = 0;
				this.Type2 = 0;
				this.Rad = new Vector2(0f, 10f);
				this.Synced = false;
			}

			public override string ToString()
			{
				return base.ToString() + "~" + Local + "~" + (int)Type + "~" + (int)Type2 + "~" + Rad.x + "~" + Rad.y + "~" + Synced;
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] arr = Regex.Split(s, "~");
				try
				{
					Local = bool.Parse(arr[base.FieldsWhenSerialized + 0]);
					Type = (SunlightType)int.Parse(arr[base.FieldsWhenSerialized + 1]);
					Type2 = (LightSourceType)int.Parse(arr[base.FieldsWhenSerialized + 2]);
					Rad.x = float.Parse(arr[base.FieldsWhenSerialized + 3]);
					Rad.y = float.Parse(arr[base.FieldsWhenSerialized + 4]);
					Synced = bool.Parse(arr[base.FieldsWhenSerialized + 5]);
				}
				catch { }
			}

			public enum SunlightType
			{
				Static,
				Sun,
				All
			}

			public enum LightSourceType
			{
				Normal,
				Flat,
				All
			}
		}
		public class LightSourceFlickerRepresentation : ManagedRepresentation, IDevUISignals
		{
			public class FlickerHandle : Handle
			{
				public FSprite Line;
				public FSprite Circle;

				public bool hidden;
				public FlickerHandle(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
				{
					hidden = !((parentNode as LightSourceFlickerRepresentation).pObj.data as LightSourceFlickerData).Local;

					fSprites.Add(Line = new FSprite("pixel"));
					owner.placedObjectsContainer.AddChild(Line);
					Line.anchorY = 0f;

					fSprites.Add(Circle = new FSprite("Futile_White"));
					owner.placedObjectsContainer.AddChild(Circle);
					Circle.shader = owner.room.game.rainWorld.Shaders["VectorCircle"];

				}

				public override void Update()
				{
					if (!hidden)
					{
						base.Update();
					}
					else if (owner != null && dragged)
					{
						dragged = false;
					}
				}

				public override void Move(Vector2 newPos)
				{
					if (!hidden)
					{
						base.Move(newPos);
						((parentNode as LightSourceFlickerRepresentation).pObj.data as LightSourceFlickerData).Rad = newPos;
					}
				}

				public override void Refresh()
				{
					if (!hidden)
					{
						base.Refresh();

						MoveSprite(fSprites.IndexOf(Line), absPos);
						Line.scaleY = pos.magnitude;
						Line.rotation = Custom.VecToDeg(-pos);

						MoveSprite(fSprites.IndexOf(Circle), (parentNode as DevInterface.PositionedDevUINode).absPos);
						Circle.scale = pos.magnitude / 8f;
						Circle.alpha = 2f / pos.magnitude;
					}

					for (int i = 0; i < fSprites.Count; i++)
					{
						fSprites[i].isVisible = !hidden;
					}

					for (int i = 0; i < fLabels.Count; i++)
					{
						fLabels[i].isVisible = !hidden;
					}
				}
			}

			public Button Local;
			public Button Type;
			public Button Type2;
			public Button Synced;
			public FlickerHandle Handle;

			public LightSourceFlickerData Data => (pObj.data as LightSourceFlickerData)!;

			public LightSourceFlickerRepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
			{
				panel.size = new Vector2(250f, 145f);

				if (Data.Rad == new Vector2(0, 0))
				{
					Data.Rad = new Vector2(0f, 90f);
				}

				subNodes.Add(Handle = new FlickerHandle(this.owner, "Handle", this, Data.Rad));

				panel.subNodes.Add(Type = new Button(this.owner, "Type", this.panel, new Vector2(5, 65), 240f, "Affects: "));
				panel.subNodes.Add(Type2 = new Button(this.owner, "Type2", this.panel, new Vector2(5, 45), 240f, "Affects: "));
				panel.subNodes.Add(Local = new Button(this.owner, "Local", this.panel, new Vector2(5, 25), 240f, "Type: "));
				panel.subNodes.Add(Synced = new Button(this.owner, "Synced", this.panel, new Vector2(5, 5), 240f, "Synced: "));

				(panel.subNodes[0] as Slider).pos = new Vector2(5, 85f);
				(panel.subNodes[1] as Slider).pos = new Vector2(5, 105f);
				(panel.subNodes[2] as Slider).pos = new Vector2(5, 125f);
			}
			public override void Refresh()
			{
				if (Data.FrequencyMin > Data.FrequencyMax)
				{
					Data.FrequencyMin = Data.FrequencyMax;
				}
				else if (Data.FrequencyMax < Data.FrequencyMin)
				{
					Data.FrequencyMax = Data.FrequencyMin;
				}

				base.Refresh();
				Local.Text = "Type: " + (Data.Local ? "Local" : "Room");
				Synced.Text = "Synced: " + (Data.Synced ? "True" : "False");
				Type.Text = "Affects: " + Data.Type;
				Type2.Text = "Affects: " + Data.Type2;

			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (sender.IDstring == "Type")
				{
					if (Data.Type == LightSourceFlickerData.SunlightType.All)
						Data.Type = 0;
					else
						Data.Type++;
				}
				else if (sender.IDstring == "Type2")
				{
					if (Data.Type2 == LightSourceFlickerData.LightSourceType.All)
						Data.Type2 = 0;
					else
						Data.Type2++;
				}
				else if (sender.IDstring == "Local")
				{
					Handle.hidden = Data.Local;
					Data.Local = !Data.Local;
				}
				else if (sender.IDstring == "Synced")
				{
					Data.Synced = !Data.Synced;
				}
			}
		}
	}
}
