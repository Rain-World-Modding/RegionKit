namespace RegionKit.Modules.AridBarrens;

public class SandPuff : CosmeticSprite
{
	public SandPuff(Vector2 pos, float size)
	{
		this.pos = pos;
		this.lastPos = pos;
		this._size = size;
		this._lastLife = 1f;
		this._life = 1f;
		this._lifeTime = Mathf.Lerp(40f, 120f, UnityEngine.Random.value) * Mathf.Lerp(0.5f, 1.5f, size);
	}
	public override void Update(bool eu)
	{
		base.Update(eu);
		this.pos.y = this.pos.y + 0.5f;
		this.pos.x = this.pos.x + 0.25f;
		this._lastLife = this._life;
		this._life -= 1f / this._lifeTime;
		if (this._lastLife < 0f)
		{
			this.Destroy();
		}
	}

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new FSprite("Futile_White", true);
		sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Spores"];
		this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Background"));
		base.InitiateSprites(sLeaser, rCam);
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		sLeaser.sprites[0].x = Mathf.Lerp(this.lastPos.x, this.pos.x, timeStacker) - camPos.x;
		sLeaser.sprites[0].y = Mathf.Lerp(this.lastPos.y, this.pos.y, timeStacker) - camPos.y;
		sLeaser.sprites[0].scale = 10f * Mathf.Pow(1f - Mathf.Lerp(this._lastLife, this._life, timeStacker), 0.35f) * Mathf.Lerp(0.5f, 1.5f, this._size);
		sLeaser.sprites[0].alpha = Mathf.Lerp(this._lastLife, this._life, timeStacker);
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		sLeaser.sprites[0].color = palette.texture.GetPixel(9, 5);
		base.ApplyPalette(sLeaser, rCam, palette);
	}
	private float _life;
	private float _lastLife;
	private float _lifeTime;
	private float _size;
}
