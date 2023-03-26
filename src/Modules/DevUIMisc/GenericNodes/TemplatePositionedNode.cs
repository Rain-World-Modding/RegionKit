using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

internal class TemplatePositionedNode : PositionedDevUINode
{
	public TemplatePositionedNode(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
	{
		this.IDstring = IDstring;
	}

	public new string IDstring;
}
