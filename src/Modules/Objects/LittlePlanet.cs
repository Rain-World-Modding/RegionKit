using System.Globalization;
using System.Runtime.CompilerServices;
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
	private readonly PlacedObject _pObj;

	private LittlePlanetData Data
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (_pObj.data as LittlePlanetData)!;
	}

	///<inheritdoc/>
	public LittlePlanet(Room room, PlacedObject placedObj)
	{
		this.room = room;
		_pObj = placedObj;
		pos = placedObj.pos;
		lastPos = pos;
		_underWaterMode = room.GetTilePosition(pos).y < room.defaultWaterLevel;
		_color = Data.Color;
		_baseRad = Data.Rad;
		_alpha = Data.alpha;
		_speed = Data.speed;
	}

	///<inheritdoc/>
	public override void Update(bool eu)
	{
		pos = _pObj.pos;
		_underWaterMode = room.GetTilePosition(pos).y < room.defaultWaterLevel;
		_color = Data.Color;
		_alpha = Data.alpha;
		_speed = Data.speed;
		var speed = _speed;
		base.Update(eu);
		var temp = _rot;
		for (var i = 0; i < temp.Length; i++)
		{
			_lastRot[i] = temp[i];
			temp[i] += _rotSpeed[i] * speed;
			if (temp[i] >= 360f)
			{
				temp[i] -= 360f;
				_lastRot[i] -= 360f;
			}
			else if (temp[i] <= -360f)
			{
				temp[i] += 360f;
				_lastRot[i] += 360f;
			}
		}
		temp = _scaleX;
		for (var i = 0; i < temp.Length; i++)
		{
			_lastScaleX[i] = temp[i];
			temp[i] += (_increaseRad[i] ? .02f : -.02f) * speed;
		}
	}

	///<inheritdoc/>
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		const string RING = "assets/regionkit/sprites/littleplanetring", PLANET = "assets/regionkit/sprites/littleplanet";
		Dictionary<string, FShader> shaders = rCam.game.rainWorld.Shaders;
		FShader hologram = shaders["Hologram"], ls = shaders[(!_underWaterMode) ? "FlatLight" : "UnderWaterLight"];
		sLeaser.sprites = new FSprite[]
		{
			new("Futile_White")
			{
				shader = ls,
				scale = _baseRad / 6f
			},
			new(PLANET) { shader = hologram, scale = _baseRad / 400f - _baseRad / 1600f },
			new(RING) { shader = hologram, scale = _baseRad / 400f * 2f },
			new(RING) { shader = hologram, scale = _baseRad / 400f * 3f },
			new(RING) { shader = hologram, scale = _baseRad / 400f * 4f },
			new("Futile_White")
			{
				shader = ls,
				scale = _baseRad / 24f
			}
		};
		AddToContainer(sLeaser, rCam, null!);
	}

	///<inheritdoc/>
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		var sPos = Vector2.Lerp(lastPos, pos, timeStacker);
		float[] lRot = _lastRot, rot = _rot, lSclx = _lastScaleX, sclX = _scaleX;
		var sRot = new SRot()
		{
			A = Mathf.Lerp(lRot[0], rot[0], timeStacker),
			B = Mathf.Lerp(lRot[1], rot[1], timeStacker),
			C = Mathf.Lerp(lRot[2], rot[2], timeStacker),
			D = Mathf.Lerp(lRot[3], rot[3], timeStacker)
		};
		float bRd = _baseRad / 400f, sn0 = Mathf.Sin(Mathf.Lerp(lSclx[0], sclX[0], timeStacker) * Mathf.PI) * bRd;
		var sScaleX = new SScaleX()
		{
			A = sn0 * 2f,
			B = bRd * 3f * Mathf.Cos(Mathf.Lerp(lSclx[1], sclX[1], timeStacker) * Mathf.PI),
			C = sn0 * 4f
		};
		FSprite[] sprites = sLeaser.sprites;
		float x = sPos.x - camPos.x, y = sPos.y - camPos.y;
		for (var i = 0; i < sprites.Length; i++)
		{
			FSprite s = sprites[i];
			s.x = x;
			s.y = y;
			s.alpha = _alpha;
		}
		for (var i = 1; i < sprites.Length - 1; i++)
			sprites[i].rotation = sRot[i - 1];
		for (var i = 2; i < sprites.Length - 1; i++)
		{
			sprites[i].scaleX = sScaleX[i - 2];
			if (sprites[i].scaleX >= bRd * i)
				_increaseRad[i - 2] = false;
			else if (sprites[i].scaleX <= 0f)
				_increaseRad[i - 2] = true;
		}
		sprites[0].alpha /= 2f;
		FShader lShader = rCam.game.rainWorld.Shaders[(!_underWaterMode) ? "FlatLight" : "UnderWaterLight"];
		sprites[0].shader = lShader;
		sprites[5].shader = lShader;
		for (var i = 0; i < sprites.Length; i++)
			sprites[i].color = _color;
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

	///<inheritdoc/>
	public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContainer)
	{
		newContainer = rCam.ReturnFContainer("GrabShaders");
		FSprite[] sprs = sLeaser.sprites;
		for (var i = 0; i < sprs.Length; i++)
		{
			sprs[i].RemoveFromContainer();
			newContainer.AddChild(sprs[i]);
		}
	}

	private ref struct SRot
	{
		public float A, B, C, D;

		public readonly float this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index == 0 ? A : (index == 1 ? B : (index == 2 ? C : D));
		}
	}

	private ref struct SScaleX
	{
		public float A, B, C;

		public readonly float this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index == 0 ? A : (index == 1 ? B : C);
		}
	}

	/// <summary>
	/// DevUI data
	/// </summary>
	public class LittlePlanetData : PlacedObject.ResizableObjectData
	{
		internal float _red = 1f, _green = 1f, _blue = 1f;
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
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(_red, _green, _blue);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		public override string ToString() => SaveUtils.AppendUnrecognizedStringAttrs(SaveState.SetCustomData(this, $"{BaseSaveString()}~Color(~{_red}~{_green}~{_blue}~)~Alpha:~{alpha}~{_panelPos.x}~{_panelPos.y}~Speed:~{speed}"), "~", unrecognizedAttributes);
	}
}

