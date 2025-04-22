using System.Globalization;
using System.Text.RegularExpressions;
using DevInterface;


namespace RegionKit.Modules.Objects;

/// <summary>
/// A rainbow that always spawns and persists throughout the cycle
/// </summary>
[Obsolete("RainbowNoFade is included in the game as of 1.10")]
internal class RainbowNoFade : CosmeticSprite
{
	public RainbowNoFade(Room room, PlacedObject placedObject)
	{
		this.room = room;
		this.placedObject = placedObject;
		Futile.atlasManager.LoadAtlasFromTexture("rainbow", Resources.Load("Atlases/rainbow") as Texture2D, false);
		Refresh();
		alwaysShow = true;
	}

	private RainbowNoFade.RainbowNoFadeData _RBData
	{
		get
		{
			return (placedObject.data as RainbowNoFade.RainbowNoFadeData)!;
		}
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		if (alwaysShow)
		{
			_fade = 1f;
		}
		else
		{
			_fade = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(Mathf.Pow(room.world.rainCycle.CycleStartUp, 0.75f) * 3.14159274f)), 0.6f);
			if (room.world.rainCycle.CycleStartUp >= 1f)
			{
				Destroy();
			}
		}
	}

	public void Refresh()
	{
		pos = placedObject.pos - _RBData.handlePos;
		_rad = _RBData.handlePos.magnitude * 2f;
	}

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new CustomFSprite("rainbow");
		sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Rainbow"];
		AddToContainer(sLeaser, rCam, null);
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		float num = _fade * Mathf.InverseLerp(0.2f, 0f, rCam.ghostMode);
		for (int i = 0; i < 4; i++)
		{
			(sLeaser.sprites[0] as CustomFSprite)!.MoveVertice(i, pos + Custom.eightDirections[1 + i * 2].ToVector2() * _rad - camPos);
			(sLeaser.sprites[0] as CustomFSprite)!.verticeColors[i] = new Color(_RBData.fades[4], 0f, 0f, num * _RBData.fades[i]);
		}
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
	}

	public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		newContatiner ??= rCam.ReturnFContainer("GrabShaders");
		base.AddToContainer(sLeaser, rCam, newContatiner);
	}

	public PlacedObject placedObject;
	private float _rad;
	private float _fade;
	public bool alwaysShow;

	public class RainbowNoFadeData : PlacedObject.ResizableObjectData
	{
		public RainbowNoFadeData(PlacedObject owner) : base(owner)
		{
			fades = new float[6];
			for (int i = 0; i < 4; i++)
			{
				fades[i] = 1f;
			}
			fades[4] = 0.5f;
			fades[5] = 0.15f;
		}
		public float Chance
		{
			get
			{
				return fades[5];
			}
		}
		public override void FromString(string s)
		{
			string[] array = Regex.Split(s, "~");
			handlePos.x = float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
			handlePos.y = float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
			panelPos.x = float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
			panelPos.y = float.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture);
			string[] array2 = array[4].Split(array[4].Contains('|')? '|' : ','); //it'll still parse the old delimiter if it's present
			int num = 0;
			while (num < fades.Length && num < array2.Length)
			{
				fades[num] = float.Parse(array2[num], NumberStyles.Any, CultureInfo.InvariantCulture);
				num++;
			}
		}
		public override string ToString()
		{
			string text = string.Empty;
			for (int i = 0; i < fades.Length; i++)
			{
				text += string.Format(CultureInfo.InvariantCulture, "{0}{1}", new object[]
				{
					fades[i],
					(i >= fades.Length - 1) ? string.Empty : "|"
				});
			}
			return string.Format(CultureInfo.InvariantCulture, "{0}~{1}~{2}~{3}~{4}", new object[]
			{
				handlePos.x,
				handlePos.y,
				panelPos.x,
				panelPos.y,
				text
			});
		}
		public Vector2 panelPos;
		public float[] fades;
	}
}

