using System.Globalization;
using System.Text.RegularExpressions;
using DevInterface;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.Triggers
{
	public class PickUpObjectTrigger : EventTrigger, ICustomTrigger
	{
		public AbstractPhysicalObject.AbstractObjectType objectType;

		public PickUpObjectTrigger() : base(_Enums.PickUpObjectTrigger)
		{
			objectType = AbstractPhysicalObject.AbstractObjectType.Spear;
		}

		public override string ToString()
		{
			string text = BaseSaveString() + string.Format(CultureInfo.InvariantCulture, 
				"<tA>objectType<tB>{0}",
				objectType.value
				);
			foreach (KeyValuePair<string, string> keyValuePair in unrecognizedSaveStrings)
			{
				text = string.Concat([text, "<tA>", keyValuePair.Key, "<tB>", keyValuePair.Value]);
			}
			return text;
		}

		public override void FromString(string[] s)
		{
			base.FromString(s);
			for (int i = 0; i < s.Length; i++)
			{
				string[] array = Regex.Split(s[i], "<tB>");
				string text = array[0];
				switch (text)
				{
					case "objectType":
						objectType = new AbstractPhysicalObject.AbstractObjectType(array[1], false);
						break;
				}
			}

			unrecognizedSaveStrings.Remove("objectType");
		}

		public bool PerformWait => false;

		public bool CheckCondition(Player player, Room room)
		{
			return objectType != null && objectType.Index > -1 && player.grasps.Any(x => x?.grabbed?.abstractPhysicalObject.type == objectType);
		}

		public void InitAtPosition(Vector2 pos)
		{
		}

		public void InitDevUI(TriggerPanel triggerPanel)
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
			triggerPanel.subNodes.Add(new PickItemSelectButton(triggerPanel.owner, "PickItemSelectButton", triggerPanel, new Vector2(5f, 25f), triggerPanel.size.x - 10f, this));
		}

		private class PickItemSelectButton : ButtonWithSelectPanel
		{
			private readonly PickUpObjectTrigger trigger;
			public PickItemSelectButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, PickUpObjectTrigger trigger) : base(owner, IDstring, parentNode, pos, width, "Object type: " + trigger.objectType, SelectPanelMaker)
			{
				this.trigger = trigger;
			}

			public override void OnValueChange(string value)
			{
				AbstractPhysicalObject.AbstractObjectType type = new AbstractPhysicalObject.AbstractObjectType(value, false);
				trigger.objectType = type;
				Text = $"Object type: {type}";
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
				return new SearchableSelectPanel(maker.owner, "PickUpObjectSelectPanel", maker, new Vector2(250f, 15f) - maker.absPos, "Select Object Type", [.. AbstractPhysicalObject.AbstractObjectType.values.entries.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)], (maker as PickItemSelectButton)?.trigger.objectType.value);
			}
		}
	}
}
