using DevInterface;

namespace RegionKit.Modules.MoonIO
{
	[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Moon IO")]
	internal static class _Module
	{
		public static void Setup()
		{
			RegisterManagedObject(new TriggerType()); // IO
			RegisterManagedObject(new IOLightSourceType()); // IO
			RegisterManagedObject(new CounterType());  // IO
			RegisterManagedObject(new CycleCooldownType());  // IO
		}

		public static void Enable()
		{
			ConsoleVisualizerIO.Apply();

			On.Room.AddObject += AddIOObjectToList;
			On.Room.CleanOutObjectNotInThisRoom += CleanIOObjects;
			On.Room.ctor += InitIOObjectList;

			On.DevInterface.ObjectsPage.ctor += AddIOConnectionButton;
			On.DevInterface.ObjectsPage.Signal += test;
		}

		public static void Disable()
		{
			ConsoleVisualizerIO.Unapply();

			On.Room.AddObject -= AddIOObjectToList;
			On.Room.CleanOutObjectNotInThisRoom -= CleanIOObjects;
			On.Room.ctor -= InitIOObjectList;

			On.DevInterface.ObjectsPage.ctor -= AddIOConnectionButton;
			On.DevInterface.ObjectsPage.Signal -= test;
		}
		private static void test(On.DevInterface.ObjectsPage.orig_Signal orig, ObjectsPage self, DevUISignalType type, DevUINode sender, string message)
		{
			if (sender.IDstring == "ChangeConnections")
			{
				bool flag = !self.MoonObjectsPageData().ShowConnections;
				self.MoonObjectsPageData().ShowConnections = flag;
				self.MoonObjectsPageData().ConnectionsButton.Text = (flag ? "Show" : "Hide") + " I/O Connections";
			}
			else
			{
				orig(self, type, sender, message);
			}
		}

		private static void AddIOConnectionButton(On.DevInterface.ObjectsPage.orig_ctor orig, ObjectsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
		{
			orig(self, owner, IDstring, parentNode, name);

			self.subNodes.Add(self.MoonObjectsPageData().ConnectionsButton = new Button(owner, "ChangeConnections", self, new Vector2(125f, 20f), 125f, "Hide I/O Connections"));
		}

		private static void InitIOObjectList(On.Room.orig_ctor orig, Room self, RainWorldGame game, World world, AbstractRoom abstractRoom, bool devUI)
		{
			orig(self, game, world, abstractRoom, devUI);
			self.MoonRoomData().IOObjects = new List<IOObject>();
		}

		private static void CleanIOObjects(On.Room.orig_CleanOutObjectNotInThisRoom orig, Room self, UpdatableAndDeletable obj)
		{
			orig(self, obj);

			if (obj is IOObject)
			{
				self.MoonRoomData().IOObjects.Remove(obj as IOObject);
			}
		}

		private static void AddIOObjectToList(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
		{
			orig(self, obj);

			if (self.game == null)
			{
				return;
			}

			if (obj is IOObject)
			{
				self.MoonRoomData().IOObjects.Add(obj as IOObject);
			}
		}
	}
}
