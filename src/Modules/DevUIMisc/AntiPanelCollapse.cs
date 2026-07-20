using DevInterface;

namespace RegionKit.Modules.DevUIMisc
{
	internal static class AntiPanelCollapse
	{
		internal static void Apply()
		{
			On.DevInterface.ObjectsPage.Refresh += ObjectsPage_Refresh;
		}

		internal static void Undo()
		{
			On.DevInterface.ObjectsPage.Refresh -= ObjectsPage_Refresh;
		}

		private static void ObjectsPage_Refresh(On.DevInterface.ObjectsPage.orig_Refresh orig, ObjectsPage self)
		{
			if (self.tempNodes is null)
			{
				orig(self);
				return;
			}

			Dictionary<PlacedObject, List<bool>> panelCollapseDict = [];
			foreach (DevUINode? node in self.tempNodes)
			{
				if (node is PlacedObjectRepresentation rep)
				{
					panelCollapseDict.Add(rep.pObj, [.. rep.subNodes.OfType<Panel>().Select(x => x.collapsed)]);
				}
			}
			orig(self);
			foreach (DevUINode? node in self.tempNodes)
			{
				if (node is PlacedObjectRepresentation rep && panelCollapseDict.TryGetValue(rep.pObj, out List<bool> collapsed))
				{
					int index = 0;
					foreach (Panel panel in rep.subNodes.OfType<Panel>())
					{
						if (index >= collapsed.Count)
						{
							break;
						}
						panel.collapsed = collapsed[index++];
						panel.Refresh();
					}
				}
			}
		}
	}
}
