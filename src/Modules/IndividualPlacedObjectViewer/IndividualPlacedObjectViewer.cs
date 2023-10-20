using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.IndividualPlacedObjectViewer;

internal static class IndividualPlacedObjectViewer
{
	private static readonly ConditionalWeakTable<ObjectsPage, ObjectsPageData> objectsPageCWT = new ConditionalWeakTable<ObjectsPage, ObjectsPageData>();
	private const int MaxPlacedObjectsPerPage = 16;

	private class ObjectsPageData
	{
		public bool isInIndividualMode = false;
		public List<int> visiblePlacedObjectsIndexes = new List<int>();
		public int placedObjectsPage = 0;
		public int typePage = 0;

		public PlacedObject.Type? sortingType = null;

		public ObjectsPageData() { }
	}

	private static ObjectsPageData GetData(this ObjectsPage objectsPage)
	{
		if (!objectsPageCWT.TryGetValue(objectsPage, out var objectsPageData))
		{
			objectsPageData = new ObjectsPageData();
			objectsPageCWT.Add(objectsPage, objectsPageData);
		}

		return objectsPageData;
	}

	public static void Enable()
	{
		On.DevInterface.ObjectsPage.ctor += ObjectsPage_ctor;
		On.DevInterface.ObjectsPage.Signal += ObjectsPage_Signal;
		On.DevInterface.ObjectsPage.RemoveObject += ObjectsPage_RemoveObject;

		On.DevInterface.PlacedObjectRepresentation.Update += PlacedObjectRepresentation_Update;

		IL.DevInterface.ObjectsPage.Refresh += IL_ObjectsPage_Refresh;
	}

	public static void Disable()
	{
		On.DevInterface.ObjectsPage.ctor -= ObjectsPage_ctor;
		On.DevInterface.ObjectsPage.Signal -= ObjectsPage_Signal;

		On.DevInterface.PlacedObjectRepresentation.Update -= PlacedObjectRepresentation_Update;

		IL.DevInterface.ObjectsPage.Refresh -= IL_ObjectsPage_Refresh;
	}

