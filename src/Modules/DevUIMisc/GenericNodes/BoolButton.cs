using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;
using RegionKit.Modules.Iggy;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

internal class BoolButton : Button, Modules.Iggy.IGiveAToolTip
{
	public bool actualValue;

	public string trueText;

	public string falseText;
	public string? toolTipTextOverride;
	public BoolButton(
		DevUI owner,
		string IDstring,
		DevUINode parentNode,
		Vector2 pos,
		float width,
		bool defaultValue,
		string trueText = "true",
		string falseText = "false")
		: base(
			owner,
			IDstring,
			parentNode,
			pos,
			width,
			defaultValue ? trueText : falseText)
	{
		actualValue = defaultValue;
		this.trueText = trueText;
		this.falseText = falseText;
	}


	public override void Clicked()
	{
		actualValue = !actualValue;
		Text = actualValue ? trueText : falseText;
		base.Clicked();
	}
	ToolTip IGiveAToolTip.ToolTip => new ToolTip($"{toolTipTextOverride ?? "Toggles a value."} Current value: {actualValue}", 10, this);

	bool IGeneralMouseOver.MouseOverMe => MouseOver;
}
