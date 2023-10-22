using System.Runtime.CompilerServices;
using DevInterface;

namespace RegionKit.Modules.IndividualPlacedObjectViewer;

internal static partial class IndividualPlacedObjectViewer
{
	private static readonly ConditionalWeakTable<ObjectsPage, ObjectsPageData> objectsPageCWT = new ConditionalWeakTable<ObjectsPage, ObjectsPageData>();
	
	internal class ObjectsPageData
	{
		public PlacedObjectsPanel? placedObjectsPanel = null;
		public bool isInIndividualMode = false;

		public List<int> visiblePlacedObjectsIndexes = new List<int>();
		public int placedObjectsPage = 0;
		public int typePage = 0;
		public PlacedObject.Type? sortingType = null;
		
		public ObjectsPageData() { }
	}

	public static ObjectsPageData GetData(this ObjectsPage objectsPage)
	{
		if (!objectsPageCWT.TryGetValue(objectsPage, out ObjectsPageData objectsPageData))
		{
			objectsPageData = new ObjectsPageData();
			objectsPageCWT.Add(objectsPage, objectsPageData);
		}

		return objectsPageData;
	}
}
