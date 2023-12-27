using DevInterface;
using RegionKit.Modules.Iggy;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

public class ItemSelectPanel : Panel, IDevUISignals, Modules.Iggy.IGiveAToolTip
{
	public ItemSelectPanel(
		DevUI owner,
		DevUINode parentNode,
		Vector2 pos,
		string[] items,
		string idstring,
		string title,
		Vector2 size = default,
		float buttonWidth = 145f,
		int columns = 2)
		: base(
			owner,
			idstring,
			parentNode,
			pos,
			size == default ? new Vector2(305f, 420f) : size,
			title)
	{
		this.items = items;
		this.idstring = idstring;
		this.buttonWidth = buttonWidth;
		this.columns = columns;

		currentOffset = 0;
		perpage = (int)((this.size.y - 60f) / 20f * columns);
		PopulateItems(currentOffset);
	}
	public void PopulateItems(int offset)
	{
		currentOffset = offset;
		foreach (DevUINode devUINode in subNodes)
		{
			devUINode.ClearSprites();
		}

		subNodes.Clear();
		var intVector = new IntVector2(0, 0);
		int num = currentOffset;
		while (num < items.Length && num < currentOffset + perpage)
		{
			Button currentOption = new Button(owner, idstring + "Button99289_" + items[num], this, new Vector2(5f + intVector.x * (buttonWidth + 5f), size.y - 25f - 20f * intVector.y), buttonWidth, items[num]);
			string currentItem = items[num]
			API.Iggy.AddTooltip(currentOption, () => new($"Select {currentItem}", 10, currentOption));
			subNodes.Add(currentOption);
			intVector.y++;
			if (intVector.y >= (int)Mathf.Floor(perpage / columns))
			{
				intVector.x++;
				intVector.y = 0;
			}
			num++;
		}

		float pageButtonWidth = (size.x - 15f) / 2f;

		subNodes.Add(new Button(owner, idstring + "BackPage99289..?/~", this, new Vector2(5f, size.y - 25f - 20f * (perpage / columns + 1f)), pageButtonWidth, "Previous"));
		subNodes.Add(new Button(owner, idstring + "NextPage99289..?/~", this, new Vector2(size.x - 5f - pageButtonWidth, size.y - 25f - 20f * (perpage / columns + 1f)), pageButtonWidth, "Next"));
	}


	public void PrevPage()
	{
		currentOffset -= perpage;
		if (currentOffset < 0)
		{
			currentOffset = 0;
		}
		PopulateItems(currentOffset);
	}


	public void NextPage()
	{
		currentOffset += perpage;
		if (currentOffset > items.Length)
		{
			currentOffset = perpage * (int)Mathf.Floor(items.Length / (float)perpage);
		}
		PopulateItems(currentOffset);
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		//can't modify subnodes during Signal, as it is called during a loop through all subnodes
		if (sender.IDstring == idstring + "BackPage99289..?/~")
		{ goPrev = true; }

		else if (sender.IDstring == idstring + "NextPage99289..?/~")
		{ goNext = true; }

		else { this.SendSignal(type, sender, message); }
	}

	public override void Update()
	{
		if (goNext)
		{ NextPage(); goNext = false; }

		if (goPrev)
		{ PrevPage(); goPrev = false; }

		base.Update();
	}

	private bool goNext = false;

	private bool goPrev = false;

	public string idstring;

	private float buttonWidth;

	private int columns;

	private int perpage;

	private int currentOffset;

	private string[] items;

	//iggy
	public string? toolTipTextOverride;

	ToolTip? IGiveAToolTip.ToolTip => new(toolTipTextOverride ?? "Select one item from the options", 5, this);

	bool IGeneralMouseOver.MouseOverMe => MouseOver;
}
