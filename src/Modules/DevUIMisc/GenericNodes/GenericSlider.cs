using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

public class GenericSlider : Slider, IDevUISignals
{
	public float defaultValue = 0f;

	public float actualValue;

	public float minValue = 0f;

	public float maxValue = 1f;

	bool stringControl;

	float stringWidth;
	public GenericSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, bool inheritButton, float titleWidth, float? initialValue = null, bool stringControl = true, float stringWidth = 16) : base(owner, IDstring, parentNode, pos, title, inheritButton, titleWidth)
	{
		actualValue = (float)((initialValue != null) ? initialValue : defaultValue);
		this.stringControl = stringControl;
		this.stringWidth = stringWidth;
		if (stringControl)
		{
			subNodes[1].ClearSprites();
			subNodes[1] = new StringControl(owner, IDstring, this, new Vector2(titleWidth + 10f, 0f), inheritButton ? stringWidth + 26f : stringWidth, actualValue.ToString(), StringControl.TextIsFloat);

			if (inheritButton && subNodes[2] is PositionedDevUINode node)
			{ node.Move(new Vector2(node.pos.x + (stringWidth - 16f), node.pos.y)); }
		}
	}
	private new float SliderStartCoord
	{
		get
		{
			if (!inheritButton)
			{
				return titleWidth + 10f + stringWidth + 4f;
			}
			return titleWidth + 10f + 26f + stringWidth + 4f + 34f;
		}
	}

	public SliderNub sliderNub => subNodes[inheritButton ? 3 : 2] as SliderNub;
	public override void Update()
	{
		base.Update();
		if (owner != null && sliderNub.held)
		{
			NubDragged(Mathf.InverseLerp(absPos.x + SliderStartCoord, absPos.x + SliderStartCoord + 92f, owner.mousePos.x + sliderNub.mousePosOffset));
		}
	}

	public new void RefreshNubPos(float nubPos)
	{
		sliderNub.Move(new Vector2(Mathf.Lerp(SliderStartCoord, SliderStartCoord + 92f, nubPos), 0f));
	}

	public override void Refresh()
	{
		base.Refresh();

		string str = "";
		if (actualValue == defaultValue) str = "<D>";
		NumberText = str + " " + ((int)actualValue).ToString();
		if (stringControl)
		{ (subNodes[1] as StringControl)!.actualValue = actualValue.ToString(); }

		RefreshNubPos(Mathf.Clamp(Mathf.InverseLerp(minValue, maxValue, actualValue), 0f, 1f));
		MoveSprite(0, absPos + new Vector2(SliderStartCoord, 0f));
		MoveSprite(1, absPos + new Vector2(SliderStartCoord, 7f));
	}

	public override void NubDragged(float nubPos)
	{
		actualValue = Mathf.Lerp(minValue, maxValue, nubPos);
		Refresh();

		this.SendSignal(SliderUpdate, this, "");
	}

	public override void ClickedResetToInherent()
	{
		actualValue = defaultValue;
		Refresh();
	}

	public new void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (sender.IDstring == "Inherit_Button")
		{
			ClickedResetToInherent();
		}

		else if (stringControl && sender.IDstring == IDstring && type == StringControl.StringFinish)
		{
			actualValue = float.Parse((subNodes[1] as StringControl)!.actualValue);
			Refresh();
		}

		this.SendSignal(SliderUpdate, this, "");
	}

	public static readonly DevUISignalType SliderUpdate = new DevUISignalType("SliderUpdate", true);
}
