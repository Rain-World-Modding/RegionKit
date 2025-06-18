using HUD;
using Watcher;

namespace RegionKit.Modules.EchoExtender
{
	public class EESpinningTopConversation : SpinningTop.SpinningTopConversation
	{
		public EESpinningTopConversation(ID id, Ghost ghost, DialogBox dialogBox) : base(id, ghost, dialogBox)
		{
		}

		public override void AddEvents()
		{
			base.AddEvents();
		}
	}
}
