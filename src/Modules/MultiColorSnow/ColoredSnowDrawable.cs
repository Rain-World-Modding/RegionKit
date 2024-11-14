namespace RegionKit.Modules.MultiColorSnow;

public class ColoredSnowDrawable : IDrawable
{

	public Room room;
	public int visibleSnow;

	public ColoredSnowDrawable(Room room)
	{
		this.room = room;
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[0]);
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{

	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (this.visibleSnow == 0)
		{
			sLeaser.sprites[0].isVisible = false;
		}
		else
		{
			sLeaser.sprites[0].isVisible = true;
		}
		if (this.room != rCam.room)
		{
			sLeaser.CleanSpritesAndRemove();
		}
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new FSprite("Futile_White", true);
		sLeaser.sprites[0].x = this.room.game.rainWorld.options.ScreenSize.x * 0.5f;
		sLeaser.sprites[0].y = this.room.game.rainWorld.options.ScreenSize.y * 0.5f;
		sLeaser.sprites[0].scaleX = this.room.game.rainWorld.options.ScreenSize.x / 16f;
		sLeaser.sprites[0].scaleY = 48f;
		sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["RKDisplaySnowShader"];
		sLeaser.sprites[0].color = new Color(1f, 1f, 1f);
		sLeaser.sprites[0].alpha = 1f;
		this.AddToContainer(sLeaser, rCam, null);
	}
}
