#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class ConveyorBeltCover : UpdatableAndDeletable, IDrawable
	{
		public PlacedObject PlacedObject;

		public ConveyorBeltCover(PlacedObject placedObject, Room room) : base()
		{
			this.PlacedObject = placedObject;
			this.room = room;
		}

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[1];
			sLeaser.sprites[0] = new FSprite("ConveyorBelt_Cover");
			sLeaser.sprites[0].shader = room.game.rainWorld.Shaders["ColoredSprite2"];

			AddToContainer(sLeaser, rCam, null);
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			sLeaser.sprites[0].SetPosition(room.MiddleOfTile(PlacedObject.pos - new Vector2(0f, 57f)) - camPos);

			if (base.slatedForDeletetion || room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
			}
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			for (int i = 0; i < sLeaser.sprites.Length; i++)
			{
				sLeaser.sprites[i].color = palette.blackColor;
			}
		}

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			if (newContatiner == null)
			{
				newContatiner = rCam.ReturnFContainer("Items");
			}

			for (int i = 0; i < sLeaser.sprites.Length; i++)
			{
				newContatiner.AddChild(sLeaser.sprites[i]);
			}
		}
	}
}
