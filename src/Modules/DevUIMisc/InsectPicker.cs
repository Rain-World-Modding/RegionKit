using DevInterface;

namespace RegionKit.Modules.DevUIMisc;

internal static class InsectPicker
{
	public static void Apply()
	{
		On.DevInterface.InsectGroupRepresentation.InsectGroupPanel.Signal += InsectGroupPanel_Signal;
	}

	public static void Undo()
	{
		On.DevInterface.InsectGroupRepresentation.InsectGroupPanel.Signal -= InsectGroupPanel_Signal;
	}

	private static void InsectGroupPanel_Signal(On.DevInterface.InsectGroupRepresentation.InsectGroupPanel.orig_Signal orig, DevInterface.InsectGroupRepresentation.InsectGroupPanel self, DevInterface.DevUISignalType type, DevInterface.DevUINode sender, string message)
	{
		// To avoid a CWT I do some scary LINQ

		var selectPanel = self.GetSelectPanel();

		if (sender == self.typeButton)
		{
			if (selectPanel != null)
			{
				// Close existing panel
				self.subNodes.Remove(selectPanel);
				selectPanel.ClearSprites();
			}
			else
			{
				// Create new panel
				var newSelectPanel = new CustomDecalRepresentation.SelectDecalPanel(self.owner, self, new Vector2(200f, 15f) - self.absPos, ExtEnum<CosmeticInsect.Type>.values.entries.ToArray());
				newSelectPanel.fLabels[0].text = "Select Insect Type";
				self.subNodes.Add(newSelectPanel);
			}
		}
		else
		{
			if (sender.IDstring == "BackPage99289..?/~")
			{
				selectPanel?.PrevPage();
			}
			else if (sender.IDstring == "NextPage99289..?/~")
			{
				selectPanel?.NextPage();
			}
			else
			{
				((self.parentNode as InsectGroupRepresentation).pObj.data as PlacedObject.InsectGroupData).insectType =
					new CosmeticInsect.Type(sender.IDstring, false);

				self.subNodes.Remove(selectPanel);
				selectPanel?.ClearSprites();
			}

			self.Refresh();
		}
	}

	private static CustomDecalRepresentation.SelectDecalPanel? GetSelectPanel(this InsectGroupRepresentation.InsectGroupPanel self)
	{
		return self.subNodes.FirstOrDefault(x => x is CustomDecalRepresentation.SelectDecalPanel) as CustomDecalRepresentation.SelectDecalPanel;
	}
}