internal class RainbowNoFadeRepresentation : ResizeableObjectRepresentation
{
	private RainbowNoFadeControlPanel controlPanel;
	public RainbowNoFadeRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, "RainbowNoFade", false)
	{
		subNodes.Add(controlPanel = new RainbowNoFadeControlPanel(owner, "RainbowNoFade_Panel", this, new Vector2(0f, 100f)));
		controlPanel.pos = (pObj.data as RainbowNoFade.RainbowNoFadeData)!.panelPos;
		fSprites.Add(new FSprite("pixel", true));
		_lineSprite = fSprites.Count - 1;
		owner.placedObjectsContainer.AddChild(fSprites[_lineSprite]);
		fSprites[_lineSprite].anchorY = 0f;
		for (int i = 0; i < owner.room.updateList.Count; i++)
		{
			if (owner.room.updateList[i] is RainbowNoFade && (owner.room.updateList[i] as RainbowNoFade)!.placedObject == pObj)
			{
				_RainbowNoFade = (owner.room.updateList[i] as RainbowNoFade)!;
				break;
			}
		}
		if (_RainbowNoFade == null)
		{
			_RainbowNoFade = new RainbowNoFade(owner.room, pObj);
			owner.room.AddObject(_RainbowNoFade);
		}
		_RainbowNoFade.alwaysShow = true;
	}

	public override void Refresh()
	{
		base.Refresh();
		MoveSprite(_lineSprite, absPos);
		fSprites[_lineSprite].scaleY = (subNodes[1] as RainbowNoFadeRepresentation.RainbowNoFadeControlPanel)!.pos.magnitude;
		fSprites[_lineSprite].rotation = Custom.AimFromOneVectorToAnother(absPos, (subNodes[1] as RainbowNoFadeRepresentation.RainbowNoFadeControlPanel)!.absPos);
		_RainbowNoFade.Refresh();
		(pObj.data as RainbowNoFade.RainbowNoFadeData)!.panelPos = (subNodes[1] as Panel)!.pos;
	}

	private int _lineSprite;
	private RainbowNoFade _RainbowNoFade;

	public class RainbowNoFadeControlPanel : Panel
	{
		public RainbowNoFadeControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 125f), "RainbowNoFade")
		{
			for (int i = 0; i < 4; i++)
			{
				subNodes.Add(new FadeSlider(owner, "Fade_Slider", this, new Vector2(5.01f, 5f + 20f * (float)i), "Fade " + i + ":", i));
			}
			subNodes.Add(new FadeSlider(owner, "Thick_Slider", this, new Vector2(5.01f, 85f), "Thickness:", 4));
			subNodes.Add(new FadeSlider(owner, "Chance_Slider", this, new Vector2(5.01f, 105f), "Per cycle chance:", 5));
		}
		public RainbowNoFade.RainbowNoFadeData RainbowNoFadeData
		{
			get
			{
				return ((parentNode as RainbowNoFadeRepresentation)!.pObj.data as RainbowNoFade.RainbowNoFadeData)!;
			}
		}
		public class FadeSlider : Slider
		{
			public FadeSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, int index) : base(owner, IDstring, parentNode, pos, title, false, 110f)
			{
				_index = index;
			}
			public RainbowNoFade.RainbowNoFadeData RainbowNoFadeData
			{
				get
				{
					return ((parentNode.parentNode as RainbowNoFadeRepresentation)!.pObj.data as RainbowNoFade.RainbowNoFadeData)!;
				}
			}
			public override void Refresh()
			{
				base.Refresh();
				NumberText = Mathf.RoundToInt(RainbowNoFadeData.fades[_index] * 100f).ToString() + "%";
				RefreshNubPos(RainbowNoFadeData.fades[_index]);
			}
			public override void NubDragged(float nubPos)
			{
				RainbowNoFadeData.fades[_index] = nubPos;
				parentNode.parentNode.Refresh();
				Refresh();
			}
			private int _index;
		}
	}
}



