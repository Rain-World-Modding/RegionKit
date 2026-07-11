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
				if (node is PositionedDevUINode positionedNode && node.IDstring != "Event_Button")
				{
					positionedNode.pos.y += 20f;
				}
			}

			// Add custom control
			triggerPanel.subNodes.Add(new SeeCreatureSelectButton(triggerPanel.owner, "SeeCreatureSelectButton", triggerPanel, new Vector2(5f, 25f), triggerPanel.size.x - 10f, trigger));
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
				if (sender.IDstring == "BackPage99289..?/~")
				{
					selectPanel.PrevPage();
				}
				else if (sender.IDstring == "NextPage99289..?/~")
				{
					selectPanel.NextPage();
				}
				else if (sender.parentNode == selectPanel && sender.IDstring != "Search99289..?/~")
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
				return new SearchableSelectPanel(maker.owner, "SeeCreatureSelectPanel", maker, new Vector2(250f, 15f) - maker.absPos, "Select Creature Type", [.. CreatureTemplate.Type.values.entries.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)], (maker as SeeCreatureSelectButton)?.trigger.creatureType.value);
			}
		}
	}
}
