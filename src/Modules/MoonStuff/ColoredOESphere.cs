using static RegionKit.Modules.MoonStuff.ColoredOESphereType;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	internal class ColoredOESphere : UpdatableAndDeletable//, IDrawable
	{
		private readonly PlacedObject placedObject;
		public float rad => RWCustom.Custom.Dist(placedObject.pos + (placedObject.data as ColoredOESphereData).rad, placedObject.pos);
		public float depth => (placedObject.data as ColoredOESphereData).depth;
		public float lIntensity => (placedObject.data as ColoredOESphereData).light / 100f;
		public float hue
		{
			get
			{
				if (room.world.region == null)
				{
					return 0.06f;
				}

				return (placedObject.data as ColoredOESphereData).Hue == -1f ? room.world.region.MoonRegionData().OESphereHue : (placedObject.data as ColoredOESphereData).Hue;
			}
		}

		public ColoredOESphere(PlacedObject pObj, Room room)
		{
			this.room = room;
			this.placedObject = pObj;
		}

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[3];
			sLeaser.sprites[0] = new FSprite("Futile_White");
			sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["ColoredOESphereBase"];
			sLeaser.sprites[1] = new FSprite("Futile_White");
			sLeaser.sprites[1].shader = rCam.room.game.rainWorld.Shaders["OESphereTop"];
			sLeaser.sprites[2] = new FSprite("Futile_White");
			sLeaser.sprites[2].shader = rCam.room.game.rainWorld.Shaders["ColoredOESphereLight"];
			AddToContainer(sLeaser, rCam, null);
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			Vector2 vector = new Vector2(placedObject.pos.x - camPos.x + 0.5f, placedObject.pos.y - camPos.y + 0.5f);
			float num = rad / 8f;
			float a = depth / 30f;
			float[] array = new float[3] { 1f, 2.87f, 4f };
			for (int i = 0; i < sLeaser.sprites.Length; i++)
			{
				sLeaser.sprites[i].x = vector.x;
				sLeaser.sprites[i].y = vector.y;
				sLeaser.sprites[i].scale = num * array[i];
			}

			sLeaser.sprites[0].color = new Color(hue, Custom.Mod(hue - 0.25f, 1f), 1f, a);
			sLeaser.sprites[1].color = new Color(1f, Custom.Mod(hue - 0.25f, 1f), 1f, a);
			sLeaser.sprites[2].color = new Color(hue, hue, lIntensity, a);
			if (base.slatedForDeletetion || room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
			}
		}

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[0]);
			rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[1]);
			rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[2]);
			sLeaser.sprites[1].MoveInFrontOfOtherNode(sLeaser.sprites[0]);
			sLeaser.sprites[0].MoveToBack();
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
	}
}
