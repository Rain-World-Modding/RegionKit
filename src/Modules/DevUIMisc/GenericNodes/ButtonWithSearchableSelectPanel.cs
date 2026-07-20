using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes
{
	public class ButtonWithSearchableSelectPanel : ButtonWithSelectPanel
	{
		public string panelTitle;
		public string[] values;
		public string actualValue;

		public ButtonWithSearchableSelectPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string defaultValue, string[] values, string panelTitle = "Select Item") : base(owner, IDstring, parentNode, pos, width, defaultValue, SelectPanelMaker)
		{
			this.values = values;
			this.panelTitle = panelTitle;
			actualValue = defaultValue;
		}

		public override void OnValueChange(string value)
		{
			actualValue = value;
			Text = value;
			base.OnValueChange(value);
		}

		public override void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender.IDstring == "BackPage99289..?/~")
			{
				selectPanel.PrevPage();
			}
			else if (sender.IDstring == "NextPage99289..?/~")
			{
				selectPanel.NextPage();
			}
			else if (sender.parentNode == selectPanel && sender.IDstring != "Search99289..?/~")
			{
				if (selectPanel != null)
				{
					subNodes.Remove(selectPanel);
					selectPanel.ClearSprites();
					selectPanel = null;
				}
				OnValueChange(sender.IDstring);
			}
		}

		private static SelectPanel SelectPanelMaker(ButtonWithSelectPanel maker)
		{
			ButtonWithSearchableSelectPanel makerCast = (maker as ButtonWithSearchableSelectPanel)!;
			return new SearchableSelectPanel(maker.owner, "SearchableSelectPanel", maker, new Vector2(250f, 15f) - maker.absPos, makerCast.panelTitle, makerCast.values, makerCast.actualValue);
		}
	}
}
