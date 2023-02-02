namespace RegionKit.Modules.Objects;

internal class Shroud : CosmeticSprite
{
	private readonly PlacedObject _pObj;
	private readonly FloatRect _rect;
	internal Vector2[] _quad;
	private float _alpha;
	private bool _active;
	private bool _playerInside;
	private int _ID;

	public Shroud(PlacedObject pObj, Room room)
	{
		this._pObj = pObj;
		this.room = room;
		_alpha = 1f;
		_quad = (this._pObj.data as ManagedData)!.GetValue<Vector2[]>("quad")!;
		//this.rect = new FloatRect(quad[0],quad[1],quad[2],quad[3]);
	}
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[1];
		var tris = new TriangleMesh.Triangle[]
		{
			new TriangleMesh.Triangle(0, 1, 2),
			new TriangleMesh.Triangle(2, 1, 3)
		};
		var mesh = new TriangleMesh("Futile_White", tris, false);
		//Bottom left
		mesh.MoveVertice(0, new Vector2(0f, 0f));
		//Top left
		mesh.MoveVertice(1, new Vector2(0f, 1f));
		//Bottom right
		mesh.MoveVertice(2, new Vector2(1f, 0f));
		//Top right
		mesh.MoveVertice(3, new Vector2(1f, 1f));

		mesh.UVvertices[0] = new Vector2(0f, 0f);
		mesh.UVvertices[1] = new Vector2(0f, 1f);
		mesh.UVvertices[2] = new Vector2(1f, 0f);
		mesh.UVvertices[3] = new Vector2(1f, 1f);

		sLeaser.sprites[0] = mesh;
		//sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Background"];
		AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("GrabShaders"));
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		var triangleMesh = (sLeaser.sprites[0] as TriangleMesh)!;
		triangleMesh.MoveVertice(0, _pObj.pos - camPos);
		triangleMesh.MoveVertice(1, _pObj.pos + _quad[1] - camPos);
		triangleMesh.MoveVertice(2, _pObj.pos + _quad[3] - camPos);
		triangleMesh.MoveVertice(3, _pObj.pos + _quad[2] - camPos);
		sLeaser.sprites[0].alpha = _alpha;
		sLeaser.sprites[0].color = rCam.PixelColorAtCoordinate(_pObj.pos);
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

	public override void Update(bool eu)
	{
		_quad = (_pObj.data as ManagedData)!.GetValue<Vector2[]>("quad")!;
		Vector2 camPos = room.game.cameras[0].pos;
		Vector2[] poly = new Vector2[]
		{
		_pObj.pos - camPos,
		_pObj.pos + _quad[1]- camPos,
		_pObj.pos + _quad[3]- camPos,
		_pObj.pos + _quad[2]- camPos,
		};

		if (_active)
		{
			_alpha -= 0.03f;
		}
		else
		{
			_alpha += 0.05f;
		}

		_alpha = Mathf.Clamp(_alpha, 0f, 1f);

		base.Update(eu);
	}

	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		sLeaser.sprites[0].color = rCam.PixelColorAtCoordinate(_pObj.pos);
		base.ApplyPalette(sLeaser, rCam, palette);
	}
}
