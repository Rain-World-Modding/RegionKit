using RegionKit.Extras;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class ConveyorBeltCover : UpdatableAndDeletable
	{
		public PlacedObject PlacedObject;
		public DynamicLevelAtlasElement element;

		public ConveyorBeltCover(PlacedObject placedObject, Room room) : base()
		{
			this.PlacedObject = placedObject;
			this.room = room;

			element = new DynamicLevelAtlasElement(PlacedObject.pos, Vector2.one, "ConveyorBelt_Cover", 2);
			room.AddObject(element);
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			element.pos = room.MiddleOfTile(PlacedObject.pos - new Vector2(0f, 57f));
		}

		public override void Destroy()
		{
			base.Destroy();
			element.Destroy();
		}
	}
}
