using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;

namespace RegionKit.Modules.IndividualPlacedObjectViewer
{
	internal class ConfirmDeletionPanel : Panel, IDevUISignals
	{
		public ConfirmDeletionPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, string title) : base(owner, IDstring, parentNode, pos, size, title)
		{
			subNodes.Add(new DevUILabel(owner, "Label", this, new Vector2(5f, size.y - 20f), 110f, "Are you sure?"));

			subNodes.Add(new Button(owner, "Confirm_Delete_Button", this, new Vector2(5f, size.y - 60f), 110f, "Yes!"));
			subNodes.Add(new Button(owner, "Cancel_Delete_Button", this, new Vector2(5f, size.y - 80f), 110f, "No wait shit"));
		}

		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			(parentNode as ObjectsPage).Signal(DevUISignalType.ButtonClick, sender, "");
		}
	}
}
