using DevInterface;
using RegionKit.Modules.Iggy;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

public class ExtEnumCycler<T> : Button, Modules.Iggy.IGiveAToolTip where T : ExtEnum<T>
{
	public T Type;
	public string? toolTipTextOverride;

	public ExtEnumCycler(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, T defaultValue, string? title = null, float titleWidth = 40f)
		: base(owner, IDstring, parentNode, pos, width, "")
	{
		Type = defaultValue;
		if (title != null)
		{ subNodes.Add(new DevUILabel(owner, title + "_label", this, new Vector2(-(titleWidth + 10f), 0f), titleWidth, title)); }
	}

	public ExtEnumCycler(PositionedDevUINode inheritNode, float width, T defaultValue, string? title = null, float titleWidth = 40f)
		: base(inheritNode.owner, inheritNode.IDstring, inheritNode.parentNode, inheritNode.pos, width, "")
	{
		Type = defaultValue;
		if (title != null)
		{ subNodes.Add(new DevUILabel(owner, title + "_label", this, new Vector2(-(titleWidth + 10f), 0f), titleWidth, title)); }
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
	ToolTip? IGiveAToolTip.ToolTip => new($"{toolTipTextOverride ?? "Selects one option from ExtEnum."} EE type {typeof(T).Name}. Currently selected {Type}. {ExtEnum<T>.values.Count} total options.", 10, this);

	bool IGeneralMouseOver.MouseOverMe => MouseOver;
}
