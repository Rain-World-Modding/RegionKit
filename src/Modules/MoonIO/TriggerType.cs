using System.Text.RegularExpressions;
using DevInterface;

#nullable disable

namespace RegionKit.Modules.MoonIO
{
	public class TriggerType : ManagedObjectType
	{
		public TriggerType() : base(_Enums.PlacedObjectType.Trigger.value, _Enums.DevObjectCategories.MoonsStuffIO.value, null, typeof(TriggerData), typeof(TriggerRepresentation))
		{
		}

		public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
		{
			return new Trigger(placedObject, room);
		}

		public class TriggerData : IOType.IOData
		{
			[BackedByField("Size")]
			public Vector2 Size;

			private static ManagedField[] SizeField = new ManagedField[] {
					new Vector2Field("Size", new Vector2(0, 60), Vector2Field.VectorReprType.circle),
			};

			public string Affects;

			public TriggerData(PlacedObject owner) : base(owner, SizeField)
			{
				Affects = "Players";
				NeedsControlPanel = true;
			}

			public override string ToString()
			{
				return base.ToString() + "~" + Affects;
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] arr = Regex.Split(s, "~");
				try
				{
					Affects = arr[base.FieldsWhenSerialized + 0].ToString();
				}
				catch { }
			}
		}

		public class TriggerRepresentation : IOType.IORepresentation, IDevUISignals
		{
			public Button StartingState;
			public PlacedObject PlacedObject;

			public TriggerRepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
			{
				panel.size = new Vector2(190f, 45f);
				PlacedObject = pObj;

				panel.subNodes.Add(StartingState = new Button(this.owner, "AffectsButton", this.panel, new Vector2(5f, 25f), 180, "Affects: Players"));

				(subNodes[2] as Button).size.x = 180;
				(subNodes[2] as Button).fSprites[0].scaleX = 180f;

				(subNodes[3] as IOType.IOPanel).pos = new Vector2(panel.size.x + 10f, 5f);
			}

			public override void Refresh()
			{
				base.Refresh();
				StartingState.Text = "Affects: " + (PlacedObject.data as TriggerData).Affects.ToString();
			}

			public override void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				base.Signal(type, sender, message);

				if (sender.IDstring == "AffectsButton")
				{
					switch ((PlacedObject.data as TriggerData).Affects)
					{
						case "Players":
							(PlacedObject.data as TriggerData).Affects = "Creatures"; break;
						case "Creatures":
							(PlacedObject.data as TriggerData).Affects = "Both"; break;
						default:
							(PlacedObject.data as TriggerData).Affects = "Players"; break;
					}
				}
			}
		}
	}
}
