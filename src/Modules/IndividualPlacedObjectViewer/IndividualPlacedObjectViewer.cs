using System.Diagnostics.CodeAnalysis;
using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.IndividualPlacedObjectViewer;

internal static partial class IndividualPlacedObjectViewer
{
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
		On.DevInterface.ObjectsPage.RemoveObject -= ObjectsPage_RemoveObject;

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

		// :rivglape:
		if (sender.IDstring == "Switch_Mode_Button")
		{
			objectsPageData.isInIndividualMode = !objectsPageData.isInIndividualMode;

			if (objectsPageData.isInIndividualMode)
			{
				objectsPageData.placedObjectsPanel = new PlacedObjectsPanel(self, self.owner, "All_Objects_Panel", self, new Vector2(1050f, 40f), new Vector2(300f, 620f), "Placed Objects");
				self.subNodes.Add(objectsPageData.placedObjectsPanel);
			}
			else if (objectsPageData.placedObjectsPanel != null)
			{
				objectsPageData.placedObjectsPanel.ClearSprites();
				self.subNodes.Remove(objectsPageData.placedObjectsPanel);
				objectsPageData.placedObjectsPanel = null;
			}

			objectsPageData.placedObjectsPanel?.RefreshButtons();
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
		else if (sender.IDstring.Contains("Duplicate_Object_Button_"))
		{
			if (int.TryParse(sender.IDstring[24..], out int placedObjectIndex))
			{
				PlacedObject placedObject = new PlacedObject(PlacedObject.Type.None, null);

				placedObject.FromString([self.RoomSettings.placedObjects[placedObjectIndex].type.ToString(), "0", "0", self.RoomSettings.placedObjects[placedObjectIndex].data.ToString().Trim()]);
				
				placedObject.pos = self.owner.game.cameras[0].pos + new Vector2(200f, 200f);
				self.RoomSettings.placedObjects.Add(placedObject);

				objectsPageData.visiblePlacedObjectsIndexes.Add(self.RoomSettings.placedObjects.Count - 1);

				// Refresh stuff
				objectsPageData.placedObjectsPanel?.RefreshButtons();
				self.Refresh();
			}

		}
		else if (sender.IDstring.Contains("Placed_Object_Toggle_Button_"))
		{
			if (int.TryParse(sender.IDstring[28..], out int placedObjectIndex))
			{
				if (!objectsPageData.visiblePlacedObjectsIndexes.Contains(placedObjectIndex))
				{
					objectsPageData.visiblePlacedObjectsIndexes.Add(placedObjectIndex);
				}
				else
				{
					objectsPageData.visiblePlacedObjectsIndexes.Remove(placedObjectIndex);
				}

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
		else if (sender.IDstring == "Sort_Objects_Button")
		{
			List<PlacedObject> visibleObjects = [];

			for (int i = 0; i < objectsPageData.visiblePlacedObjectsIndexes.Count; i++)
			{
				visibleObjects.Add(self.RoomSettings.placedObjects[objectsPageData.visiblePlacedObjectsIndexes[i]]);
			}

			self.RoomSettings.placedObjects.Sort((PlacedObject p1, PlacedObject p2) =>
			{
				return string.Compare(p1.type.ToString(), p2.type.ToString());
			});

			objectsPageData.visiblePlacedObjectsIndexes.Clear();

			for (int i = 0; i < visibleObjects.Count; i++)
			{
				objectsPageData.visiblePlacedObjectsIndexes.Add(self.RoomSettings.placedObjects.IndexOf(visibleObjects[i]));
			}

			objectsPageData.placedObjectsPanel?.RefreshButtons();
			self.Refresh();
		}
		else if (sender.IDstring == "Delete_Selected_Button")
		{
			ConfirmDeletionPanel confirmPanel = new ConfirmDeletionPanel(self.owner, "Confirm_Deletion_Panel", self, new Vector2(600f, 400f), new Vector2(120f, 85f), "Delete Selected");
			self.subNodes.Add(confirmPanel);
			self.tempNodes.Add(confirmPanel);
		}
		else if (sender.IDstring == "Confirm_Delete_Button")
		{
			List<PlacedObject> objectsToDelete = new List<PlacedObject>();

			for (int i = 0; i < objectsPageData.visiblePlacedObjectsIndexes.Count; i++)
			{
				objectsToDelete.Add(self.RoomSettings.placedObjects[objectsPageData.visiblePlacedObjectsIndexes[i]]);
			}

			foreach (PlacedObject placedObject in objectsToDelete)
			{
				self.RoomSettings.placedObjects.Remove(placedObject);
			}

			objectsPageData.visiblePlacedObjectsIndexes.Clear();

			objectsPageData.placedObjectsPanel?.RefreshButtons();
			self.Refresh();
		}
		else if (sender.IDstring == "Cancel_Delete_Button")
		{
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

			int numberOfPages = self.RoomSettings.placedObjects.Count / PlacedObjectsPanel.MaxPlacedObjectsPerPage + 1;

			if (objectsPageData.placedObjectsPage < 0) objectsPageData.placedObjectsPage += numberOfPages;
			objectsPageData.placedObjectsPage %= numberOfPages;

			objectsPageData.placedObjectsPanel?.RefreshButtons();
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

			int numberOfPages = roomPlacedObjectTypes.Count / PlacedObjectsPanel.MaxTypesPerPage + 1;

			if (objectsPageData.typePage < 0) objectsPageData.typePage += numberOfPages;
			objectsPageData.typePage %= numberOfPages;

			objectsPageData.placedObjectsPanel?.RefreshButtons();
			self.Refresh();
		}
		else if (sender.IDstring.StartsWith("View_Type_"))
		{
			if (sender.IDstring == "View_Type_All")
			{
				objectsPageData.sortingType = null;
			}
			else
			{
				objectsPageData.sortingType = new PlacedObject.Type(sender.IDstring[10..]);
			}

			objectsPageData.placedObjectsPanel?.RefreshButtons();
			self.Refresh();
		}
		else if (type == DevUISignalType.Create)
		{
			objectsPageData.visiblePlacedObjectsIndexes.Add(self.RoomSettings.placedObjects.Count - 1);
			objectsPageData.placedObjectsPanel?.RefreshButtons();
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

		self.GetData().placedObjectsPanel?.RefreshButtons();
		self.Refresh();
	}

	private static void PlacedObjectRepresentation_Update(On.DevInterface.PlacedObjectRepresentation.orig_Update orig, PlacedObjectRepresentation self)
	{
		// This is in the update because of LightFixtures & MultiplayerItems making everything hard.
		orig(self);

		if (!int.TryParse(self.fLabels[0].text.Split(' ')[0], out _) 
			&& self.Page is ObjectsPage objectsPage
			&& objectsPage.GetData().isInIndividualMode) 
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
		});
	}
}
