using DevInterface;

namespace RegionKit.Modules.IndividualPlacedObjectViewer;

internal class PlacedObjectsPanel : Panel, IDevUISignals
{
	public PlacedObjectsPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, string title) : base(owner, IDstring, parentNode, pos, size, title)
	{
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		Debug.Log("Button " + sender.IDstring + " was pressed!");
		(parentNode as ObjectsPage).Signal(DevUISignalType.ButtonClick, sender, "");
	}
}
