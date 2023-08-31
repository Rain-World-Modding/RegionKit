namespace RegionKit.Modules.Slideshow;

public class SlideShowUAD : UpdatableAndDeletable, IDrawable
{
	//private readonly Room room;
	private readonly PlacedObject _owner;
	private readonly Playback _playback;
	private PlayState _playState;
	private SlideShowInstant _prevInstant;
	private SlideShowInstant _thisInstant;
	private SlideShowData _Data => (SlideShowData)_owner.data;

	public SlideShowUAD(
		Room room,
		PlacedObject placedObject)
	{
		this.room = room;
		this._owner = placedObject;
		try
		{
			(Playback? playback, _) = _Module.__playbacksById[_Data.id];
			this._playback = playback;
			this._playState = new(playback);
		}
		catch (KeyNotFoundException keyex)
		{
			__logger.LogError($"Error constructing slideshow UAD {keyex} destroying itself");
			Destroy();
			throw;
		}
		_prevInstant = _thisInstant = new SlideShowInstant("Circle20", "Basic", ContainerCodes.Foreground, Vector2.zero, Color.white);
	}
	public override void Update(bool eu)
	{
		try
		{
			base.Update(eu);
			if (!_playState.Completed)
			{
				_playState.Update();
			}
			_prevInstant = _thisInstant;
			_thisInstant = _playState.ThisInstant();
		}
		catch (Exception ex)
		{
			__logger.LogError($"Error on slideshow uad update {ex}, deleting itself");
			Destroy();
		}
	}
	public void AddToContainer(
		RoomCamera.SpriteLeaser sLeaser,
		RoomCamera rCam,
		FContainer? newContatiner)
	{
		newContatiner ??= rCam.ReturnFContainer(_Data.container.ToString());
		foreach (FSprite fsprite in sLeaser.sprites)
		{
			fsprite.RemoveFromContainer();
			newContatiner.AddChild(fsprite);
		}
	}
	public void ApplyPalette(
		RoomCamera.SpriteLeaser sLeaser,
		RoomCamera rCam,
		RoomPalette palette)
	{

	}
	public void DrawSprites(
		RoomCamera.SpriteLeaser sLeaser,
		RoomCamera rCam,
		float timeStacker,
		Vector2 camPos)
	{
		FAtlasElement element = Futile.atlasManager.GetElementWithName(Futile.atlasManager.DoesContainElementWithName(_thisInstant.elementName) ? _thisInstant.elementName : "Futile_White");
		FShader shader = rCam.game.rainWorld.Shaders.TryGetValue(_thisInstant.shader, out FShader selectedShader) ? selectedShader : rCam.game.rainWorld.Shaders["Basic"];
		Vector2 position = Vector2.Lerp(_prevInstant.position, _thisInstant.position, timeStacker);
		Color color = Color.Lerp(_prevInstant.color, _thisInstant.color, timeStacker);

		TriangleMesh mesh = (TriangleMesh)sLeaser.sprites[0];
		mesh.MoveVertice(0, _owner.pos + position - camPos);
		mesh.MoveVertice(1, _owner.pos + position + _Data.quad[1] - camPos);
		mesh.MoveVertice(2, _owner.pos + position + _Data.quad[3] - camPos);
		mesh.MoveVertice(3, _owner.pos + position + _Data.quad[2] - camPos);
		mesh.element = element;
		mesh.shader = shader;
		for (int i = 0; i < 4; i++)
		{
			mesh.verticeColors[i] = color;
		}
		mesh.UVvertices[0] = element.uvBottomLeft;
		mesh.UVvertices[1] = element.uvBottomRight;
		mesh.UVvertices[3] = element.uvTopRight;
		mesh.UVvertices[2] = element.uvTopLeft;

		// mesh.color = color;
		// mesh.alpha = color.a;
		if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || this.room != rCam.room))
		{
			sLeaser.CleanSpritesAndRemove();
		}
		if (_thisInstant.container != _prevInstant.container)
		{
			AddToContainer(sLeaser, rCam, rCam.ReturnFContainer(_thisInstant.container.ToString()));
		}
	}
	public void InitiateSprites(
		RoomCamera.SpriteLeaser sLeaser,
		RoomCamera rCam)
	{
		ref FSprite[]? sprites = ref sLeaser.sprites;
		sprites = new FSprite[1];
		TriangleMesh.Triangle[]? tris = new TriangleMesh.Triangle[]
		{
			new TriangleMesh.Triangle(0, 1, 2),
			new TriangleMesh.Triangle(2, 1, 3)
		};
		TriangleMesh? mesh = new TriangleMesh("Futile_White", tris, true);
		sprites[0] = mesh;
		__logger.LogWarning($"{_thisInstant}, {_prevInstant}");
		AddToContainer(
			sLeaser,
			rCam,
			// null
			rCam.ReturnFContainer(_thisInstant.container.ToString())
			);
	}
}