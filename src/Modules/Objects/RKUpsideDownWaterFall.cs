using DevInterface;
using System.Text.RegularExpressions;
using System.Globalization;

namespace RegionKit.Modules.Objects;

/// <summary>
/// By LB/M4rbleL1ne
/// Upside down waterfall
/// </summary>
internal class UpsideDownWaterFallObject : UpdatableAndDeletable, IDrawable
{
	public FloatRect rect;
	public PlacedObject pObj;
    public float bubbleAmount, bubbleLightness, dripAmount, lastFlow, flow, dripLightness;
	public Vector2 pos;
	public string container;
	public bool cutAtWaterLevel;

	private float InvertX => Data.handlePos.x < 0 ? -1f : 1f;

    private float InvertY => Data.handlePos.y < 0 ? -1f : 1f;

    private UpDownWFData Data => (pObj.data as UpDownWFData)!;

    public IntVector2 IntPos => new((int)(pos.x / 20f), (int)(pos.y / 20f));

	public int IntWidth => (int)(rect.Width() / 20f);

	public UpsideDownWaterFallObject(Room room, PlacedObject pObj, float flow, float bubbleAmount, float bubbleLightness, float dripAmount, float dripLightness, string container, bool cutAtWaterLevel)
    {
        this.room = room;
        this.flow = flow;
        this.pObj = pObj;
        this.bubbleAmount = bubbleAmount;
        this.bubbleLightness = bubbleLightness;
        rect = Data.Rect;
        lastFlow = flow;
        pos = pObj.pos;
        this.dripAmount = dripAmount;
		this.container = container;
		this.cutAtWaterLevel = cutAtWaterLevel;
		this.dripLightness = dripLightness;
    }

	/// <summary>
	/// Warning: will be destroyed if water is inverted in the room
	/// </summary>
	/// <param name="eu"></param>
    public override void Update(bool eu)
	{
		base.Update(eu);
		lastFlow = flow;
		if (room is null || pObj is null || Data is not UpDownWFData d)
			return;
		Vector2 vec = pObj.pos;
        pos = vec;
		FloatRect r = d.Rect;
        if (!rect.EqualsFloatRect(r))
            rect = r;
        flow = d.flow;
        bubbleAmount = d.bubbleAmount;
        bubbleLightness = d.bubbleLightness;
        dripAmount = d.dripAmount;
        dripLightness = d.dripLightness;
        cutAtWaterLevel = d.cutAtWaterLevel;
        int bonus = (IntWidth + (InvertX > 0f ? 1 : 0)) * (int)InvertX, max = Math.Max(IntPos.x + bonus, IntPos.x), min = Math.Min(IntPos.x + bonus, IntPos.x);
        for (var j = min; j < max; j++)
		{
			if (RNG.value < flow)
			{
				if (RNG.value <= bubbleAmount / 5f)
                {
					for (var ick = 0; ick < 4; ick++)
                    {
						Vector2 bubblePos = room.MiddleOfTile(new IntVector2(j, 0)) + new Vector2(RNG.Range(-10f, 10f), rect.top);
						var bubble = new UBubble(bubblePos, Custom.DegToVec(160f + RNG.value * 40f) * RNG.value * 20f, false, true, bubbleLightness, container ?? "Water");
						if (room.water && cutAtWaterLevel && bubblePos.y < room.FloatWaterLevel(bubblePos.x))
							bubble = null;
						if (bubble is not null) 
							room.AddObject(bubble);
					}
				}
				if (RNG.value <= dripAmount / 5f)
				{
					Vector2 dripPos = room.MiddleOfTile(new IntVector2(j, 0)) + new Vector2(RNG.Range(-10f, 10f), rect.top);
					dripPos.y = rect.top;
					var drip = new UWaterDrip(dripPos, new Vector2(dripPos.x - (pos.x + rect.Width() * InvertX / 2f), 0f) * .4f / IntWidth + Custom.DegToVec(-45f + RNG.value * 90f) * RNG.value * 10f, true, dripLightness, container ?? "Water");
					if (room.water && cutAtWaterLevel && dripPos.y < room.FloatWaterLevel(dripPos.x))
						drip = null;
					if (drip is not null)
						room.AddObject(drip);
				}
			}
		}
        if (room.waterInverted is true)
		{
            Destroy();
			Debug.Log("UpsideDownWaterFall object doesn't support InvertedWater!");
        }
    }

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[1]
		{
			new("Futile_White")
			{
				shader = rCam.game.rainWorld.Shaders["WaterFallInverted"],
				scaleX = rect.Width() / 16f * InvertX,
				scaleY = rect.Height() / 16f * InvertY,
				anchorY = 0f,
				anchorX = 0f
			}
		};
		AddToContainer(sLeaser, rCam, rCam.ReturnFContainer(container ?? "Water"));
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		FSprite spr0 = sLeaser.sprites[0];
        spr0.isVisible = true;
		spr0.x = pos.x - camPos.x;
		spr0.scaleX = rect.Width() / 16f * InvertX;
		if (room is not null && room.water && cutAtWaterLevel && rect.bottom - 10f < room.FloatWaterLevel(pos.x))
        {
			spr0.y = Math.Min(pos.y, pos.y + Data?.handlePos.y ?? 0) + (room.FloatWaterLevel(pos.x) - rect.bottom - 10f) - camPos.y;
			spr0.scaleY = (rect.top > room.FloatWaterLevel(pos.x) ? rect.Height() - (room.FloatWaterLevel(pos.x) - rect.bottom - 10f) : 0f) / 16f;
		}
		else
        {
			spr0.y = pos.y - camPos.y;
			spr0.scaleY = rect.Height() / 16f * InvertY;
		}
		spr0.color = new(Mathf.Lerp(lastFlow, flow, timeStacker), 0f, 0f);
		if (Data?.container is string cont && container != cont)
		{
			container = cont;
			FContainer newCont = rCam.ReturnFContainer(container);
			spr0.RemoveFromContainer();
			AddToContainer(sLeaser, rCam, newCont);
		}
		if (slatedForDeletetion || room != rCam.room)
			sLeaser.CleanSpritesAndRemove();
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) => (newContatiner ?? rCam.ReturnFContainer("Water")).AddChild(sLeaser.sprites[0]);
}

