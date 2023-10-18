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
	private const int MaxPlacedObjectsPerPage = 21;

	private class ObjectsPageData
	{
		public bool isInIndividualMode = false;
		public List<int> visiblePlacedObjects = new List<int>();
		public int page = 0;
		public int typePage = 0;

		public PlacedObject.Type? type = null;

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

		IL.DevInterface.ObjectsPage.Refresh += IL_ObjectsPage_Refresh;
	}


	public static void Disable()
	{
		On.DevInterface.ObjectsPage.ctor -= ObjectsPage_ctor;
		On.DevInterface.ObjectsPage.Signal -= ObjectsPage_Signal;

		IL.DevInterface.ObjectsPage.Refresh -= IL_ObjectsPage_Refresh;
	}

	private static void ObjectsPage_ctor(On.DevInterface.ObjectsPage.orig_ctor orig, ObjectsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
	{
		orig(self, owner, IDstring, parentNode, name);

		self.subNodes.Add(new Button(owner, "Switch_Mode", self, new Vector2(180f, 710f), 95f, "Switch mode"));
	}

	private static void ObjectsPage_Signal(On.DevInterface.ObjectsPage.orig_Signal orig, ObjectsPage self, DevUISignalType type, DevUINode sender, string message)
	{
		orig(self, type, sender, message);

		if (sender.IDstring == "Switch_Mode")
		{
			self.GetData().isInIndividualMode = !self.GetData().isInIndividualMode;
			//Debug.Log("Switch_Mode was pressed!");
			//Debug.Log("Mode is now: " + self.GetData().isInIndividualMode.ToString());

			self.Refresh();
		}
		if (sender.IDstring.Contains("Placed_Object_Button_"))
		{
			//Debug.Log(sender.IDstring[21..]);
			int.TryParse(sender.IDstring[21..], out int placedObjectIndex);
			self.GetData().visiblePlacedObjects.Clear();
			self.GetData().visiblePlacedObjects.Add(placedObjectIndex);
			self.Refresh();
		}

		if (sender.IDstring.Contains("Placed_Object_Off_Button_"))
		{
			int.TryParse(sender.IDstring[25..], out int placedObjectIndex);
			self.GetData().visiblePlacedObjects.Remove(placedObjectIndex);

			self.Refresh();
		}

		if (sender.IDstring.Contains("Placed_Object_On_Button_"))
		{
			int.TryParse(sender.IDstring[24..], out int placedObjectIndex);
			if (!self.GetData().visiblePlacedObjects.Contains(placedObjectIndex))
			{
				self.GetData().visiblePlacedObjects.Add(placedObjectIndex);
			}

			self.Refresh();
		}

		if (sender.IDstring == "Select_All_Button")
		{
			self.GetData().visiblePlacedObjects.Clear();

			for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
			{
				if (self.RoomSettings.placedObjects[i].type == self.GetData().type || self.GetData().type == null)
				{
					self.GetData().visiblePlacedObjects.Add(i);
				}
			}
			self.Refresh();
		}

		if (sender.IDstring == "Deselect_All_Button")
		{
			self.GetData().visiblePlacedObjects.Clear();
			self.Refresh();
		}

		if (sender.IDstring == "Prev_Page_Button") 
		{ 
			self.GetData().page--;

			int numberOfPages = self.RoomSettings.placedObjects.Count / MaxPlacedObjectsPerPage + 1;
			if (self.GetData().page < 0) self.GetData().page += numberOfPages;
			self.GetData().page %= numberOfPages;

			self.Refresh();
		}
		if (sender.IDstring == "Next_Page_Button") 
		{
			self.GetData().page++;

			int numberOfPages = self.RoomSettings.placedObjects.Count / MaxPlacedObjectsPerPage + 1;
			if (self.GetData().page < 0) self.GetData().page += numberOfPages;
			self.GetData().page %= numberOfPages;

			self.Refresh();
		}

		if (sender.IDstring == "Types_Prev_Page_Button")
		{
			self.GetData().typePage--;

			List<PlacedObject.Type> roomPlacedObjectTypes = new List<PlacedObject.Type>();

			for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
			{
				if (!roomPlacedObjectTypes.Contains(self.RoomSettings.placedObjects[i].type))
				{
					roomPlacedObjectTypes.Add(self.RoomSettings.placedObjects[i].type);
				}
			}

			int numberOfPages = roomPlacedObjectTypes.Count / 4 + 1;
			if (self.GetData().typePage < 0) self.GetData().typePage += numberOfPages;
			self.GetData().typePage %= numberOfPages;

			self.Refresh();
		}
		if (sender.IDstring == "Types_Next_Page_Button")
		{
			self.GetData().typePage++;

			List<PlacedObject.Type> roomPlacedObjectTypes = new List<PlacedObject.Type>();

			for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
			{
				if (!roomPlacedObjectTypes.Contains(self.RoomSettings.placedObjects[i].type))
				{
					roomPlacedObjectTypes.Add(self.RoomSettings.placedObjects[i].type);
				}
			}

			int numberOfPages = roomPlacedObjectTypes.Count / 4 + 1;
			if (self.GetData().typePage < 0) self.GetData().typePage += numberOfPages;
			self.GetData().typePage %= numberOfPages;

			self.Refresh();
		}

		if (sender.IDstring[0..10] == "View_Type_")
		{
			if(sender.IDstring == "View_Type_All")
			{
				self.GetData().type = null;
			}
			else
			{
				self.GetData().type = new PlacedObject.Type(sender.IDstring[10..]);
			}

			self.Refresh();
		}

		if (type == DevUISignalType.Create)
		{
			self.GetData().visiblePlacedObjects.Clear();
			self.GetData().visiblePlacedObjects.Add(self.RoomSettings.placedObjects.Count - 1);
			self.Refresh();
		}
	}

	private static void IL_ObjectsPage_Refresh(ILContext il)
	{
		// todo: add back orig??? Is it even that needed?
		//orig(self); 

		ILCursor cursor = new ILCursor(il);
		cursor.Index = 2;
		cursor.RemoveRange(26);
		cursor.Emit(OpCodes.Ldarg_0);

		cursor.EmitDelegate<Action<ObjectsPage>>((self) => 
		{
			for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
			{
				if (self.GetData().visiblePlacedObjects.Contains(i) || !self.GetData().isInIndividualMode) 
				{ 
					self.CreateObjRep(self.RoomSettings.placedObjects[i].type, self.RoomSettings.placedObjects[i]); 
				}
			}

			if (self.GetData().isInIndividualMode)
			{
				PlacedObjectsPanel panel = new PlacedObjectsPanel(self.owner, "All_Objects_Panel", self, new Vector2(1050f, 40f), new Vector2(300f, 600f), "Placed Objects");

				// Get all different types of placed objects.
				List<PlacedObject.Type> roomPlacedObjectTypes = new List<PlacedObject.Type>();

				for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
				{
					if (!roomPlacedObjectTypes.Contains(self.RoomSettings.placedObjects[i].type))
					{
						roomPlacedObjectTypes.Add(self.RoomSettings.placedObjects[i].type);
					}
				}

				panel.subNodes.Add(new Button(self.owner, "Types_Prev_Page_Button", panel, new Vector2(5f, panel.size.y - 20f), 100f, "Prev Page"));
				panel.subNodes.Add(new Button(self.owner, "Types_Next_Page_Button", panel, new Vector2(195f, panel.size.y - 20f), 100f, "Next Page"));


				int j2 = 1;

				if (self.GetData().typePage == 0) 
				{
					panel.subNodes.Add(new Button(self.owner, "View_Type_All", panel, new Vector2(5f, panel.size.y - 60f), 290f, "All"));
				}
				for (int i = 0; i < roomPlacedObjectTypes.Count; i++)
				{
					if(j2 / 4 == self.GetData().typePage)
					{
						panel.subNodes.Add(new Button(self.owner, "View_Type_" + roomPlacedObjectTypes[i].ToString(), panel, new Vector2(5f, panel.size.y - 60f - 20f * (j2 % 4)), 290f, roomPlacedObjectTypes[i].ToString()));
					}
					j2++;
				}
				// Pages for those??


				panel.subNodes.Add(new Panel.HorizontalDivider(self.owner, "Divider", panel, panel.size.y - 120f));

				//int numberOfPages = self.RoomSettings.placedObjects.Count / MaxPlacedObjectsPerPage + 1;

				panel.subNodes.Add(new Button(self.owner, "Prev_Page_Button", panel, new Vector2(5f, panel.size.y - 140f), 100f, "Prev Page"));
				panel.subNodes.Add(new Button(self.owner, "Next_Page_Button", panel, new Vector2(195f, panel.size.y - 140f), 100f, "Next Page"));

				panel.subNodes.Add(new Button(self.owner, "Select_All_Button", panel, new Vector2(5f, panel.size.y - 180f), 100f, "Select All"));
				panel.subNodes.Add(new Button(self.owner, "Deselect_All_Button", panel, new Vector2(110f, panel.size.y - 180f), 100f, "Deselect All"));

				int j = 0; 
				for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
				{
					if(self.GetData().type == null || self.RoomSettings.placedObjects[i].type == self.GetData().type)
					{
						if (j / MaxPlacedObjectsPerPage == self.GetData().page)
						{
							panel.subNodes.Add(new Button(self.owner, $"Placed_Object_Button_{i}", panel, new Vector2(5f, panel.size.y - 200f - 20f * (j % MaxPlacedObjectsPerPage)), 248f, $"{i} {self.RoomSettings.placedObjects[i].type.ToString()}"));

							panel.subNodes.Add(new Button(self.owner, $"Placed_Object_Off_Button_{i}", panel, new Vector2(279f, panel.size.y - 200f - 20f * (j % MaxPlacedObjectsPerPage)), 16f, " -"));
							panel.subNodes.Add(new Button(self.owner, $"Placed_Object_On_Button_{i}", panel, new Vector2(258f, panel.size.y - 200f - 20f * (j % MaxPlacedObjectsPerPage)), 16f, " +"));
						}

						j++;
					}
				}

				self.tempNodes.Add(panel);
				self.subNodes.Add(panel);
			}
		});
	}
}
