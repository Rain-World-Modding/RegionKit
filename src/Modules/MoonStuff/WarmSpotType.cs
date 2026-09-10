using DevInterface;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class WarmSpotType : ManagedObjectType
	{
		public WarmSpotType() : base(_Enums.WarmSpot, Objects._Enums.GameplayCategory, null, typeof(WarmSpotData), typeof(WarmSpotRepresentation)) { }
		public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room) => ModManager.MSC ? new WarmSpot(placedObject, room) : null;

		public override PlacedObjectRepresentation MakeRepresentation(PlacedObject pObj, ObjectsPage objPage)
		{
			if (!ModManager.MSC)
			{
				return null;
			}

			return base.MakeRepresentation(pObj, objPage);
		}

		public class WarmSpotData : ManagedData
		{
#pragma warning disable 0649
			[FloatField("warmth", 0.01f, 1f, 0.05f, 0.01f, ManagedFieldWithPanel.ControlType.slider, "Warmth:")]
			public float warmth;

			[BackedByField("rad")]
			public Vector2 rad;
#pragma warning restore 0649

			private static ManagedField[] customFields = new ManagedField[] { new Vector2Field("rad", new Vector2(0, 50), Vector2Field.VectorReprType.circle) };
			public float Hue = -1f;

			public WarmSpotData(PlacedObject owner) : base(owner, customFields) { }
		}

		public class WarmSpotRepresentation : ManagedRepresentation
		{
			public WarmSpotRepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj) { }
		}
	}
}
