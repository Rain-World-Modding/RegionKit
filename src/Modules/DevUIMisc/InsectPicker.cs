using DevInterface;

namespace RegionKit.Modules.DevUIMisc;

internal static class InsectPicker
{
	public static void Apply()
	{
		LogMessage("Applying InsectPicker");
		On.DevInterface.InsectGroupRepresentation.InsectGroupPanel.Signal += InsectGroupPanel_Signal;
	}

	private static void InsectGroupPanel_Signal(On.DevInterface.InsectGroupRepresentation.InsectGroupPanel.orig_Signal orig, DevInterface.InsectGroupRepresentation.InsectGroupPanel self, DevInterface.DevUISignalType type, DevInterface.DevUINode sender, string message)
	{
		// To aoid a CWT I do some strange checks
		if (sender == self.typeButton)
		{
			if (self.HasInsectSelectPanel())
			{
				// Close existing panel

				self.subNodes.Where(x => x is CustomDecalRepresentation.SelectDecalPanel).ToList().ForEach(x => x.ClearSprites());
				self.subNodes.RemoveAll(x => x is CustomDecalRepresentation.SelectDecalPanel);
			}
			else
			{
				// Create new panel
				self.subNodes.Add(new CustomDecalRepresentation.SelectDecalPanel(self.owner, self, new Vector2(200f, 15f) - self.absPos, ExtEnum<CosmeticInsect.Type>.values.entries.ToArray()));
			}
		}
		else
		{
			if (sender.IDstring == "BackPage99289..?/~")
			{
			}
			else if (sender.IDstring == "NextPage99289..?/~")
			{
			}
			else
			{
				((self.parentNode as InsectGroupRepresentation).pObj.data as PlacedObject.InsectGroupData).insectType =
					new CosmeticInsect.Type(sender.IDstring, false);
					
				self.subNodes.Where(x => x is CustomDecalRepresentation.SelectDecalPanel).ToList().ForEach(x => x.ClearSprites());
				self.subNodes.RemoveAll(x => x is CustomDecalRepresentation.SelectDecalPanel);
			}

			self.Refresh();
		}
	}

	private static bool HasInsectSelectPanel(this InsectGroupRepresentation.InsectGroupPanel self)
	{
		return self.subNodes.Any(x => x is CustomDecalRepresentation.SelectDecalPanel);
	}
}