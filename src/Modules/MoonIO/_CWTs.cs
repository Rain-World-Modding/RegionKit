using System.Runtime.CompilerServices;
using DevInterface;
using MoreSlugcats;

#nullable disable

namespace RegionKit.Modules.MoonIO
{
	internal static class _CWTs
	{
		static ConditionalWeakTable<ObjectsPage, ObjectsPageData> ObjectsPageCWT = new ConditionalWeakTable<ObjectsPage, ObjectsPageData>();
		static ConditionalWeakTable<Room, RoomData> RoomCWT = new ConditionalWeakTable<Room, RoomData>();
		static ConditionalWeakTable<ConsoleVisualizer, ConsoleVisualizerData> ConsoleVisualizerCWT = new ConditionalWeakTable<ConsoleVisualizer, ConsoleVisualizerData>();

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
