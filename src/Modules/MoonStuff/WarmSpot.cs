using MoreSlugcats;
using static RegionKit.Modules.MoonStuff.WarmSpotType;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class WarmSpot : UpdatableAndDeletable, IProvideWarmth
	{
		float IProvideWarmth.warmth => (placedObject.data as WarmSpotData).warmth / 100f;
		Room IProvideWarmth.loadedRoom => room;
		float IProvideWarmth.range => (placedObject.data as WarmSpotData).rad.magnitude;
		Vector2 IProvideWarmth.Position() => placedObject.pos;

		public PlacedObject placedObject;
		public WarmSpot(PlacedObject pObj, Room room) { this.room = room; this.placedObject = pObj; }
	}
}
