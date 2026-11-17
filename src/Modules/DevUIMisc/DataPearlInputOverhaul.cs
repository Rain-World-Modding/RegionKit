using DevInterface;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.DevUIMisc
{
	internal static class DataPearlInputOverhaul
	{
		internal static void Apply()
		{
			On.DevInterface.ConsumableRepresentation.DataPearlControlPanel.ctor += DataPearlControlPanel_ctor;
		}

		internal static void Undo()
		{
			On.DevInterface.ConsumableRepresentation.DataPearlControlPanel.ctor -= DataPearlControlPanel_ctor;
		}

		private static void DataPearlControlPanel_ctor(On.DevInterface.ConsumableRepresentation.DataPearlControlPanel.orig_ctor orig, ConsumableRepresentation.DataPearlControlPanel self, DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string name)
		{
			orig(self, owner, IDstring, parentNode, pos, name);

			self.typeButton.ClearSprites();
			self.subNodes.Remove(self.typeButton);
			self.subNodes.Add(self.typeButton = new PearlSelector(owner, self.typeButton.IDstring, self, self.typeButton.pos, self.typeButton.size.x));
		}

		private class PearlSelector : ButtonWithSearchableSelectPanel
		{
			private ConsumableRepresentation.DataPearlControlPanel Panel => (parentNode as ConsumableRepresentation.DataPearlControlPanel)!;
			private PlacedObject.DataPearlData Data => ((parentNode.parentNode as ConsumableRepresentation)!.pObj.data as PlacedObject.DataPearlData)!;

			public PearlSelector(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width) 
				: base(owner, IDstring, parentNode, pos, width, "", [.. ExtEnum<DataPearl.AbstractDataPearl.DataPearlType>.values.entries], "Select Pearl Type")
			{
				Text = "TYPE";
			}

			public override void OnValueChange(string value)
			{
				Data.pearlType = new DataPearl.AbstractDataPearl.DataPearlType(value);
				Panel.Refresh();
			}
		}
	}
}
