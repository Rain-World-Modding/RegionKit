using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

public class GenericSlider : Slider, IDevUISignals
{
	float defaultValue;

	public float actualValue;

	float minValue;

	float maxValue;

	bool stringControl;
	public GenericSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, bool inheritButton, float titleWidth, float defaultValue = 0f, float minValue = 0f, float maxValue = 1, float? initialValue = null, bool stringControl = true) : base(owner, IDstring, parentNode, pos, title, inheritButton, titleWidth)
	{
		this.defaultValue = defaultValue;
		this.actualValue = (float)((initialValue != null) ? initialValue : defaultValue);
		this.minValue = minValue;
		this.maxValue = maxValue;
		this.stringControl = stringControl;
		if (stringControl)
		{
			subNodes[1] = new StringControl(owner, IDstring, this, new Vector2(titleWidth + 10f, 0f), inheritButton ? 42f : 16f, actualValue.ToString(), StringControl.TextIsFloat);
		}
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