	private static void ObjectsPage_ctor(On.DevInterface.ObjectsPage.orig_ctor orig, ObjectsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
	{
		orig(self, owner, IDstring, parentNode, name);

		self.subNodes.Add(new Button(owner, "Switch_Mode_Button", self, new Vector2(180f, 710f), 95f, "Switch mode"));
	}

	private static void ObjectsPage_Signal(On.DevInterface.ObjectsPage.orig_Signal orig, ObjectsPage self, DevUISignalType type, DevUINode sender, string message)
	{
		orig(self, type, sender, message);

		ObjectsPageData objectsPageData = self.GetData();

		if (sender.IDstring == "Switch_Mode_Button")
		{
			objectsPageData.isInIndividualMode = !objectsPageData.isInIndividualMode;

			self.Refresh();
		}
		else if (sender.IDstring.Contains("Placed_Object_Button_"))
		{
			if (int.TryParse(sender.IDstring[21..], out int placedObjectIndex))
			{
				objectsPageData.visiblePlacedObjectsIndexes.Clear();
				objectsPageData.visiblePlacedObjectsIndexes.Add(placedObjectIndex);

				self.Refresh();
			}
		}
		else if (sender.IDstring.Contains("Placed_Object_On_Button_"))
		{
			if (int.TryParse(sender.IDstring[24..], out int placedObjectIndex))
			{
				if (!objectsPageData.visiblePlacedObjectsIndexes.Contains(placedObjectIndex))
				{
					objectsPageData.visiblePlacedObjectsIndexes.Add(placedObjectIndex);
				}

				self.Refresh();
			}
		}
		else if (sender.IDstring.Contains("Placed_Object_Off_Button_"))
		{
			if (int.TryParse(sender.IDstring[25..], out int placedObjectIndex))
			{
				objectsPageData.visiblePlacedObjectsIndexes.Remove(placedObjectIndex);

				self.Refresh();
			}
		}
		else if (sender.IDstring == "Select_All_Button")
		{
			objectsPageData.visiblePlacedObjectsIndexes.Clear();

			for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
			{
				if (self.RoomSettings.placedObjects[i].type == objectsPageData.sortingType || objectsPageData.sortingType == null)
				{
					objectsPageData.visiblePlacedObjectsIndexes.Add(i);
				}
			}

			self.Refresh();
		}
		else if (sender.IDstring == "Deselect_All_Button")
		{
			objectsPageData.visiblePlacedObjectsIndexes.Clear();

			self.Refresh();
		}
		else if (sender.IDstring == "Prev_Page_Button" || sender.IDstring == "Next_Page_Button") 
		{ 
			if (sender.IDstring == "Prev_Page_Button")
			{
				objectsPageData.placedObjectsPage--;
			}
			else
			{
				objectsPageData.placedObjectsPage++;
			}

			int numberOfPages = self.RoomSettings.placedObjects.Count / MaxPlacedObjectsPerPage + 1;

			if (objectsPageData.placedObjectsPage < 0) objectsPageData.placedObjectsPage += numberOfPages;
			objectsPageData.placedObjectsPage %= numberOfPages;

			self.Refresh();
		}
		else if (sender.IDstring == "Types_Prev_Page_Button" || sender.IDstring == "Types_Next_Page_Button")
		{
			if (sender.IDstring == "Types_Prev_Page_Button")
			{
				objectsPageData.typePage--;
			}
			else
			{
				objectsPageData.typePage++;
			}

			// Count number of different object types in the room
			List<PlacedObject.Type> roomPlacedObjectTypes = new List<PlacedObject.Type>();
			for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
			{
				if (!roomPlacedObjectTypes.Contains(self.RoomSettings.placedObjects[i].type))
				{
					roomPlacedObjectTypes.Add(self.RoomSettings.placedObjects[i].type);
				}
			}

			int numberOfPages = roomPlacedObjectTypes.Count / 8 + 1;

			if (objectsPageData.typePage < 0) objectsPageData.typePage += numberOfPages;
			objectsPageData.typePage %= numberOfPages;

			self.Refresh();
		}
		else if (sender.IDstring[0..10] == "View_Type_")
		{
			if (sender.IDstring == "View_Type_All")
			{
				objectsPageData.sortingType = null;
			}
			else
			{
				objectsPageData.sortingType = new PlacedObject.Type(sender.IDstring[10..]);
			}

			self.Refresh();
		}
		else if (type == DevUISignalType.Create)
		{
			// todo: change this so it doesnt remove all objects from selection
			//objectsPageData.visiblePlacedObjectsIndexes.Clear();
			objectsPageData.visiblePlacedObjectsIndexes.Add(self.RoomSettings.placedObjects.Count - 1);
			self.Refresh();
		}
	}

	private static void ObjectsPage_RemoveObject(On.DevInterface.ObjectsPage.orig_RemoveObject orig, ObjectsPage self, PlacedObjectRepresentation objRep)
	{
		int placedObjectIndex = self.RoomSettings.placedObjects.IndexOf(objRep.pObj);

		orig(self, objRep);

		self.GetData().visiblePlacedObjectsIndexes.Remove(placedObjectIndex);
		for (int i = 0; i < self.GetData().visiblePlacedObjectsIndexes.Count; i++)
		{
			if (self.GetData().visiblePlacedObjectsIndexes[i] >= placedObjectIndex)
			{
				self.GetData().visiblePlacedObjectsIndexes[i]--;
			}
		}

		self.Refresh();
	}

	private static void PlacedObjectRepresentation_Update(On.DevInterface.PlacedObjectRepresentation.orig_Update orig, PlacedObjectRepresentation self)
	{
		// This is in the update because of LightFixtures & MultiplayerItems making everything hard.
		orig(self);

		if (!int.TryParse(self.fLabels[0].text.Split(' ')[0], out _) 
			&& (self.Page is ObjectsPage)
			&& (self.Page as ObjectsPage).GetData().isInIndividualMode) 
		{ 
			self.fLabels[0].text = self.RoomSettings.placedObjects.IndexOf(self.pObj) + " " + self.fLabels[0].text; 
		}
	}

	private static void IL_ObjectsPage_Refresh(ILContext il)
	{
		ILCursor cursor = new ILCursor(il);
		cursor.Index = 2;
		cursor.RemoveRange(26);
		cursor.Emit(OpCodes.Ldarg_0);

		cursor.EmitDelegate<Action<ObjectsPage>>((self) => 
		{
			ObjectsPageData objectsPageData = self.GetData();

			// Add object rep for all objects that should be shown
			for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
			{
				if (objectsPageData.visiblePlacedObjectsIndexes.Contains(i) || !objectsPageData.isInIndividualMode) 
				{ 
					self.CreateObjRep(self.RoomSettings.placedObjects[i].type, self.RoomSettings.placedObjects[i]); 
				}
			}

			if (objectsPageData.isInIndividualMode)
			{
				PlacedObjectsPanel panel = new PlacedObjectsPanel(self.owner, "All_Objects_Panel", self, new Vector2(1050f, 40f), new Vector2(300f, 620f), "Placed Objects Browser");

				// Get all different types of placed objects.
				List<PlacedObject.Type> roomPlacedObjectTypes = new List<PlacedObject.Type>();
				for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
				{
					if (!roomPlacedObjectTypes.Contains(self.RoomSettings.placedObjects[i].type))
					{
						roomPlacedObjectTypes.Add(self.RoomSettings.placedObjects[i].type);
					}
				}

				panel.subNodes.Add(new DevUILabel(self.owner, "Label", panel, new Vector2(5f, panel.size.y - 20f), 290f, "View by type"));

				panel.subNodes.Add(new Button(self.owner, "Types_Prev_Page_Button", panel, new Vector2(5f, panel.size.y - 40f), 100f, "Prev Page"));
				panel.subNodes.Add(new Button(self.owner, "Types_Next_Page_Button", panel, new Vector2(195f, panel.size.y - 40f), 100f, "Next Page"));

				if (objectsPageData.typePage == 0) 
				{
					panel.subNodes.Add(new Button(self.owner, "View_Type_All", panel, new Vector2(5f, panel.size.y - 80f), 290f, "All"));
				}

				for (int i = 0; i < roomPlacedObjectTypes.Count; i++)
				{
					if ((i + 1) / 8 == objectsPageData.typePage)
					{
						panel.subNodes.Add(new Button(self.owner, "View_Type_" + roomPlacedObjectTypes[i].ToString(), panel, new Vector2(5f, panel.size.y - 80f - 20f * ((i + 1) % 8)), 290f, roomPlacedObjectTypes[i].ToString()));
					}
				}

				panel.subNodes.Add(new Panel.HorizontalDivider(self.owner, "Divider", panel, panel.size.y - 220f));

				panel.subNodes.Add(new DevUILabel(self.owner, "Label", panel, new Vector2(5f, panel.size.y - 240f), 290f, "Placed Objects"));

				panel.subNodes.Add(new Button(self.owner, "Prev_Page_Button", panel, new Vector2(5f, panel.size.y - 260f), 100f, "Prev Page"));
				panel.subNodes.Add(new Button(self.owner, "Next_Page_Button", panel, new Vector2(195f, panel.size.y - 260f), 100f, "Next Page"));

				panel.subNodes.Add(new Button(self.owner, "Select_All_Button", panel, new Vector2(5f, panel.size.y - 300f), 100f, "Select All"));
				panel.subNodes.Add(new Button(self.owner, "Deselect_All_Button", panel, new Vector2(110f, panel.size.y - 300), 100f, "Deselect All"));

				// Display buttons for all placed objects
				int placedObjectListIndex = 0; 
				for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
				{
					if (objectsPageData.sortingType == null || self.RoomSettings.placedObjects[i].type == objectsPageData.sortingType)
					{
						if (placedObjectListIndex / MaxPlacedObjectsPerPage == objectsPageData.placedObjectsPage)
						{
							string test = "";
							if (objectsPageData.visiblePlacedObjectsIndexes.Contains(i)) test = "> ";
							panel.subNodes.Add(new Button(self.owner, $"Placed_Object_Button_{i}", panel, new Vector2(5f, panel.size.y - 320f - 20f * (placedObjectListIndex % MaxPlacedObjectsPerPage)), 248f, $"{i} {test}{self.RoomSettings.placedObjects[i].type.ToString()}"));

							panel.subNodes.Add(new Button(self.owner, $"Placed_Object_Off_Button_{i}", panel, new Vector2(279f, panel.size.y - 320f - 20f * (placedObjectListIndex % MaxPlacedObjectsPerPage)), 16f, " -"));
							panel.subNodes.Add(new Button(self.owner, $"Placed_Object_On_Button_{i}", panel, new Vector2(258f, panel.size.y - 320f - 20f * (placedObjectListIndex % MaxPlacedObjectsPerPage)), 16f, " +"));
						}

						placedObjectListIndex++;
					}
				}

				self.tempNodes.Add(panel);
				self.subNodes.Add(panel);
			}
		});
	}
}
