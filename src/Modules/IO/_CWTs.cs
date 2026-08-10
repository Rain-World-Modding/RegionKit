using System.Runtime.CompilerServices;
using DevInterface;
using MoreSlugcats;

#nullable disable

namespace RegionKit.Modules.IO
{
	internal static class _CWTs
	{
		static ConditionalWeakTable<ObjectsPage, ObjectsPageData> ObjectsPageCWT = new ConditionalWeakTable<ObjectsPage, ObjectsPageData>();
		static ConditionalWeakTable<Room, RoomData> RoomCWT = new ConditionalWeakTable<Room, RoomData>();
		static ConditionalWeakTable<ConsoleVisualizer, ConsoleVisualizerData> ConsoleVisualizerCWT = new ConditionalWeakTable<ConsoleVisualizer, ConsoleVisualizerData>();

		public static ObjectsPageData CustomData(this ObjectsPage self) => ObjectsPageCWT.GetOrCreateValue(self);
		public static RoomData CustomData(this Room self) => RoomCWT.GetOrCreateValue(self);
		public static ConsoleVisualizerData CustomData(this ConsoleVisualizer self) => ConsoleVisualizerCWT.GetOrCreateValue(self);

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
