namespace RegionKit.Modules.Machinery.V2;

public class Wheel : UpdatableAndDeletable, IDrawable
{
	public float lastLifetime;
	public float lifetime;
	public Func<float>? getSpeed;
	public OscillationParams oscillation;
	public PartVisuals visuals;
	public Vector2 pos;
	private ContainerCodes _lastContainer;
	public Wheel(Func<float>? getSpeed)
	{
		this.getSpeed = getSpeed;
	}
	public override void Update(bool eu)
	{
		base.Update(eu);
		lastLifetime = lifetime;
		lifetime += getSpeed?.Invoke() ?? 1.0f;
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		if (newContatiner == null)
		{
			newContatiner = rCam.ReturnFContainer(visuals.container);
		}
		foreach (FSprite fsprite in sLeaser.sprites)
		{
			fsprite.RemoveFromContainer();
			newContatiner.AddChild(fsprite);
		}
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		FSprite sprite = sLeaser.sprites[0];
		float now = Mathf.Lerp(lastLifetime, lifetime, timeStacker);
		float rotateBy = oscillation.ValueAt(now);
		Futile.atlasManager.TryGetElementWithName(visuals.atlasElement, out var selectedElement);
		FAtlasElement defaultElement = Futile.atlasManager.GetElementWithName("pixel");
		Vector2 drawPos = pos - camPos;

		sprite.SetPosition(drawPos);
		float finalAngle = sprite.rotation + rotateBy;
		if (finalAngle > 360f) finalAngle = finalAngle - finalAngle % 360f;
		sprite.rotation = finalAngle;
		sprite.element = selectedElement ?? defaultElement;
		sprite.scaleX = visuals.scaleX;
		sprite.scaleY = visuals.scaleY;
		sprite.alpha = visuals.alpha;
		sprite.color = visuals.color;
		sprite.anchorX = visuals.anchorX;
		sprite.anchorY = visuals.anchorY;
		if (_lastContainer != visuals.container)
		{
			LogTrace("switching wheel container");
			AddToContainer(sLeaser, rCam, rCam.ReturnFContainer(visuals.container));
		}
		_lastContainer = visuals.container;
		if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || this.room != rCam.room))
		{
			sLeaser.CleanSpritesAndRemove();
		}
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new FSprite("pixel");
		AddToContainer(sLeaser, rCam, null);
	}
}