internal class UBubble : Bubble
{
	public float lightness;
	public string container;

	public UBubble(Vector2 pos, Vector2 vel, bool bottomBubble, bool fakeWaterBubble, float lightness, string container) : base(pos, vel, bottomBubble, fakeWaterBubble)
	{
		this.lightness = lightness;
		fullSize *= 2.5f;
		this.container = container;
	}

    public override void Update(bool eu)
    {
        base.Update(eu);
		size -= Math.Abs((vel.x + vel.y) / 20f);
		if (size <= 0)
			Destroy();
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        base.ApplyPalette(sLeaser, rCam, palette);
		sLeaser.sprites[0].color = Color.Lerp(Color.Lerp(palette.waterColor1, palette.waterColor2, .3f), Color.white, lightness + RNG.Range(-lightness / 2f, lightness / 2f));
	}

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) => (newContatiner ?? rCam.ReturnFContainer(container ?? "Water")).AddChild(sLeaser.sprites[0]);
}

internal class UWaterDrip : WaterDrip
{
	public float lightness;
	public string container;

	public UWaterDrip(Vector2 pos, Vector2 vel, bool waterColor, float lightness, string container) : base(pos, vel, waterColor)
    {
		this.lightness = lightness;
		this.container = container;
	}

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        base.ApplyPalette(sLeaser, rCam, palette);
		colors[0] = Color.Lerp(colors[0], Color.white, lightness + RNG.Range(-lightness / 2f, lightness / 2f));
		colors[1] = Color.Lerp(colors[0], Color.white, lightness + RNG.Range(-lightness / 2f, lightness / 2f));
	}

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
		newContatiner ??= rCam.ReturnFContainer(container ?? "Water");
		FSprite[] sprites = sLeaser.sprites;
		foreach (FSprite spr in sprites)
		{
			spr.RemoveFromContainer();
			newContatiner.AddChild(spr);
		}
	}
}

internal class UpDownWFData : PlacedObject.Data
{
    public Vector2 handlePos = new(80f, 80f), panelPos;
	public float flow = 1f, bubbleAmount = 5f, bubbleLightness = 1f, dripAmount = 5f, dripLightness = 1f;
	public string container = "Water";
	public bool cutAtWaterLevel = true;

