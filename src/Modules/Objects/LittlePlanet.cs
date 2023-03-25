using System.Globalization;
using System.Text.RegularExpressions;
using DevInterface;

namespace RegionKit.Modules.Objects;

/// <summary>
/// By LB/M4rbleL1ne
/// A small cosmetic hologram
/// </summary>
public class LittlePlanet : CosmeticSprite
{
	private bool _underWaterMode;
	private Color _color;
	private readonly float _baseRad;
	private float _alpha, _speed;
	private readonly float[] _lastRot = new float[4], _rot = new float[4], _rotSpeed = new float[4] { -1f, .5f, -.25f, .125f }, _lastScaleX = new float[3], _scaleX = new float[3];
	private readonly bool[] _increaseRad = new bool[3];

	private PlacedObject PObj { get; init; }

	private LittlePlanetData Data => (PObj.data as LittlePlanetData)!;

	///<inheritdoc/>
	public LittlePlanet(Room room, PlacedObject placedObj)
	{
		this.room = room;
		PObj = placedObj;
		pos = placedObj.pos;
		lastPos = pos;
		_underWaterMode = room.GetTilePosition(PObj.pos).y < room.defaultWaterLevel;
		_color = Data.Color;
		_baseRad = Data.Rad;
		_alpha = Data.alpha;
		_speed = Data.speed;
	}

	///<inheritdoc/>
	public override void Update(bool eu)
	{
		pos = PObj.pos;
		_underWaterMode = room.GetTilePosition(PObj.pos).y < room.defaultWaterLevel;
		_color = Data.Color;
		_alpha = Data.alpha;
		_speed = Data.speed;
		base.Update(eu);
		for (var i = 0; i < 4; i++)
		{
			_lastRot[i] = _rot[i];
			_rot[i] += _rotSpeed[i] * _speed;
		}
		for (var i = 0; i < 3; i++)
		{
			_lastScaleX[i] = _scaleX[i];
			_scaleX[i] += (_increaseRad[i] ? .02f : -.02f) * _speed;
		}
	}

