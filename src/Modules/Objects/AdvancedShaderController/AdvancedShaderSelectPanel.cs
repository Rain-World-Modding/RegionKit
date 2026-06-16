using DevInterface;
using Menu.Remix.MixedUI;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.Objects.AdvancedShaderController
{
	internal class AdvancedShaderSelectPanel : SelectPanel
	{
		private const float BUTTON_WIDTH = 165f;
		private const string SEARCH_LABEL = "Search: ";

		private static readonly float widthOfSearchText = LabelTest.GetWidth(SEARCH_LABEL);

		private string? selectedItem;
		private StringControl? searchBar;
		public string filterBy = "";

		private string[] FilteredItems
		{
			get
			{
				if (filterBy.Length == 0)
				{
					return items;
				}
				return [.. items.Where(x => x.IndexOf(filterBy, StringComparison.InvariantCultureIgnoreCase) > -1)];
			}
		}

		public AdvancedShaderSelectPanel(DevUI owner, string id, DevUINode parentNode, Vector2 pos, string name, string[] items, string? selectedItem) : base(owner, id, parentNode, pos, new Vector2(15f + 2 * BUTTON_WIDTH, 415f), name, items)
		{
			this.selectedItem = selectedItem;
			PopulateItems(currentOffset);
		}

		public override void Update()
		{
			base.Update();
			if (searchBar != null && searchBar.actualValue != filterBy)
			{
				filterBy = searchBar.actualValue;
				currentOffset = 0;
				PopulateItems(0);
			}
		}

		public override void PopulateItems(int offset)
		{
			// Clear out previous sprites
			foreach (DevUINode node in subNodes)
			{
				if (node != searchBar)
				{
					node.ClearSprites();
				}
			}
			subNodes.Clear();
			if (searchBar != null)
			{
				subNodes.Add(searchBar);
			}

			// Get filtered items
			string[] filteredItems = FilteredItems;

			// Add page switch buttons
			if (filteredItems.Length > perpage)
			{
				subNodes.Add(new Button(owner, "BackPage99289..?/~", this, new Vector2(5f, 5f), BUTTON_WIDTH, "Previous"));
				subNodes.Add(new Button(owner, "NextPage99289..?/~", this, new Vector2(10f + BUTTON_WIDTH, 5f), BUTTON_WIDTH, "Next"));
			}

			// Add search thingy
			subNodes.Add(new DevUILabel(owner, "SearchLabel99289..?/~", this, new Vector2(5f, size.y - 25f), widthOfSearchText, SEARCH_LABEL));
			if (searchBar == null)
			{
				searchBar = new StringControlNoSignal(owner, "Search99289..?/~", this, new Vector2(10f + widthOfSearchText, size.y - 25f), size.x - 15f - widthOfSearchText, filterBy, StringControl.TextIsAny);
				subNodes.Add(searchBar);
			}

			// Add buttons
			IntVector2 intVector = new IntVector2(0, 0);
			int num = currentOffset;
			while (num < filteredItems.Length && num < currentOffset + perpage)
			{
				subNodes.Add(new Button(owner, filteredItems[num], this, new Vector2(5f + intVector.x * (BUTTON_WIDTH + 5f), size.y - 50f - 20f * intVector.y), BUTTON_WIDTH, filteredItems[num])
				{
					overrideTextColor = (filteredItems[num] == selectedItem) ? new Color(0f, 0f, 1f) : null
				});
				intVector.y++;
				if (intVector.y >= (int)Mathf.Floor((float)perpage / columns))
				{
					intVector.x++;
					intVector.y = 0;
				}
				num++;
			}
		}
	}
}
