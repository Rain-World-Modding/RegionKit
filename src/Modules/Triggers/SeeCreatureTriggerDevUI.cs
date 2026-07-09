using DevInterface;
using Menu.Remix.MixedUI;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.Triggers
{
	internal static class SeeCreatureTriggerDevUI
	{
		internal static void Init(TriggerPanel triggerPanel, SeeCreatureTrigger trigger)
		{
			// Resize panel
			triggerPanel.size.y += 20f;
			foreach (DevUINode node in triggerPanel.subNodes)
			{
				if (node is PositionedDevUINode positionedNode)
				{
					positionedNode.pos.y += 20f;
				}
			}

			// Add custom control
			triggerPanel.subNodes.Add(new SeeCreatureSelectButton(triggerPanel.owner, "SeeCreatureSelectButton", triggerPanel, new Vector2(5f, 5f), triggerPanel.size.x - 10f, trigger));
		}

		private class SeeCreatureSelectButton : ButtonWithSelectPanel
		{
			private readonly SeeCreatureTrigger trigger;
			public SeeCreatureSelectButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, SeeCreatureTrigger trigger) : base(owner, IDstring, parentNode, pos, width, "Creature: " + trigger.creatureType.value, SelectPanelMaker)
			{
				this.trigger = trigger;
			}

			public override void OnValueChange(string value)
			{
				CreatureTemplate.Type type = new CreatureTemplate.Type(value, false);
				trigger.creatureType = type;
				Text = $"Creature: {type}";
			}

			public override void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (sender.IDstring == "BackPageSeeCreature")
				{
					selectPanel.PrevPage();
				}
				else if (sender.IDstring == "NextPageSeeCreature")
				{
					selectPanel.NextPage();
				}
				else if (sender.parentNode == selectPanel && sender.IDstring != "SearchSeeCreature")
				{
					if (selectPanel != null)
					{
						subNodes.Remove(selectPanel);
						selectPanel.ClearSprites();
						selectPanel = null;
					}
					OnValueChange(sender.IDstring);
				}
			}

			private static SelectPanel SelectPanelMaker(ButtonWithSelectPanel maker)
			{
				return new SeeCreatureSelectPanel(maker.owner, "SeeCreatureSelectPanel", maker, new Vector2(250f, 15f) - maker.absPos, "Select Creature Type", [.. CreatureTemplate.Type.values.entries], (maker as SeeCreatureSelectButton)?.trigger.creatureType.value);
			}
		}

		private class SeeCreatureSelectPanel : SelectPanel
		{
			private const float BUTTON_WIDTH = 145f;
			private const string SEARCH_LABEL = "Search: ";

			private static readonly float widthOfSearchText = LabelTest.GetWidth(SEARCH_LABEL);

			private readonly string? selectedItem;
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

			public SeeCreatureSelectPanel(DevUI owner, string id, DevUINode parentNode, Vector2 pos, string name, string[] items, string? selectedItem) : base(owner, id, parentNode, pos, new Vector2(15f + 2 * BUTTON_WIDTH, 415f), name, items)
			{
				this.selectedItem = selectedItem;
				if (selectedItem != null)
				{
					int index = Array.IndexOf(items, selectedItem);
					currentOffset = (index / perpage) * perpage;
					if (index >= 0)
					{
						PopulateItems(currentOffset);
					}
				}
				else
				{
					PopulateItems(currentOffset);
				}
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
					subNodes.Add(new Button(owner, "BackPageSeeCreature", this, new Vector2(5f, 5f), BUTTON_WIDTH, "Previous"));
					subNodes.Add(new Button(owner, "NextPageSeeCreature", this, new Vector2(10f + BUTTON_WIDTH, 5f), BUTTON_WIDTH, "Next"));
				}

				// Add search thingy
				subNodes.Add(new DevUILabel(owner, "SearchLabelSeeCreature", this, new Vector2(5f, size.y - 25f), widthOfSearchText, SEARCH_LABEL));
				if (searchBar == null)
				{
					searchBar = new StringControlNoSignal(owner, "SearchSeeCreature", this, new Vector2(10f + widthOfSearchText, size.y - 25f), size.x - 15f - widthOfSearchText, filterBy, StringControl.TextIsAny);
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
}