	///<inheritdoc/>
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		const string RING = "assets/regionkit/sprites/littleplanetring";
		const string PLANET = "assets/regionkit/sprites/littleplanet";
		sLeaser.sprites = new FSprite[6]
		{
			new("Futile_White")
			{
				shader = rCam.game.rainWorld.Shaders[(!_underWaterMode) ? "FlatLight" : "UnderWaterLight"],
				scale = _baseRad / 6f
			},
			new(PLANET) { shader = rCam.game.rainWorld.Shaders["Hologram"] },
			new(RING) { shader = rCam.game.rainWorld.Shaders["Hologram"] },
			new(RING) { shader = rCam.game.rainWorld.Shaders["Hologram"] },
			new(RING) { shader = rCam.game.rainWorld.Shaders["Hologram"] },
			new("Futile_White")
			{
				shader = rCam.game.rainWorld.Shaders[(!_underWaterMode) ? "FlatLight" : "UnderWaterLight"],
				scale = _baseRad / 24f
			}
		};
		for (var i = 1; i < sLeaser.sprites.Length - 1; i++)
			sLeaser.sprites[i].scale = _baseRad / 400f * i;
		sLeaser.sprites[1].scale -= sLeaser.sprites[1].scale / 4f;
		AddToContainer(sLeaser, rCam, null!);
	}

	///<inheritdoc/>
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		var sPos = Vector2.Lerp(lastPos, pos, timeStacker);
		var sRot = new[]
		{
			Mathf.Lerp(_lastRot[0], _rot[0], timeStacker),
			Mathf.Lerp(_lastRot[1], _rot[1], timeStacker),
			Mathf.Lerp(_lastRot[2], _rot[2], timeStacker),
			Mathf.Lerp(_lastRot[3], _rot[3], timeStacker)
		};
		var sScaleX = new[]
		{
			_baseRad / 400f * 2f * Mathf.Sin(Mathf.Lerp(_lastScaleX[0], _scaleX[0], timeStacker) * Mathf.PI),
			_baseRad / 400f * 3f * Mathf.Cos(Mathf.Lerp(_lastScaleX[1], _scaleX[1], timeStacker) * Mathf.PI),
			_baseRad / 400f * 4f * Mathf.Sin(Mathf.Lerp(_lastScaleX[0], _scaleX[0], timeStacker) * Mathf.PI)
		};
		foreach (FSprite? s in sLeaser.sprites)
		{
			s.x = sPos.x - camPos.x;
			s.y = sPos.y - camPos.y;
			s.alpha = _alpha;
		}
		for (var i = 1; i < sLeaser.sprites.Length - 1; i++)
			sLeaser.sprites[i].rotation = sRot[i - 1];
		for (var i = 2; i < sLeaser.sprites.Length - 1; i++)
		{
			sLeaser.sprites[i].scaleX = sScaleX[i - 2];
			if (sLeaser.sprites[i].scaleX >= _baseRad / 400f * i)
				_increaseRad[i - 2] = false;
			else if (sLeaser.sprites[i].scaleX <= 0f)
				_increaseRad[i - 2] = true;
		}
		sLeaser.sprites[0].alpha /= 2f;
		sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders[(!_underWaterMode) ? "FlatLight" : "UnderWaterLight"];
		sLeaser.sprites[5].shader = rCam.game.rainWorld.Shaders[(!_underWaterMode) ? "FlatLight" : "UnderWaterLight"];
		for (var i = 0; i < sLeaser.sprites.Length; i++)
			sLeaser.sprites[i].color = _color;
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

	///<inheritdoc/>
	public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContainer)
	{
		newContainer ??= rCam.ReturnFContainer("GrabShaders");
		sLeaser.sprites.RemoveFromContainer();
		newContainer.AddChild(sLeaser.sprites);
	}

	/// <summary>
	/// DevUI data
	/// </summary>
	public class LittlePlanetData : PlacedObject.ResizableObjectData
	{
		internal float _red = 1f;
		internal float _green = 1f;
		internal float _blue = 1f;
		/// <summary>
		/// Sprite alpha
		/// </summary>
		public float alpha = 1f;
		internal Vector2 _panelPos;
		/// <summary>
		/// Ring rotation speed
		/// </summary>
		public float speed = 1f;

		/// <summary>
		/// Sprite color
		/// </summary>
		public Color Color
		{
			get => new(_red, _green, _blue);
			set
			{
				_red = value.r;
				_green = value.g;
				_blue = value.b;
			}
		}

		///<inheritdoc/>
		public LittlePlanetData(PlacedObject owner) : base(owner) { }

		///<inheritdoc/>
		public override void FromString(string s)
		{
			var sAr = Regex.Split(s, "~");
			float.TryParse(sAr[0], NumberStyles.Any, CultureInfo.InvariantCulture, out handlePos.x);
			float.TryParse(sAr[1], NumberStyles.Any, CultureInfo.InvariantCulture, out handlePos.y);
			float.TryParse(sAr[3], NumberStyles.Any, CultureInfo.InvariantCulture, out _red);
			float.TryParse(sAr[4], NumberStyles.Any, CultureInfo.InvariantCulture, out _green);
			float.TryParse(sAr[5], NumberStyles.Any, CultureInfo.InvariantCulture, out _blue);
			float.TryParse(sAr[8], NumberStyles.Any, CultureInfo.InvariantCulture, out alpha);
			float.TryParse(sAr[9], NumberStyles.Any, CultureInfo.InvariantCulture, out _panelPos.x);
			float.TryParse(sAr[10], NumberStyles.Any, CultureInfo.InvariantCulture, out _panelPos.y);
			float.TryParse(sAr[12], NumberStyles.Any, CultureInfo.InvariantCulture, out speed);
			unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(sAr, 13);
		}

		///<inheritdoc/>
		public override string ToString() => SaveUtils.AppendUnrecognizedStringAttrs($"{BaseSaveString()}~Color(~{_red}~{_green}~{_blue}~)~Alpha:~{alpha}~{_panelPos.x}~{_panelPos.y}~Speed:~{speed}", "~", unrecognizedAttributes);
	}
}

