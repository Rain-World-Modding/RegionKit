using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

public class GenericSlider : Slider, IDevUISignals
{
	public float defaultValue = 0f;

	public float actualValue;

	public float minValue = 0f;

	public float maxValue = 1f;

	public int valueRounding = -1;

	public int displayRounding = 0;

	bool stringControl;

	float stringWidth; 

	public float Width => 92f + SliderStartCoord;

    public event OnValueChangedHandler? OnValueChanged;

	public GenericSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, bool inheritButton, float titleWidth, float? initialValue, float? minValue, float? maxValue, bool stringControl = true, float stringWidth = 16) 
		: base(owner, IDstring, parentNode, pos, title, inheritButton, titleWidth)
	{
		if (minValue != null) this.minValue = minValue.Value;
		if (maxValue != null) this.maxValue = maxValue.Value;
		defaultValue = this.minValue;
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
		else
		{
			var oldLabel = (subNodes[1] as DevUILabel)!;
			oldLabel.size.x = stringWidth;
			oldLabel.fSprites[0].scaleX = stringWidth;
		}
	}
	public GenericSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, bool inheritButton, float titleWidth, float? initialValue = null, bool stringControl = true, float stringWidth = 16) 
		: this(owner, IDstring, parentNode, pos, title, inheritButton, titleWidth, initialValue, null, null, stringControl, stringWidth)
	{
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

	public SliderNub sliderNub => (SliderNub)subNodes[inheritButton ? 3 : 2];
	public override void Update()
	{
		base.Update();
		if (owner != null && sliderNub.held)
		{
			NubDragged2(Mathf.InverseLerp(absPos.x + SliderStartCoord, absPos.x + SliderStartCoord + 92f, owner.mousePos.x + sliderNub.mousePosOffset));
		}
	}

	public new void RefreshNubPos(float nubPos)
	{
		sliderNub.Move(new Vector2(Mathf.Lerp(SliderStartCoord, SliderStartCoord + 92f, nubPos), 0f));
	}

	public override void Refresh()
	{
		base.Refresh();

		if (valueRounding >= 0)
		{ 
			actualValue = (float)Math.Round(actualValue, 2); 
		}

		string str = "";
		if (actualValue == defaultValue && inheritButton) str = "<D>";
		str += " ";
		if (displayRounding >= 0) str += Math.Round(actualValue, displayRounding).ToString();
		else str += actualValue.ToString();

		NumberText = str;
		if (stringControl)
		{ (subNodes[1] as StringControl)!.actualValue = actualValue.ToString(); }

		RefreshNubPos(Mathf.InverseLerp(minValue, maxValue, actualValue));
		MoveSprite(0, absPos + new Vector2(SliderStartCoord, 0f));
		MoveSprite(1, absPos + new Vector2(SliderStartCoord, 7f));
	}


	public void NubDragged2(float nubPos)
	{
		float oldValue = actualValue;
		actualValue = Mathf.Lerp(minValue, maxValue, nubPos);
		Refresh();

		this.SendSignal(SliderUpdate, this, "");
		OnValueChanged?.Invoke(actualValue, oldValue);
	}

	public override void NubDragged(float nubPos)
	{
		//do nothing so evil vanilla NubDragged with bad math doesn't give the wrong value
	}

	public override void ClickedResetToInherent()
	{
		float oldValue = actualValue;
		actualValue = defaultValue;
		Refresh();
		OnValueChanged?.Invoke(actualValue, oldValue);
	}

	public new void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (sender.IDstring == "Inherit_Button")
		{
			ClickedResetToInherent();
		}

		else if (stringControl && sender.IDstring == IDstring && type == StringControl.StringFinish)
		{
			float oldValue = actualValue;
			actualValue = float.Parse((subNodes[1] as StringControl)!.actualValue);
			Refresh();
			OnValueChanged?.Invoke(actualValue, oldValue);
		}

		this.SendSignal(SliderUpdate, this, "");
	}

	public static readonly DevUISignalType SliderUpdate = new DevUISignalType("SliderUpdate", true);
	public delegate void OnValueChangedHandler(float value, float oldValue);
}
