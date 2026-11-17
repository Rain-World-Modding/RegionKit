using System.Runtime.CompilerServices;
using DevInterface;
using MoreSlugcats;

#nullable disable

namespace RegionKit.Modules.MoonIO
{
	internal static class _CWTs
	{
		static RKConditionalWeakTable<ObjectsPage, ObjectsPageData> ObjectsPageCWT = new();
		static RKConditionalWeakTable<Room, RoomData> RoomCWT = new();
		static RKConditionalWeakTable<ConsoleVisualizer, ConsoleVisualizerData> ConsoleVisualizerCWT = new();

		public static ObjectsPageData MoonObjectsPageData(this ObjectsPage self) => ObjectsPageCWT.GetValue(self, _ => new());
		public static RoomData MoonRoomData(this Room self) => RoomCWT.GetValue(self, _ => new());
		public static ConsoleVisualizerData MoonConsoleVizData(this ConsoleVisualizer self) => ConsoleVisualizerCWT.GetValue(self, _ => new());

		public class ObjectsPageData
		{
			public bool ShowConnections = true;

			public Button ConnectionsButton;
		}

		public class RoomData
		{
			public List<IOObject> IOObjects;
		}

		public class ConsoleVisualizerData
		{
			public FLabel IOLog;

			public List<string> RecentIOLog;
		}
	}
}