/// <summary>
/// DevUI representation
/// </summary>
public class LittlePlanetRepresentation : ResizeableObjectRepresentation
{
	internal class LittlePlanetControlPanel : Panel, IDevUISignals
	{
		public class ControlSlider : Slider
		{
			private LittlePlanet.LittlePlanetData Data => ((parentNode.parentNode as LittlePlanetRepresentation)!.pObj.data as LittlePlanet.LittlePlanetData)!;

			public ControlSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title, false, 110f) { }

			public override void Refresh()
			{
				base.Refresh();
				var num = 0f;
				switch (IDstring)
				{
				case "ColorR_Slider":
					num = Data._red;
					NumberText = ((int)(255f * num)).ToString();
					break;
				case "ColorG_Slider":
					num = Data._green;
					NumberText = ((int)(255f * num)).ToString();
					break;
				case "ColorB_Slider":
					num = Data._blue;
					NumberText = ((int)(255f * num)).ToString();
					break;
				case "Alpha_Slider":
					num = Data.alpha;
					NumberText = ((int)(100f * num)).ToString() + "%";
					break;
				case "Speed_Slider":
					num = Data.speed / 2f;
					NumberText = ((int)(100f * num)).ToString() + "%";
					break;
				}
				RefreshNubPos(num);
			}

			public override void NubDragged(float nubPos)
			{
				switch (IDstring)
				{
				case "ColorR_Slider":
					Data._red = nubPos;
					break;
				case "ColorG_Slider":
					Data._green = nubPos;
					break;
				case "ColorB_Slider":
					Data._blue = nubPos;
					break;
				case "Alpha_Slider":
					Data.alpha = nubPos;
					break;
				case "Speed_Slider":
					Data.speed = nubPos * 2f;
					break;
				}
				parentNode.parentNode.Refresh();
				Refresh();
			}
		}

		public LittlePlanetControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new(250f, 105f), "Little Planet")
		{
			subNodes.Add(new ControlSlider(owner, "ColorR_Slider", this, new(5f, 85f), "Red: "));
			subNodes.Add(new ControlSlider(owner, "ColorG_Slider", this, new(5f, 65f), "Green: "));
			subNodes.Add(new ControlSlider(owner, "ColorB_Slider", this, new(5f, 45f), "Blue: "));
			subNodes.Add(new ControlSlider(owner, "Alpha_Slider", this, new(5f, 25f), "Alpha: "));
			subNodes.Add(new ControlSlider(owner, "Speed_Slider", this, new(5f, 5f), "Speed: "));
		}

		public void Signal(DevUISignalType type, DevUINode sender, string message) { }
	}

	private readonly int _linePixelSpriteIndex;
	private readonly LittlePlanetControlPanel _panel;

	///<inheritdoc/>
	public LittlePlanetRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, pObj.type.ToString(), true)
	{
		_panel = new(owner, "LittlePlanet_Panel", this, new(0f, 100f));
		subNodes.Add(_panel);
		_panel.pos = (pObj.data as LittlePlanet.LittlePlanetData)!._panelPos;
		fSprites.Add(new("pixel"));
		_linePixelSpriteIndex = fSprites.Count - 1;
		owner.placedObjectsContainer.AddChild(fSprites[_linePixelSpriteIndex]);
		fSprites[_linePixelSpriteIndex].anchorY = 0f;
	}

	///<inheritdoc/>
	public override void Refresh()
	{
		base.Refresh();
		MoveSprite(_linePixelSpriteIndex, absPos);
		fSprites[_linePixelSpriteIndex].scaleY = _panel.pos.magnitude;
		fSprites[_linePixelSpriteIndex].rotation = AimFromOneVectorToAnother(absPos, _panel.absPos);
		(pObj.data as LittlePlanet.LittlePlanetData)!._panelPos = _panel.pos;
	}
}

internal static class LittlePlanetExtensions
{
	internal static void AddChild(this FContainer self, params FNode[] nodes)
	{
		foreach (FNode node in nodes)
			self.AddChild(node);
	}

	internal static void RemoveFromContainer(this FNode[] self)
	{
		foreach (FNode node in self)
			node.RemoveFromContainer();
	}
}
