using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

public class RGBSelectButton : Button, IDevUISignals
{
	bool recolor = true;
	bool hexDisplay = true;
	public RGBSelectButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text, 
		Color color, string panelName)
		: base(owner, IDstring, parentNode, pos, width, text)
	{
		actualValue = color;
		RGBSelectPanel = null;
		this.panelName = panelName;

		if (hexDisplay)
		{ Text = colorToHex(actualValue); }
	}

	public override void Update()
	{
		base.Update();

		if (recolor)
		{ fSprites[0].color = Color.Lerp(actualValue, colorA, 0.5f); }
	}

	public override void Clicked()
	{
		//sending signal first in case values wanna be changed
		base.Clicked();

		if (RGBSelectPanel != null)
		{
			subNodes.Remove(RGBSelectPanel);
			RGBSelectPanel.ClearSprites();
			RGBSelectPanel = null;
		}
		else
		{
			RGBSelectPanel = new RGBSelectPanel(owner, this, panelPos - pos, IDstring + "_Panel", panelName, actualValue);
			subNodes.Add(RGBSelectPanel);
		}
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (RGBSelectPanel != null && sender == RGBSelectPanel)
		{
			actualValue = RGBSelectPanel.actualValue;
			if (hexDisplay)
			{ Text = colorToHex(actualValue); }
		}

		//send signal up
		this.SendSignal(DevUISignalType.ButtonClick, this, message);
	}

	public RGBSelectPanel? RGBSelectPanel;

	public string panelName;

	public Color actualValue;

	public Vector2 panelPos = new Vector2(420f, 280f);
}
