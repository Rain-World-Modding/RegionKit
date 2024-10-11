namespace RegionKit.Modules.Objects;
/// <summary>
/// A fan that turns on and off depending on room power level
/// </summary>
internal class SpinningFan : UpdatableAndDeletable, IDrawable
{
	private readonly PlacedObject _pObj;
	private Vector2 _pos;
	private float _speed, _rot, _lastRot, _scale, _depth, _getToSpeed;

	public SpinningFan(PlacedObject pObj, Room room)
	{
		_pObj = pObj;
		this.room = room;
		var managedData = (ManagedData)_pObj.data;
		_speed = managedData.GetValue<float>("speed");
		_scale = managedData.GetValue<float>("scale");
		_depth = managedData.GetValue<float>("depth");
	}

	public override void Update(bool eu)
	{
		_pos = _pObj.pos;
		var managedData = (ManagedData)_pObj.data;
		_getToSpeed = Mathf.Lerp(-10f, 10f, managedData.GetValue<float>("speed"));
		if (room.world.rainCycle.brokenAntiGrav is AntiGravity.BrokenAntiGravity g)
			_speed = LerpAndTick(_speed, g.CurrentLightsOn > 0f ? _getToSpeed : 0f, 0.035f, 0.0008f);
		else
			_speed = _getToSpeed;
		_lastRot = _rot;
		_rot += _speed;
		if (_rot >= 360f)
		{
			_rot -= 360f;
			_lastRot -= 360f;
		}
		else if (_rot <= -360f)
		{
			_rot += 360f;
			_lastRot += 360f;
		}
		_scale = managedData.GetValue<float>("scale");
		_depth = managedData.GetValue<float>("depth");
		base.Update(eu);
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[]
		{
			new("assets/regionkit/sprites/fan", true)
			{
				shader = rCam.game.rainWorld.Shaders["LegacyColoredSprite2"]
			}
		};
		AddToContainer(sLeaser, rCam, null);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		FSprite s0 = sLeaser.sprites[0];
		s0.x = _pos.x - camPos.x;
		s0.y = _pos.y - camPos.y;
		s0.scale = Mathf.Lerp(0.2f, 2f, _scale);
		s0.rotation = Mathf.Lerp(_lastRot, _rot, timeStacker);
		s0.alpha = _depth;
		if (slatedForDeletetion || room != rCam.room)
			sLeaser.CleanSpritesAndRemove();
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[0]);
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
}
