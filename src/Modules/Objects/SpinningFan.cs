namespace RegionKit.Modules.Objects;
/// <summary>
/// A fan that turns on and off depending on room power level
/// </summary>
internal class SpinningFan : UpdatableAndDeletable, IDrawable
{
	private readonly PlacedObject _pObj;
	private float _getToSpeed;
	private Vector2 _pos;
	private float _speed;
	private float _scale;
	private float _depth;
	public SpinningFan(PlacedObject pObj, Room room)
	{
		this._pObj = pObj;
		this.room = room;
		var managedData = (this._pObj.data as ManagedData)!;
		_speed = managedData.GetValue<float>("speed");
		_scale = managedData.GetValue<float>("scale");
		_depth = managedData.GetValue<float>("depth");
	}
	
	public override void Update(bool eu)
	{
		_pos = _pObj.pos;
		var managedData = (_pObj.data as ManagedData)!;
		_getToSpeed = Mathf.Lerp(-10f, 10f, managedData.GetValue<float>("speed"));
		if (room.world.rainCycle.brokenAntiGrav != null)
		{
			float target = room.world.rainCycle.brokenAntiGrav.CurrentLightsOn > 0f ? _getToSpeed : 0f;
			_speed = Custom.LerpAndTick(_speed, target, 0.035f, 0.0008f);
		}
		else
		{
			_speed = _getToSpeed;
		}
		_scale = managedData.GetValue<float>("scale");
		_depth = managedData.GetValue<float>("depth");
		base.Update(eu);
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new FSprite("assets/regionkit/sprites/fan", true);
		sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["ColoredSprite2"];
		AddToContainer(sLeaser, rCam, null);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		sLeaser.sprites[0].x = _pos.x - camPos.x;
		sLeaser.sprites[0].y = _pos.y - camPos.y;
		sLeaser.sprites[0].scale = Mathf.Lerp(0.2f, 2f, _scale);
		sLeaser.sprites[0].rotation += _speed * timeStacker;
		sLeaser.sprites[0].alpha = _depth;
		if (slatedForDeletetion || room != rCam.room)
		{
			sLeaser.CleanSpritesAndRemove();
		}
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[0]);
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{

	}
}
