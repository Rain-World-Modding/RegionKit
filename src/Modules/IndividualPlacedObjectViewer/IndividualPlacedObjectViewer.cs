using System.Runtime.CompilerServices;
using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.IndividualPlacedObjectViewer;

internal static class IndividualPlacedObjectViewer
{
	private static readonly ConditionalWeakTable<ObjectsPage, ObjectsPageData> objectsPageCWT = new ConditionalWeakTable<ObjectsPage, ObjectsPageData>();

	private class ObjectsPageData
	{
		public bool isInIndividualMode;
		public int currentPlacedObject = -1;

		public ObjectsPageData()
		{
			isInIndividualMode = false;
		}
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
		// Hooks, alot?
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
			Debug.Log("Switch_Mode was pressed!");
			Debug.Log("Mode is now: " + self.GetData().isInIndividualMode.ToString());

			// Switch mode
			self.Refresh();
		}
		if (sender.IDstring.Contains("Placed_Object_Button_"))
		{
			Debug.Log(sender.IDstring[21..]);
			int.TryParse(sender.IDstring[21..], out int bruh);
			self.GetData().currentPlacedObject = bruh;
			self.Refresh();
		}

		if (type == DevUISignalType.Create)
		{
			self.GetData().currentPlacedObject = self.RoomSettings.placedObjects.Count - 1;
			self.Refresh();
		}
	}

	private static void IL_ObjectsPage_Refresh(ILContext il)
	{
		// todo: add back orig??? Is it even that needed?
		//orig(self); 

		ILCursor cursor = new ILCursor(il);

		cursor.Index = 2;
		Debug.Log(cursor.Instrs.Count);
		for (int i = 0; i < cursor.Instrs.Count; i++)
		{
			Debug.Log(cursor.Instrs[i].OpCode.ToString());
		}
		Debug.Log("-------------------");

		cursor.RemoveRange(26);
		for (int i = 0; i < cursor.Instrs.Count; i++)
		{
			Debug.Log(cursor.Instrs[i].OpCode.ToString());
		}


		cursor.Emit(OpCodes.Ldarg_0);

		cursor.EmitDelegate<Action<ObjectsPage>>((self) => 
		{
			for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
			{
				if (self.GetData().currentPlacedObject == i || !self.GetData().isInIndividualMode) self.CreateObjRep(self.RoomSettings.placedObjects[i].type, self.RoomSettings.placedObjects[i]);
			}

			if (self.GetData().isInIndividualMode)
			{
				PlacedObjectsPanel panel = new PlacedObjectsPanel(self.owner, "All_Objects_Panel", self, new Vector2(1050f, 20f), new Vector2(300f, 650f), "Placed Objects");

				for (int i = 0; i < self.RoomSettings.placedObjects.Count; i++)
				{
					panel.subNodes.Add(new Button(self.owner, $"Placed_Object_Button_{i}", panel, new Vector2(5f, panel.size.y - 20f * (i + 1)), 290, $"{i} {self.RoomSettings.placedObjects[i].type.ToString()}"));
				}

				self.tempNodes.Add(panel);
				self.subNodes.Add(panel);
			}
		});
	}
}