    public virtual FloatRect Rect => new(Mathf.Min(owner.pos.x, owner.pos.x + handlePos.x), Mathf.Min(owner.pos.y, owner.pos.y + handlePos.y), Mathf.Max(owner.pos.x, owner.pos.x + handlePos.x), Mathf.Max(owner.pos.y, owner.pos.y + handlePos.y));

	public UpDownWFData(PlacedObject owner) : base(owner) { }

    public override void FromString(string s)
    {
        var sAr = Regex.Split(s, "~");
        float.TryParse(sAr[0], NumberStyles.Any, CultureInfo.InvariantCulture, out handlePos.x);
        float.TryParse(sAr[1], NumberStyles.Any, CultureInfo.InvariantCulture, out handlePos.y);
        float.TryParse(sAr[2], NumberStyles.Any, CultureInfo.InvariantCulture, out flow);
        float.TryParse(sAr[3], NumberStyles.Any, CultureInfo.InvariantCulture, out bubbleAmount);
		float.TryParse(sAr[4], NumberStyles.Any, CultureInfo.InvariantCulture, out bubbleLightness);
		float.TryParse(sAr[5], NumberStyles.Any, CultureInfo.InvariantCulture, out panelPos.x);
		float.TryParse(sAr[6], NumberStyles.Any, CultureInfo.InvariantCulture, out panelPos.y);
		float.TryParse(sAr[7], NumberStyles.Any, CultureInfo.InvariantCulture, out dripAmount);
		container = sAr[8];
		bool.TryParse(sAr[9], out cutAtWaterLevel);
		float.TryParse(sAr[10], NumberStyles.Any, CultureInfo.InvariantCulture, out dripLightness);
		unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(sAr, 11);
    }

    public override string ToString() => SaveUtils.AppendUnrecognizedStringAttrs($"{handlePos.x}~{handlePos.y}~{flow}~{bubbleAmount}~{bubbleLightness}~{panelPos.x}~{panelPos.y}~{dripAmount}~{container}~{cutAtWaterLevel}~{dripLightness}", "~", unrecognizedAttributes);
}

internal class UpDownWFRepresentation : PlacedObjectRepresentation
{
	private readonly UpDownWFControlPanel _panel;

	private UpDownWFData Data => (pObj.data as UpDownWFData)!;

    public UpDownWFRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
    {
        subNodes.Add(new Handle(owner, "Float_Rect_Handle", this, new(80f, 80f)));
        (subNodes[^1] as Handle)!.pos = Data.handlePos;
        for (var i = 0; i < 5; i++)
        {
            fSprites.Add(new("pixel")
            {
                anchorX = 0f,
                anchorY = 0f
            });
            owner.placedObjectsContainer.AddChild(fSprites[1 + i]);
        }
        fSprites[5].alpha = .05f;
		subNodes.Add(_panel = new(owner, "UpsideDownWaterFall_Panel", this, new(0f, 100f)) { pos = Data.panelPos });
		fSprites.Add(new("pixel") { anchorY = 0f });
		owner.placedObjectsContainer.AddChild(fSprites[^1]);
	}

    public override void Refresh()
    {
        base.Refresh();
		Vector2 camPos = owner.room.game.cameras[0].pos;
        MoveSprite(1, absPos);
        Data.handlePos = (subNodes[0] as Handle)!.pos;
		FloatRect rect = Data.Rect;
        rect.right++;
        rect.top++;
        MoveSprite(1, new Vector2(rect.left, rect.bottom) - camPos);
        fSprites[1].scaleY = rect.Height() + 1f;
        MoveSprite(2, new Vector2(rect.left, rect.bottom) - camPos);
        fSprites[2].scaleX = rect.Width() + 1f;
        MoveSprite(3, new Vector2(rect.right, rect.bottom) - camPos);
        fSprites[3].scaleY = rect.Height() + 1f;
        MoveSprite(4, new Vector2(rect.left, rect.top) - camPos);
        fSprites[4].scaleX = rect.Width() + 1f;
        MoveSprite(5, new Vector2(rect.left, rect.bottom) - camPos);
        fSprites[5].scaleX = rect.Width() + 1f;
        fSprites[5].scaleY = rect.Height() + 1f;
		MoveSprite(6, absPos);
		fSprites[6].scaleY = _panel.pos.magnitude;
		fSprites[6].rotation = AimFromOneVectorToAnother(absPos, _panel.absPos);
		Data.panelPos = _panel.pos;
	}

