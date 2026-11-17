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

		public static ObjectsPageData MoonObjectsPageData(this ObjectsPage self) => ObjectsPageCWT.GetOrCreateValue(self);
		public static RoomData MoonRoomData(this Room self) => RoomCWT.GetOrCreateValue(self);
		public static ConsoleVisualizerData MoonConsoleVizData(this ConsoleVisualizer self) => ConsoleVisualizerCWT.GetOrCreateValue(self);

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
