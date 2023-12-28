using DevInterface;
using RegionKit.Modules.Iggy;
using static RegionKit.Modules.IndividualPlacedObjectViewer.IndividualPlacedObjectViewer;

namespace RegionKit.Modules.IndividualPlacedObjectViewer;

internal class PlacedObjectsPanel : Panel, IDevUISignals, Modules.Iggy.IGiveAToolTip
{
	public const int MaxPlacedObjectsPerPage = 16;
	public const int MaxTypesPerPage = 8;

	private ObjectsPage objectsPage;
	private List<DevUINode> tempNodes;

	private Dictionary<int, Button> placedObjectToggleButtons;

	ToolTip IGiveAToolTip.ToolTip => new("A cleaner object menu for cluttered rooms. Bottom half filter objects in room with '+' and '-' buttons. Top half filters the bottom list view by type.", 5, this);

	bool IGeneralMouseOver.MouseOverMe => MouseOver;

	public PlacedObjectsPanel(
		ObjectsPage objectsPage,
		DevUI owner,
		string IDstring,
		DevUINode parentNode,
		Vector2 pos,
		Vector2 size,
		string title) : base(
			owner,
			IDstring,
			parentNode,
			pos,
			size,
			title)
	{
		this.objectsPage = objectsPage;
		tempNodes = new List<DevUINode>();
		placedObjectToggleButtons = new Dictionary<int, Button>();

		subNodes.Add(new DevUILabel(objectsPage.owner, "Label", this, new Vector2(5f, size.y - 20f), 290f, "View by type"));

		Button typesPrevPage = new Button(objectsPage.owner, "Types_Prev_Page_Button", this, new Vector2(5f, size.y - 40f), 100f, "Previous Page");
		API.Iggy.AddTooltip(typesPrevPage, () => new("Cycle type filters to previous page.", 10, typesPrevPage));
		subNodes.Add(typesPrevPage);
		Button typesNextPage = new Button(objectsPage.owner, "Types_Next_Page_Button", this, new Vector2(195f, size.y - 40f), 100f, "Next Page");
		API.Iggy.AddTooltip(typesNextPage, () => new("Cycle type filters to next page.", 10, typesPrevPage));
		subNodes.Add(typesNextPage);
		//subNodes.Add(typesNextPage);

		subNodes.Add(new HorizontalDivider(objectsPage.owner, "Divider", this, size.y - 220f));

		subNodes.Add(new DevUILabel(objectsPage.owner, "Label", this, new Vector2(5f, size.y - 240f), 290f, "Placed Objects"));

		Button objectsPrevPage = new Button(objectsPage.owner, "Prev_Page_Button", this, new Vector2(5f, size.y - 260f), 100f, "Previous Page");
		API.Iggy.AddTooltip(objectsPrevPage, () => new("Cycle current objects list to previous page.", 10, objectsPrevPage));
		subNodes.Add(objectsPrevPage);

		Button objectsNextPage = new Button(objectsPage.owner, "Next_Page_Button", this, new Vector2(195f, size.y - 260f), 100f, "Next Page");
		API.Iggy.AddTooltip(objectsNextPage, () => new("Cycle current objects list to next page.", 10, objectsNextPage));
		subNodes.Add(objectsNextPage);

		Button selectAll = new Button(objectsPage.owner, "Select_All_Button", this, new Vector2(5f, size.y - 300f), 70f, "Select All");
		API.Iggy.AddTooltip(selectAll, () => new("Select and show all objects that pass the type filter.", 10, selectAll));
		subNodes.Add(selectAll);

		Button clearSelection = new Button(objectsPage.owner, "Deselect_All_Button", this, new Vector2(80f, size.y - 300), 70f, "Deselect All");
		API.Iggy.AddTooltip(clearSelection, () => new("Clear selection and hide all object controls.", 10, clearSelection));
		subNodes.Add(clearSelection);


		Button deleteSelected = new Button(objectsPage.owner, "Delete_Selected_Button", this, new Vector2(195f, size.y - 300), 100f, "Delete Selected");
		API.Iggy.AddTooltip(deleteSelected, () => new("Delete all currently selected objects.", 10, deleteSelected));
		subNodes.Add(deleteSelected);

		Button sort = new Button(objectsPage.owner, "Sort_Objects_Button", this, new Vector2(155f, size.y - 300), 35f, "Sort");
		API.Iggy.AddTooltip(sort, () => new("Sort objects list.", 10, sort));
		subNodes.Add(sort);

		RefreshButtons();
	}

