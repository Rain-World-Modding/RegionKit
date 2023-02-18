using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc;

public class ItemSelectPanel : Panel
{
	public ItemSelectPanel(DevUI owner, DevUINode parentNode, Vector2 pos, string[] items, string idstring, string title, Vector2 size = default) :
		base(owner, idstring, parentNode, pos, size == default ? new Vector2(305f, 420f) : size, title)
	{
		this.items = items;
		this.idstring = idstring;

		currentOffset = 0;
		perpage = 36;
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
		IntVector2 intVector = new IntVector2(0, 0);
		int num = currentOffset;
		while (num < items.Length && num < currentOffset + perpage)
		{
			subNodes.Add(new Button(owner, idstring + "Button99289_" + items[num], this, new Vector2(5f + (float)intVector.x * 150f, size.y - 25f - 20f * (float)intVector.y), 145f, items[num]));
			intVector.y++;
			if (intVector.y >= (int)Mathf.Floor((float)this.perpage / 2f))
			{
				intVector.x++;
				intVector.y = 0;
			}
			num++;
		}
		subNodes.Add(new Button(owner, idstring + "BackPage99289..?/~", this, new Vector2(5f, size.y - 25f - 20f * ((float)(perpage / 2) + 1f)), 145f, "Previous"));
		subNodes.Add(new Button(owner, idstring + "NextPage99289..?/~", this, new Vector2(155f, size.y - 25f - 20f * ((float)(perpage / 2) + 1f)), 145f, "Next"));
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
			currentOffset = perpage * (int)Mathf.Floor((float)items.Length / (float)perpage);
		}
		PopulateItems(currentOffset);
	}

	public string idstring;

	private int perpage;

	private int currentOffset;

	private string[] items;
}
