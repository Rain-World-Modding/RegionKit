﻿using DevInterface;
using static RegionKit.Modules.IndividualPlacedObjectViewer.IndividualPlacedObjectViewer;

namespace RegionKit.Modules.IndividualPlacedObjectViewer;

internal class PlacedObjectsPanel : Panel, IDevUISignals
{
	private ObjectsPage objectsPage;
	private List<DevUINode> tempNodes;

	public PlacedObjectsPanel(ObjectsPage objectsPage, DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, string title) : base(owner, IDstring, parentNode, pos, size, title)
	{
		this.objectsPage = objectsPage;
		tempNodes = new List<DevUINode>();

		subNodes.Add(new DevUILabel(objectsPage.owner, "Label", this, new Vector2(5f, size.y - 20f), 290f, "View by type"));
		
		subNodes.Add(new Button(objectsPage.owner, "Types_Prev_Page_Button", this, new Vector2(5f, size.y - 40f), 100f, "Prev Page"));
		subNodes.Add(new Button(objectsPage.owner, "Types_Next_Page_Button", this, new Vector2(195f, size.y - 40f), 100f, "Next Page"));

		subNodes.Add(new HorizontalDivider(objectsPage.owner, "Divider", this, size.y - 220f));

		subNodes.Add(new DevUILabel(objectsPage.owner, "Label", this, new Vector2(5f, size.y - 240f), 290f, "Placed Objects"));

		subNodes.Add(new Button(objectsPage.owner, "Prev_Page_Button", this, new Vector2(5f, size.y - 260f), 100f, "Prev Page"));
		subNodes.Add(new Button(objectsPage.owner, "Next_Page_Button", this, new Vector2(195f, size.y - 260f), 100f, "Next Page"));

		subNodes.Add(new Button(objectsPage.owner, "Select_All_Button", this, new Vector2(5f, size.y - 300f), 100f, "Select All"));
		subNodes.Add(new Button(objectsPage.owner, "Deselect_All_Button", this, new Vector2(110f, size.y - 300), 100f, "Deselect All"));

		RefreshButtons();
	}

	private void RefreshTypeBrowser()
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
			if ((i + 1) / 8 == objectsPageData.typePage)
			{
				subNodes.Add(new Button(owner, "View_Type_" + roomPlacedObjectTypes[i].ToString(), this, new Vector2(5f, size.y - 80f - 20f * ((i + 1) % 8)), 290f, roomPlacedObjectTypes[i].ToString()));
				tempNodes.Add(subNodes[subNodes.Count - 1]);
			}
		}
	}

	private void RefreshPlacedObjectsBrowser()
	{
		ObjectsPageData objectsPageData = objectsPage.GetData();

		int placedObjectListIndex = 0;
		for (int i = 0; i < RoomSettings.placedObjects.Count; i++)
		{
			if (objectsPageData.sortingType == null || RoomSettings.placedObjects[i].type == objectsPageData.sortingType)
			{
				if (placedObjectListIndex / MaxPlacedObjectsPerPage == objectsPageData.placedObjectsPage)
				{
					string selectedString = "";
					if (objectsPageData.visiblePlacedObjectsIndexes.Contains(i)) selectedString = "> ";

					subNodes.Add(new Button(owner, $"Placed_Object_Button_{i}", this, new Vector2(5f, size.y - 320f - 20f * (placedObjectListIndex % MaxPlacedObjectsPerPage)), 248f, $"{i} {selectedString}{RoomSettings.placedObjects[i].type.ToString()}"));
					tempNodes.Add(subNodes[subNodes.Count - 1]);

					subNodes.Add(new Button(owner, $"Placed_Object_Off_Button_{i}", this, new Vector2(279f, size.y - 320f - 20f * (placedObjectListIndex % MaxPlacedObjectsPerPage)), 16f, " -"));
					tempNodes.Add(subNodes[subNodes.Count - 1]);

					subNodes.Add(new Button(owner, $"Placed_Object_On_Button_{i}", this, new Vector2(258f, size.y - 320f - 20f * (placedObjectListIndex % MaxPlacedObjectsPerPage)), 16f, " +"));
					tempNodes.Add(subNodes[subNodes.Count - 1]);
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

		RefreshTypeBrowser();
		RefreshPlacedObjectsBrowser();
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		Debug.Log("Button " + sender.IDstring + " was pressed!");
		objectsPage.Signal(DevUISignalType.ButtonClick, sender, "");
	}
}