	private void RefreshTypesList()
	{
		ObjectsPageData objectsPageData = objectsPage.GetData();

		List<PlacedObject.Type> roomPlacedObjectTypes = new List<PlacedObject.Type>();
		for (int i = 0; i < RoomSettings.placedObjects.Count; i++)
		{
			if (!roomPlacedObjectTypes.Contains(RoomSettings.placedObjects[i].type))
			{
				roomPlacedObjectTypes.Add(RoomSettings.placedObjects[i].type);
			}
		}

		if (objectsPageData.typePage == 0)
		{
			subNodes.Add(new Button(owner, "View_Type_All", this, new Vector2(5f, size.y - 80f), 290f, "All"));
			tempNodes.Add(subNodes[subNodes.Count - 1]);
		}
		for (int i = 0; i < roomPlacedObjectTypes.Count; i++)
		{
			if ((i + 1) / MaxTypesPerPage == objectsPageData.typePage)
			{
				subNodes.Add(new Button(owner, "View_Type_" + roomPlacedObjectTypes[i].ToString(), this, new Vector2(5f, size.y - 80f - 20f * ((i + 1) % MaxTypesPerPage)), 290f, roomPlacedObjectTypes[i].ToString()));
				tempNodes.Add(subNodes[subNodes.Count - 1]);
			}
		}
	}

	private void RefreshPlacedObjectsList()
	{
		ObjectsPageData objectsPageData = objectsPage.GetData();

		placedObjectToggleButtons = new Dictionary<int, Button>();

		int placedObjectListIndex = 0;
		for (int i = 0; i < RoomSettings.placedObjects.Count; i++)
		{
			if (objectsPageData.sortingType == null || RoomSettings.placedObjects[i].type == objectsPageData.sortingType)
			{
				if (placedObjectListIndex / MaxPlacedObjectsPerPage == objectsPageData.placedObjectsPage)
				{
					subNodes.Add(new Button(owner, $"Placed_Object_Button_{i}", this, new Vector2(5f, size.y - 320f - 20f * (placedObjectListIndex % MaxPlacedObjectsPerPage)), 209f, $"{i} {RoomSettings.placedObjects[i].type.ToString()}"));
					tempNodes.Add(subNodes[subNodes.Count - 1]);

					subNodes.Add(new Button(owner, $"Duplicate_Object_Button_{i}", this, new Vector2(219f, size.y - 320f - 20f * (placedObjectListIndex % MaxPlacedObjectsPerPage)), 55f, "Duplicate"));
					tempNodes.Add(subNodes[subNodes.Count - 1]);

					string toggleButtonText = " +";
					if (objectsPageData.visiblePlacedObjectsIndexes.Contains(i)) toggleButtonText = " -";

					subNodes.Add(new Button(owner, $"Placed_Object_Toggle_Button_{i}", this, new Vector2(279f, size.y - 320f - 20f * (placedObjectListIndex % MaxPlacedObjectsPerPage)), 16f, toggleButtonText));
					tempNodes.Add(subNodes[subNodes.Count - 1]);

					placedObjectToggleButtons.Add(i, subNodes[subNodes.Count - 1] as Button);
				}

				placedObjectListIndex++;
			}
		}
	}

	public void RefreshButtons()
	{
		if (tempNodes != null)
		{
			for (int i = 0; i < tempNodes.Count; i++)
			{
				tempNodes[i].ClearSprites();
				subNodes.Remove(tempNodes[i]);
			}
		}
		tempNodes = new List<DevUINode>();

		RefreshTypesList();
		RefreshPlacedObjectsList();
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		objectsPage.Signal(DevUISignalType.ButtonClick, sender, "");

		foreach (var item in placedObjectToggleButtons)
		{
			if (objectsPage.GetData().visiblePlacedObjectsIndexes.Contains(item.Key))
			{
				item.Value.Text = " -";
			}
			else
			{
				item.Value.Text = " +";
			}
		}
	}
}
