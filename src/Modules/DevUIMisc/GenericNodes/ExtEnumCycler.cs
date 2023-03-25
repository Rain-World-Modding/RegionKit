using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

public class ExtEnumCycler<T> : Button where T : ExtEnum<T>
{
	public T Type;
	public ExtEnumCycler(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, T defaultValue, string title = "Title", bool label = true) : base(owner, IDstring, parentNode, pos, width, "")
	{
		Type = defaultValue;
		if (label)
		{ subNodes.Add(new DevUILabel(owner, title + "_label", this, new Vector2(-50f, 0f), 40f, title)); }
	}

	public ExtEnumCycler(PositionedDevUINode inheritNode, float width, T defaultValue, string title = "Title", bool label = true)
		: base(inheritNode.owner, inheritNode.IDstring, inheritNode.parentNode, inheritNode.pos, width, "")
	{
		Type = defaultValue;
		if (label)
		{ subNodes.Add(new DevUILabel(owner, title + "_label", this, new Vector2(-50f, 0f), 40f, title)); }
	}

	public override void Refresh()
	{
		base.Refresh();
		string text = "";
		/*if (!RoomSettings.parent.isAncestor && RoomSettings.parent.dType != null)
		{
			text = "<T>";
		}
		else
		{
			text = "<A>";
		}*/
		string str = text;
		string str2 = " ";
		Text = str + str2 + (Type?.ToString());
	}

	public override void Clicked()
	{
		if (Type == null)
		{
			Type = (T)ExtEnumBase.Parse(typeof(T), ExtEnum<T>.values.GetEntry(0), false);
		}
		else
		{ Type = (T)ExtEnumBase.Parse(typeof(T), Increment(), false); }

		Refresh();
		base.Clicked();
	}

	private string Increment()
	{
		int i = (Type.Index + 1) % ExtEnum<T>.values.Count;
		return ExtEnum<T>.values.GetEntry(i);
	}
}
