using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

internal class PanelSelectButton : Button, IDevUISignals
{
	public PanelSelectButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text, 
		string[] values, string panelName, Vector2? panelPos = null, Vector2? panelSize = null, float panelButtonWidth = 145f, int panelColumns = 2)
		: base(owner, IDstring, parentNode, pos, width, text)
	{
		this.values = values;
		itemSelectPanel = null;
		this.panelName = panelName;
		this.panelPos = panelPos ?? new Vector2(420f, -400f);
		this.panelSize = panelSize ?? new Vector2(305f, 420f);
		this.panelButtonWidth = panelButtonWidth;
		this.panelColumns = panelColumns;
	}

	public override void Clicked()
	{
		try
		{
			//sending signal first in case values wanna be changed
			base.Clicked();

			if (itemSelectPanel != null)
			{
				Debug.Log("removing panel");
				subNodes.Remove(itemSelectPanel);
				itemSelectPanel.ClearSprites();
				itemSelectPanel = null;
			}
			else
			{
				Debug.Log("adding panel");
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
			Debug.Log("removing panel after signal");
			parentNode.subNodes.Remove(itemSelectPanel);
			itemSelectPanel.ClearSprites();
			itemSelectPanel = null;
		}
	}

	public bool IsSubButtonID(string IDstring, out string subButtonID)
	{
		subButtonID = "";
		Debug.Log($"IDstring [{IDstring}]");
		if (itemSelectPanel != null && IDstring.StartsWith(itemSelectPanel.idstring + "Button99289_"))
		{
			subButtonID = IDstring.Remove(0, (itemSelectPanel.idstring + "Button99289_").Length);
			Debug.Log($"true with [{subButtonID}]");
			return true;
		}

		return false;
	}

	public ItemSelectPanel? itemSelectPanel;

	public string panelName;

	public string[] values;

	public Vector2 panelPos;

	public Vector2 panelSize;

	public float panelButtonWidth;

	public int panelColumns;
}
