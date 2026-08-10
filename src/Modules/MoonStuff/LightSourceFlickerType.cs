using System.Text.RegularExpressions;
using DevInterface;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class LightSourceFlickerType : ManagedObjectType
	{
		public LightSourceFlickerType() : base("Light Source Flicker", Objects._Module.DECORATIONS_POM_CATEGORY, null, typeof(LightSourceFlickerData), typeof(LightSourceFlickerRepresentation))
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

			public int Type;

			public int Type2;

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
				return base.ToString() + "~" + Local + "~" + Type + "~" + Type2 + "~" + Rad.x + "~" + Rad.y + "~" + Synced;
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] arr = Regex.Split(s, "~");
				try
				{
					Local = bool.Parse(arr[base.FieldsWhenSerialized + 0]);
					Type = int.Parse(arr[base.FieldsWhenSerialized + 1]);
					Type2 = int.Parse(arr[base.FieldsWhenSerialized + 2]);
					Rad.x = float.Parse(arr[base.FieldsWhenSerialized + 3]);
					Rad.y = float.Parse(arr[base.FieldsWhenSerialized + 4]);
					Synced = bool.Parse(arr[base.FieldsWhenSerialized + 5]);
				}
				catch { }
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
			public LightSourceFlickerRepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
			{
				panel.size = new Vector2(250f, 145f);

				if ((pObj.data as LightSourceFlickerData).Rad == new Vector2(0, 0))
				{
					(pObj.data as LightSourceFlickerData).Rad = new Vector2(0f, 90f);
				}

				subNodes.Add(Handle = new FlickerHandle(this.owner, "Handle", this, (pObj.data as LightSourceFlickerData).Rad));

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
				if ((pObj.data as LightSourceFlickerData).FrequencyMin > (pObj.data as LightSourceFlickerData).FrequencyMax)
				{
					(pObj.data as LightSourceFlickerData).FrequencyMin = (pObj.data as LightSourceFlickerData).FrequencyMax;
				}
				else if ((pObj.data as LightSourceFlickerData).FrequencyMax < (pObj.data as LightSourceFlickerData).FrequencyMin)
				{
					(pObj.data as LightSourceFlickerData).FrequencyMax = (pObj.data as LightSourceFlickerData).FrequencyMin;
				}

				base.Refresh();
				Local.Text = "Type: " + ((pObj.data as LightSourceFlickerData).Local ? "Local" : "Room");
				Synced.Text = "Synced: " + ((pObj.data as LightSourceFlickerData).Synced ? "True" : "False");

				if ((pObj.data as LightSourceFlickerData).Type == 0)
				{
					Type.Text = "Affects: Static";
				}
				else if ((pObj.data as LightSourceFlickerData).Type == 1)
				{
					Type.Text = "Affects: Sun";
				}
				else
				{
					Type.Text = "Affects: All";
				}

				if ((pObj.data as LightSourceFlickerData).Type2 == 0)
				{
					Type2.Text = "Affects: Normal";
				}
				else if ((pObj.data as LightSourceFlickerData).Type2 == 1)
				{
					Type2.Text = "Affects: Flat";
				}
				else
				{
					Type2.Text = "Affects: Both";
				}
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (sender.IDstring == "Type")
				{
					if ((pObj.data as LightSourceFlickerData).Type < 2)
					{
						(pObj.data as LightSourceFlickerData).Type += 1;
					}
					else
					{
						(pObj.data as LightSourceFlickerData).Type = 0;
					}
				}
				else if (sender.IDstring == "Type2")
				{
					if ((pObj.data as LightSourceFlickerData).Type2 < 2)
					{
						(pObj.data as LightSourceFlickerData).Type2 += 1;
					}
					else
					{
						(pObj.data as LightSourceFlickerData).Type2 = 0;
					}
				}
				else if (sender.IDstring == "Local")
				{
					Handle.hidden = (pObj.data as LightSourceFlickerData).Local;
					(pObj.data as LightSourceFlickerData).Local = !(pObj.data as LightSourceFlickerData).Local;
				}
				else if (sender.IDstring == "Synced")
				{
					(pObj.data as LightSourceFlickerData).Synced = !(pObj.data as LightSourceFlickerData).Synced;
				}
			}
		}
	}
}
