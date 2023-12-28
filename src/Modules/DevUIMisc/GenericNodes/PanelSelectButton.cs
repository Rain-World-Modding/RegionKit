using DevInterface;
using RegionKit.Modules.Iggy;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

public class PanelSelectButton : Button, IDevUISignals, Modules.Iggy.IGiveAToolTip
{
	public PanelSelectButton(
		DevUI owner,
		string IDstring,
		DevUINode parentNode,
		Vector2 pos,
		float width,
		string text,
		string[] values,
		string panelName)
		: base(
			owner,
			IDstring,
			parentNode,
			pos,
			width,
			text)
	{
		this.values = values;
		itemSelectPanel = null;
		this.panelName = panelName;
	}

	public override void Clicked()
	{
		try
		{
			//sending signal first in case values wanna be changed
			base.Clicked();

			if (itemSelectPanel != null)
			{
				subNodes.Remove(itemSelectPanel);
				itemSelectPanel.ClearSprites();
				itemSelectPanel = null;
			}
			else
			{
				itemSelectPanel = new ItemSelectPanel(owner, this, panelPos - pos, values, IDstring + "_Panel", panelName, panelSize, panelButtonWidth, panelColumns);
				subNodes.Add(itemSelectPanel);
			}
		}
		catch (Exception e) { throw e; }
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		//send signal up
		this.SendSignal(DevUISignalType.ButtonClick, sender, "");

		//remove panel after signal
		if (itemSelectPanel != null && sender.IDstring.StartsWith(itemSelectPanel.idstring + "Button99289_"))
		{
			parentNode.subNodes.Remove(itemSelectPanel);
			itemSelectPanel.ClearSprites();
			itemSelectPanel = null;
		}
	}

	public bool IsSubButtonID(string IDstring, out string subButtonID)
	{
		subButtonID = "";
		if (itemSelectPanel != null && IDstring.StartsWith(itemSelectPanel.idstring + "Button99289_"))
		{
			subButtonID = IDstring.Remove(0, (itemSelectPanel.idstring + "Button99289_").Length);
			return true;
		}

		return false;
	}

	public ItemSelectPanel? itemSelectPanel;

	public string panelName;

	public string[] values;

	public Vector2 panelPos = new Vector2(420f, 280f);

	public Vector2 panelSize = new Vector2(305f, 420f);

	public float panelButtonWidth = 145f;

	public int panelColumns = 2;

	public string itemDescription = "item";

	ToolTip? IGiveAToolTip.ToolTip => new($"Open a panel to select one {itemDescription}", 5, this);

	bool IGeneralMouseOver.MouseOverMe => MouseOver;
}