	public class UpDownWFControlPanel : Panel, IDevUISignals
	{
		public class ControlSlider : Slider
		{
			private UpDownWFData Data => ((parentNode.parentNode as UpDownWFRepresentation)!.pObj.data as UpDownWFData)!;

			public ControlSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title, false, 110f) { }

			public override void Refresh()
			{
				base.Refresh();
				float num;
				switch (IDstring)
				{
				case "Flow_Slider":
					num = Data.flow;
					NumberText = ((int)(100f * num)).ToString() + "%";
					RefreshNubPos(num);
					break;
				case "BubbleAmount_Slider":
					num = Data.bubbleAmount;
					NumberText = Math.Round(num, 1).ToString();
					RefreshNubPos(num / 5f);
					break;
				case "DripAmount_Slider":
					num = Data.dripAmount;
					NumberText = Math.Round(num, 1).ToString();
					RefreshNubPos(num / 5f);
					break;
				case "BubbleLightness_Slider":
					num = Data.bubbleLightness;
					NumberText = ((int)(100f * num)).ToString() + "%";
					RefreshNubPos(num);
					break;
				case "DripLightness_Slider":
					num = Data.dripLightness;
					NumberText = ((int)(100f * num)).ToString() + "%";
					RefreshNubPos(num);
					break;
				}
			}

			public override void NubDragged(float nubPos)
			{
				switch (IDstring)
				{
				case "Flow_Slider":
					Data.flow = nubPos;
					break;
				case "BubbleAmount_Slider":
					Data.bubbleAmount = nubPos * 5f;
					break;
				case "DripAmount_Slider":
					Data.dripAmount = nubPos * 5f;
					break;
				case "BubbleLightness_Slider":
					Data.bubbleLightness = nubPos;
					break;
				case "DripLightness_Slider":
					Data.dripLightness = nubPos;
					break;
				}
				parentNode.parentNode.Refresh();
				Refresh();
			}
		}

		private UpDownWFData Data => ((parentNode as UpDownWFRepresentation)!.pObj.data as UpDownWFData)!;

		public UpDownWFControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new(250f, 145f), "UpsideDownWaterFall")
		{
			subNodes.Add(new ControlSlider(owner, "Flow_Slider", this, new(5f, 125f), "Flow: "));
			subNodes.Add(new ControlSlider(owner, "BubbleAmount_Slider", this, new(5f, 105f), "Bubble Amount: "));
			subNodes.Add(new ControlSlider(owner, "BubbleLightness_Slider", this, new(5f, 85f), "Bubble Lightness: "));
			subNodes.Add(new ControlSlider(owner, "DripAmount_Slider", this, new(5f, 65f), "Drip Amount: "));
			subNodes.Add(new ControlSlider(owner, "DripLightness_Slider", this, new(5f, 45f), "Drip Lightness: "));
			subNodes.Add(new Button(owner, "Container_Button", this, new(5f, 25f), 240f, "Container: " + Data.container));
			subNodes.Add(new Button(owner, "CutAtWaterLevel_Button", this, new(5f, 5f), 240f, "Cut At Water Level: " + Data.cutAtWaterLevel));
		}

		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender.IDstring is "Container_Button")
			{
				switch (Data.container)
				{
				case "Water":
					Data.container = "Foreground";
					break;
				case "Foreground":
					Data.container = "Background";
					break;
				case "Background":
					Data.container = "Water";
					break;
				}
				(sender as Button)!.Text = "Container: " + Data.container;
			}
			else if (sender.IDstring is "CutAtWaterLevel_Button")
			{
				Data.cutAtWaterLevel = !Data.cutAtWaterLevel;
				(sender as Button)!.Text = "Cut At Water Level: " + Data.cutAtWaterLevel;
			}
		}
	}
}
