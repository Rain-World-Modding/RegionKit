using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DevInterface;
using RWCustom;
using UnityEngine;

namespace RegionKit.Modules.Objects;

/// <summary>
/// A small cosmetic hologram
/// </summary>
public class LittlePlanet : CosmeticSprite
{
	/// <summary>
	/// DevUI data
	/// </summary>
	public class LittlePlanetData : PlacedObject.ResizableObjectData
	{
#pragma warning disable 1591
		public float red = 1f;
		public float green = 1f;
		public float blue = 1f;
		public float alpha = 1f;
		public Vector2 panelPos;
		public float speed = 1f;

		public Color Color
		{
			get => new(red, green, blue);
			set
			{
				red = value.r;
				green = value.g;
				blue = value.b;
			}
		}
#pragma warning restore 1591
		///<inheritdoc/>
		public LittlePlanetData(PlacedObject owner) : base(owner) { }
		///<inheritdoc/>
		public override void FromString(string s)
		{
			base.FromString(s);
			var sAr = Regex.Split(s, "~");
			red = float.Parse(sAr[3]);
			green = float.Parse(sAr[4]);
			blue = float.Parse(sAr[5]);
			alpha = float.Parse(sAr[8]);
			panelPos.x = float.Parse(sAr[9]);
			panelPos.y = float.Parse(sAr[10]);
			speed = float.Parse(sAr[12]);
		}
		///<inheritdoc/>
		public override string ToString() => $"{base.ToString()}~Color(~{red}~{green}~{blue}~)~Alpha:~{alpha}~{panelPos.x}~{panelPos.y}~Speed:~{speed}";
	}

	private bool _underWaterMode;
	private Color _color;
	private readonly float _baseRad;
	private float _alpha;
	private float _speed;
	private readonly float[] _lastRot = new float[4];
	private readonly float[] _rot = new float[4];
	private readonly float[] _rotSpeed = new float[4] { -1f, .5f, -.25f, .125f };
	private readonly float[] _lastScaleX = new float[3];
	private readonly float[] _scaleX = new float[3];
	private readonly bool[] _increaseRad = new bool[3];

	private PlacedObject _PObj { get; init; }
	private LittlePlanetData _Data { get; init; }
	///<inheritdoc/>
	public LittlePlanet(Room room, PlacedObject placedObj)
	{
		this.room = room;
		_PObj = placedObj;
		_Data = (placedObj.data as LittlePlanetData)!;
		pos = placedObj.pos;
		lastPos = pos;
		_underWaterMode = room.GetTilePosition(_PObj.pos).y < room.defaultWaterLevel;
		_color = _Data.Color;
		_baseRad = _Data.Rad;
		_alpha = _Data.alpha;
		_speed = _Data.speed;
	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		pos = _PObj.pos;
		_underWaterMode = room.GetTilePosition(_PObj.pos).y < room.defaultWaterLevel;
		_color = _Data.Color;
		_alpha = _Data.alpha;
		_speed = _Data.speed;
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
		foreach (var s in sLeaser.sprites)
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
}
/// <summary>
/// DevUI repr
/// </summary>
public class LittlePlanetRepresentation : ResizeableObjectRepresentation
{
	#pragma warning disable 1591
	public class LittlePlanetControlPanel : Panel, IDevUISignals
	{
		public class ControlSlider : Slider
		{
			LittlePlanet.LittlePlanetData Data { get; init; }

			public ControlSlider(
				DevUI owner,
				string IDstring,
				DevUINode parentNode,
				Vector2 pos,
				string title) : base(
					owner,
					IDstring,
					parentNode,
					pos,
					title,
					false,
					110f)
				=> Data
					= ((parentNode.parentNode as LittlePlanetRepresentation)!.pObj.data as LittlePlanet.LittlePlanetData)!;

			public override void Refresh()
			{
				base.Refresh();
				var num = 0f;
				switch (IDstring)
				{
				case "ColorR_Slider":
					num = Data.red;
					NumberText = ((int)(255f * num)).ToString();
					break;
				case "ColorG_Slider":
					num = Data.green;
					NumberText = ((int)(255f * num)).ToString();
					break;
				case "ColorB_Slider":
					num = Data.blue;
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
					Data.red = nubPos;
					break;
				case "ColorG_Slider":
					Data.green = nubPos;
					break;
				case "ColorB_Slider":
					Data.blue = nubPos;
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

	public LittlePlanetRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, pObj.type.ToString(), true)
	{
		_panel = new(owner, "LittlePlanet_Panel", this, new(0f, 100f));
		subNodes.Add(_panel);
		_panel.pos = (pObj.data as LittlePlanet.LittlePlanetData)!.panelPos;
		fSprites.Add(new("pixel"));
		_linePixelSpriteIndex = fSprites.Count - 1;
		owner.placedObjectsContainer.AddChild(fSprites[_linePixelSpriteIndex]);
		fSprites[_linePixelSpriteIndex].anchorY = 0f;
	}

	public override void Refresh()
	{
		base.Refresh();
		MoveSprite(_linePixelSpriteIndex, absPos);
		fSprites[_linePixelSpriteIndex].scaleY = _panel.pos.magnitude;
		fSprites[_linePixelSpriteIndex].rotation = Custom.AimFromOneVectorToAnother(absPos, _panel.absPos);
		(pObj.data as LittlePlanet.LittlePlanetData)!.panelPos = _panel.pos;
	}
}

public static class LittlePlanetExtensions
{
	public static void AddChild(this FContainer self, params FNode[] nodes)
	{
		foreach (var node in nodes)
			self.AddChild(node);
	}

	public static void RemoveFromContainer(this FNode[] self)
	{
		foreach (var node in self)
			node.RemoveFromContainer();
	}
}


public static class EmbeddedResourceLoader
{
	public static void LoadEmbeddedResource(string name)
	{
		__logger.LogDebug($"Loading ER {name}");
		var thisAssembly = Assembly.GetExecutingAssembly();
		//var resourceName = thisAssembly.GetManifestResourceNames().First(r => r.Contains(name));

		using Stream resource = _Assets.GetStream("LittlePlanet", $"{name}.png")!;
		using MemoryStream memoryStream = new();
		var buffer = new byte[16384];
		int count;
		while ((count = resource!.Read(buffer, 0, buffer.Length)) > 0)
			memoryStream.Write(buffer, 0, count);
		Texture2D spriteTexture = new(0, 0, TextureFormat.ARGB32, false);
		//TODO: check if loads correctly
		spriteTexture.LoadImage(memoryStream.ToArray());
		spriteTexture.anisoLevel = 1;
		spriteTexture.filterMode = 0;
		FAtlas atlas = new(name, spriteTexture, FAtlasManager._nextAtlasIndex, false);
		Futile.atlasManager.AddAtlas(atlas);
		FAtlasManager._nextAtlasIndex++;
	}
	#pragma warning restore 1591
}
