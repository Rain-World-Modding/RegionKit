using DevInterface;

#nullable disable

namespace RegionKit.Modules.IO
{
	public class CounterType : ManagedObjectType
	{
		public CounterType() : base("Counter", _Enums.DevObjectCategories.MoonsStuffIO.value, null, typeof(CounterData), typeof(CounterRepresentation))
		{
		}

		public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
		{
			return new Counter(placedObject);
		}

		public class CounterData : IOType.IOData
		{
			[IntegerField("index", 2, 10, 2, displayName: "Trigger At:")]
			public int index;

			public CounterData(PlacedObject owner) : base(owner, null)
			{
				NeedsControlPanel = true;
			}
		}

		public class CounterRepresentation : IOType.IORepresentation, IDevUISignals
		{
			public Button StartingState;

			public PlacedObject PlacedObject;

			public CounterRepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
			{
				panel.size = new Vector2(165f, 45f);
				PlacedObject = pObj;

				(panel.subNodes[0] as PositionedDevUINode).pos = new Vector2(5f, 25f);
				(subNodes[2] as IOType.IOPanel).pos = new Vector2(panel.size.x + 10f, 5f);
			}
		}
	}
}
