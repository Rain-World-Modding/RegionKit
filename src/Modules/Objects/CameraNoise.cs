namespace RegionKit.Modules.Objects;
public class CameraNoise : UpdatableAndDeletable, IDrawable
{
	private CameraNoiseData _data => (CameraNoiseData)_owner.data;
	private PlacedObject _owner;
	private (Vector2, float, int, int) _c_settings = (default, 0f, 1, 2);
	private string _c_tags = "";
	private int[] _tags;
	public CameraNoise(Room room, PlacedObject owner)
	{
		_tags = new int[0];
		this.room = room;
		this._owner = owner;
	}
	public override void Update(bool eu)
	{
		base.Update(eu);
		RegenerateTagArrayIfNeeded();
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		var resolution = rCam.game.rainWorld.screenSize;
		RegenerateSpritesIfNeeded(sLeaser, rCam);
		this.AddToContainer(sLeaser, rCam, null);
	}
	private void RegenerateTagArrayIfNeeded()
	{
		if (_c_tags == _data.tags)
		{
			return;
		}
		_tags = System.Text.RegularExpressions.Regex.Split(_data.tags, "\\s*,\\s*").Select((x) =>
		{
			int.TryParse(x, out int res);
			return res;
		}).ToArray();
		_c_tags = _data.tags;
	}
	private void RegenerateSpritesIfNeeded(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		var resolution = rCam.game.rainWorld.screenSize;
		if (sLeaser.sprites != null && (resolution, _data.amount, _data.minsegments, _data.maxsegments) == _c_settings)
		{
			return;
		}
		LogDebug("generating cameranoise sprites");
		var area = resolution.x * resolution.y;
		const float PARTICLES_PER_PIXEL = 0.0005f;
		if (sLeaser.sprites is FSprite[] sprites)
		{
			foreach (FSprite? sprite in sprites) sprite?.RemoveFromContainer();
		}
		sLeaser.sprites = new FSprite[(int)(area * PARTICLES_PER_PIXEL * _data.amount)];
		foreach (int i in Range(sLeaser.sprites.Length))
		{
			// sLeaser.sprites[i] = new FSprite("pixel")
			// {
			// 	scale = 10f

			// };
			sLeaser.sprites[i] = TriangleMesh.MakeLongMesh(_data.maxsegments, false, true);
		}
		_c_settings = (resolution, _data.amount, _data.minsegments, _data.maxsegments);
		this.AddToContainer(sLeaser, rCam, null);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		var resolution = rCam.game.rainWorld.screenSize;
		RegenerateSpritesIfNeeded(sLeaser, rCam);
		foreach (TriangleMesh mesh in sLeaser.sprites)
		{
			var basepos = new Vector2(UnityEngine.Random.Range(0f, resolution.x), UnityEngine.Random.Range(0f, resolution.y));
			var lastpos = basepos;
			var currentpos = basepos;
			var direction = DegToVec(UnityEngine.Random.Range(0f, 360f)).normalized;
			var segments = UnityEngine.Random.Range(_data.minsegments, _data.maxsegments + 1);
			for (int i = 0; i < _data.maxsegments; i++)
			{
				lastpos = currentpos;
				var baseindex = i * 4;
				var (bl, br, fl, fr) = (baseindex, baseindex + 1, baseindex + 2, baseindex + 3);
				var turn = ClampedFloatDeviation(_data.swirlbase, _data.swirlfluke);
				var step = ClampedFloatDeviation(_data.lenbase, _data.lenfluke);
				var thickness = ClampedFloatDeviation(_data.thickbase, _data.thickfluke);
				direction = RotateAroundOrigo(direction, turn);
				var right = RotateAroundOrigo(direction, 90f);
				var left = RotateAroundOrigo(direction, -90f);
				currentpos += direction * step;
				mesh.MoveVertice(bl, lastpos + left * (thickness / 2f));
				mesh.MoveVertice(br, lastpos + right * (thickness / 2f));
				mesh.MoveVertice(fl, lastpos + left * (thickness / 2f));
				mesh.MoveVertice(fr, lastpos + right * (thickness / 2f));
			}
			foreach (int i in Range(mesh.verticeColors.Length))
			{
				var col = _data.colorBase.Deviation(_data.colorFluke);
				mesh.verticeColors[i] = col.Clamped();
				mesh.verticeColors[i].a =
					(i >= segments + 1 * 4)
					? 0f
					: ClampedFloatDeviation(_data.alphabase, _data.alphafluke, 0, 1);
			}

			//sprite.SetPosition(UnityEngine.Random.Range(0f, resolution.x), UnityEngine.Random.Range(0f, resolution.y));
			//sprite.alpha = UnityEngine.Random.Range(0.5f, 1f);
		}
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		var container = newContatiner ?? rCam.ReturnFContainer(ContainerCodes.HUD.ToString());
		foreach (var sprite in sLeaser.sprites)
		{
			sprite.RemoveFromContainer();
			container.AddChild(sprite);
		}

	}
	public class CameraNoiseData : ManagedData
	{
		[FloatField("00amount", 0f, 1f, 1f, 0.1f, displayName: "amount")]
		public float amount;
		[IntegerField("01minseg", 1, 20, 1, ManagedFieldWithPanel.ControlType.slider, displayName: "min segments")]
		public int minsegments;
		[IntegerField("02maxseg", 2, 20, 1, ManagedFieldWithPanel.ControlType.slider, displayName: "min segments")]
		public int maxsegments;
		[FloatField("03lenbase", 0f, 20f, 5f, 1f, displayName: "segment len base (px)")]
		public float lenbase;
		[FloatField("04lenfluke", 0f, 20f, 5f, 0.1f, displayName: "segment len fluke (px)")]
		public float lenfluke;
		[FloatField("05thickbase", 1f, 20f, 2f, 0.1f, displayName: "thickness base (px)")]
		public float thickbase;
		[FloatField("06thickfluke", 1f, 20f, 1f, 0.1f, displayName: "thickness fluke (px)")]
		public float thickfluke;
		[FloatField("07swirlbase", -30f, 30f, 0f, 1f, displayName: "swirl base (deg)")]
		public float swirlbase;
		[FloatField("08swirlfluke", 0f, 60f, 10f, 1f, displayName: "swirl fluke (deg)")]
		public float swirlfluke;

		[ColorField("09colbase", 1f, 1f, 1f, 0.1f, DisplayName: "color base")]
		public Color colorBase;
		[ColorField("10colfluke", 0f, 0f, 0f, 1f, DisplayName: "color fluke")]
		public Color colorFluke;
		[FloatField("11alphabase", 0f, 1f, 1f, 0.05f, displayName: "alpha base")]
		public float alphabase;
		[FloatField("12alphafluke", 0f, 1f, 1f, 0.05f, displayName: "alpha fluke")]
		public float alphafluke;
		[StringField("13tags", "0", displayName: "tags")]
		public string tags = "0";
		public CameraNoiseData(PlacedObject owner) : base(owner, null)
		{
		}
	}
}