/// <summary>
/// DevUI representation
/// </summary>
public class LittlePlanetRepresentation : ResizeableObjectRepresentation
{
	private sealed class LittlePlanetControlPanel : Panel, IDevUISignals
	{
		private sealed class ControlSlider : Slider
		{
			private LittlePlanet.LittlePlanetData Data
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ((parentNode.parentNode as LittlePlanetRepresentation)!.pObj.data as LittlePlanet.LittlePlanetData)!;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
			List<DevUINode> sb = subNodes;
			sb.Add(new ControlSlider(owner, "ColorR_Slider", this, new(5f, 85f), "Red: "));
			sb.Add(new ControlSlider(owner, "ColorG_Slider", this, new(5f, 65f), "Green: "));
			sb.Add(new ControlSlider(owner, "ColorB_Slider", this, new(5f, 45f), "Blue: "));
			sb.Add(new ControlSlider(owner, "Alpha_Slider", this, new(5f, 25f), "Alpha: "));
			sb.Add(new ControlSlider(owner, "Speed_Slider", this, new(5f, 5f), "Speed: "));
		}

		void IDevUISignals.Signal(DevUISignalType type, DevUINode sender, string message) { }
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
		FSprite spr = fSprites[_linePixelSpriteIndex];
		Vector2 panelPos = _panel.pos;
		spr.scaleY = panelPos.magnitude;
		spr.rotation = AimFromOneVectorToAnother(absPos, _panel.absPos);
		(pObj.data as LittlePlanet.LittlePlanetData)!._panelPos = panelPos;
	}
}
