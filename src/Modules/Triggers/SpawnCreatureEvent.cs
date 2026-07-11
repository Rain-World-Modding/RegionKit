using System.Globalization;
using System.Text.RegularExpressions;
using DevInterface;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.Triggers
{
	public class SpawnCreatureEvent : TriggeredEvent, ICustomEvent
	{
		private const int RANDOM_NODE = -3;
		private const int RANDOM_DEN = -2;
		private const int RANDOM_EXIT = -1;

		private const int NODE_RESET_VALUE = RANDOM_NODE;

		public CreatureTemplate.Type creatureType;
		public string creatureTags;
		public int creatureQuantity = 1;
		public int nodeToSpawnFrom = -2;
		public bool useSpecificId = false;
		public int creatureId = -1;
		public bool saveCreature = false;

		public SpawnCreatureEvent() : base(_Enums.SpawnCreatureEvent)
		{
			creatureType = CreatureTemplate.Type.DaddyLongLegs;
			creatureTags = "";
			creatureId = UnityEngine.Random.Range(1000, 10000);
		}

		public override string ToString()
		{
			string text = base.ToString() + string.Format(CultureInfo.InvariantCulture, 
				"<eA>{0}<eB>{1}<eA>{2}<eB>{3}<eA>{4}<eB>{5}<eA>{6}<eB>{7}<eA>{8}<eB>{9}<eA>{10}<eB>{11}<eA>{12}<eB>{13}",
				nameof(creatureType),
				creatureType.value,
				nameof(creatureTags),
				creatureTags,
				nameof(creatureQuantity),
				creatureQuantity,
				nameof(nodeToSpawnFrom),
				nodeToSpawnFrom,
				nameof(useSpecificId),
				useSpecificId ? "1" : "0",
				nameof(creatureId),
				creatureId,
				nameof(saveCreature),
				saveCreature ? "1" : "0"
				);
			foreach (string saveStr in unrecognizedSaveStrings)
			{
				text = text + "<eA>" + saveStr;
			}
			return text;
		}

		public override void FromString(string[] s)
		{
			base.FromString(s);
			unrecognizedSaveStrings.Clear();
			int i = 0;
			while (i < s.Length)
			{
				string[] array = Regex.Split(s[i], "<eB>");
				string str = array[0];
				switch (str)
				{
					case nameof(creatureType):
						creatureType = new CreatureTemplate.Type(array[1], false);
						break;
					case nameof(creatureTags):
						creatureTags = array[1];
						break;
					case nameof(creatureQuantity):
						creatureQuantity = int.Parse(array[1], CultureInfo.InvariantCulture);
						break;
					case nameof(nodeToSpawnFrom):
						nodeToSpawnFrom = int.Parse(array[1], CultureInfo.InvariantCulture);
						break;
					case nameof(useSpecificId):
						useSpecificId = array[1] == "1";
						break;
					case nameof(creatureId):
						creatureId = int.Parse(array[1], CultureInfo.InvariantCulture);
						break;
					case nameof(saveCreature):
						saveCreature = array[1] == "1";
						break;
					default:
						if (s[i].Trim().Length > 0 && array.Length >= 2)
						{
							unrecognizedSaveStrings.Add(s[i]);
						}
						break;
				}
				i++;
			}
		}

		public bool DefaultMultiUse => false;

		public void Fire(EventTrigger trigger, Room room)
		{
			LogInfo("Triggered!");
			if (creatureType.Index > -1 && nodeToSpawnFrom < room.abstractRoom.nodes.Length)
			{
				for (int i = 0; i < creatureQuantity; i++)
				{
					var wc = new WorldCoordinate(room.abstractRoom.index, -1, -1, NodeToSpawnFrom(room));
					var ac = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(creatureType), null, wc, room.game.GetNewID());
					if (useSpecificId)
					{
						ac.ID.setAltSeed(creatureId);
					}
					ac.spawnData = $"{{{creatureTags}}}";
					ac.setCustomFlags();
					room.abstractRoom.AddEntity(ac);
					ac.RealizeInRoom();
				}
			}
		}

		private int NodeToSpawnFrom(Room room)
		{
			return nodeToSpawnFrom switch
			{
				RANDOM_NODE => UnityEngine.Random.Range(0, room.abstractRoom.nodes.Length),
				RANDOM_DEN => UnityEngine.Random.Range(room.abstractRoom.nodes.FirstIndexOr(IsDen, 0), room.abstractRoom.nodes.LastIndexOr(IsDen, room.abstractRoom.nodes.Length - 1) + 1),
				RANDOM_EXIT => UnityEngine.Random.Range(room.abstractRoom.nodes.FirstIndexOr(IsExit, 0), room.abstractRoom.nodes.LastIndexOr(IsExit, room.abstractRoom.nodes.Length - 1) + 1),
				_ when nodeToSpawnFrom >= room.abstractRoom.nodes.Length => UnityEngine.Random.Range(0, room.abstractRoom.nodes.Length),
				_ => nodeToSpawnFrom
			};

			static bool IsDen(AbstractRoomNode x) => x.type == AbstractRoomNode.Type.Den;
			static bool IsExit(AbstractRoomNode x) => x.type == AbstractRoomNode.Type.Exit;
		}

		public StandardEventPanel? InitDevUIPanel(TriggerPanel triggerPanel)
		{
			return new SpawnCreatureEventPanel(triggerPanel.owner, triggerPanel, this);
		}

		private class SpawnCreatureEventPanel : StandardEventPanel, IDevUISignals
		{
			private bool hasInit = false;
			private readonly SpawnCreatureEvent evnt;
			private readonly StringControl creatureQuantityTextbox;
			private readonly StringControl creatureTagsTextbox;
			private readonly Button nodePickerButton;
			private readonly Button saveSpawnButton;
			private readonly Button useSpecificIdButton;
			private readonly StringControl useSpecificIdTextbox;
			private readonly FSprite nodePickerLine;

			public SpawnCreatureEventPanel(DevUI owner, DevUINode parentNode, SpawnCreatureEvent evnt) : base(owner, parentNode, 175f)
			{
				this.evnt = evnt;
				subNodes.Add(new SpawnCreatureSelectButton(owner, "SpawnCreatureSelectButton", this, new Vector2(5f, size.y - 45f), 235f, evnt));
				subNodes.Add(new DevUILabel(owner, "SpawnCreatureQuantityLabel", this, new Vector2(5f, size.y - 65f), 75f, "Quantity:"));
				subNodes.Add(creatureQuantityTextbox = new StringControl(owner, "SpawnCreatureQuantityTextbox", this, new Vector2(85f, size.y - 65f), 155f, evnt.creatureQuantity.ToString(), StringControl.TextIsIntNonNegative));
				subNodes.Add(new DevUILabel(owner, "SpawnCreatureTagsLabel", this, new Vector2(5f, size.y - 85f), 75f, "Spawn tags:"));
				subNodes.Add(creatureTagsTextbox = new StringControl(owner, "SpawnCreatureTagsTextbox", this, new Vector2(85f, size.y - 85f), 155f, evnt.creatureTags, StringControl.TextIsAny));
				subNodes.Add(nodePickerButton = new Button(owner, "SpawnCreatureNodePicker", this, new Vector2(5f, size.y - 105f), 235f, ""));
				subNodes.Add(saveSpawnButton = new Button(owner, "SpawnCreatureSaveSpawn", this, new Vector2(5f, size.y - 125f), 235f, ""));

				subNodes.Add(useSpecificIdButton = new Button(owner, "SpawnCreatureUseID", this, new Vector2(5f, size.y - 150f), 235f, ""));
				subNodes.Add(new DevUILabel(owner, "SpawnCreatureUseIDLabel", this, new Vector2(5f, size.y - 170f), 75f, "Specific id:"));
				subNodes.Add(useSpecificIdTextbox = new StringControl(owner, "SpawnCreatureUseIDTextbox", this, new Vector2(85f, size.y - 170f), 155f, evnt.creatureId.ToString(), StringControl.TextIsInt));

				nodePickerLine = new FSprite("pixel") { anchorY = 0f };
				fSprites.Add(nodePickerLine);
				Futile.stage.AddChild(nodePickerLine);

				hasInit = true;
				Refresh();
			}

			public override void Refresh()
			{
				base.Refresh();
				if (!hasInit) return;

				nodePickerButton.Text = NodePickerText(evnt.nodeToSpawnFrom, owner.room);
				saveSpawnButton.Text = evnt.saveCreature ? "Creature will save" : "Creature will NOT save";
				useSpecificIdButton.Text = evnt.useSpecificId ? "Creature uses specific id visuals" : "Creature uses random id";

				if (evnt.nodeToSpawnFrom < 0 || evnt.nodeToSpawnFrom >= owner.room.abstractRoom.nodes.Length)
				{
					nodePickerLine.isVisible = false;
				}
				else
				{
					nodePickerLine.isVisible = true;
					nodePickerLine.SetPosition(nodePickerButton.absPos);
					Vector2 lineStart = nodePickerButton.absPos, lineEnd = owner.room.MiddleOfTile(owner.room.LocalCoordinateOfNode(evnt.nodeToSpawnFrom));
					nodePickerLine.rotation = AimFromOneVectorToAnother(lineStart, lineEnd);
					nodePickerLine.scaleY = Vector2.Distance(lineStart, lineEnd);
					nodePickerLine.color = NodeLineColor(owner.room.abstractRoom.nodes[evnt.nodeToSpawnFrom].type);
				}
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (sender == nodePickerButton)
				{
					evnt.nodeToSpawnFrom++;
					if (evnt.nodeToSpawnFrom >= owner.room.abstractRoom.nodes.Length)
					{
						evnt.nodeToSpawnFrom = NODE_RESET_VALUE;
					}
					Refresh();
				}
				else if (sender == saveSpawnButton)
				{
					evnt.saveCreature = !evnt.saveCreature;
					Refresh();
				}
				else if (sender == useSpecificIdButton)
				{
					evnt.useSpecificId = !evnt.useSpecificId;
					Refresh();
				}
				else if (sender == useSpecificIdTextbox && int.TryParse(useSpecificIdTextbox.actualValue, NumberStyles.Any, CultureInfo.InvariantCulture, out int id))
				{
					evnt.creatureId = id;
				}
				else if (sender == creatureQuantityTextbox && int.TryParse(creatureQuantityTextbox.actualValue, NumberStyles.Any, CultureInfo.InvariantCulture, out int quantity))
				{
					evnt.creatureQuantity = quantity;
				}
				else if (sender == creatureTagsTextbox)
				{
					evnt.creatureTags = creatureTagsTextbox.actualValue;
				}
			}

			private static string NodePickerText(int node, Room room)
			{
				return node switch
				{
					RANDOM_NODE => "Random node",
					RANDOM_DEN => "Random den",
					RANDOM_EXIT => "Random entrance",
					_ when node < room.abstractRoom.nodes.Length => $"Node: {node} ({room.abstractRoom.nodes[node].type.value})",
					_ => "INVALID NODE"
				};
			}

			private static Color NodeLineColor(AbstractRoomNode.Type nodeType)
			{
				if (nodeType == AbstractRoomNode.Type.Exit)
				{
					return new Color(1f, 1f, 1f);
				}
				else if (nodeType == AbstractRoomNode.Type.Den)
				{
					return new Color(1f, 0f, 1f);
				}
				else if (nodeType == AbstractRoomNode.Type.RegionTransportation)
				{
					return new Color(0.2f, 0.2f, 0.2f);
				}
				else if (nodeType == AbstractRoomNode.Type.SideExit)
				{
					return new Color(0.5f, 0.85f, 0.5f);
				}
				else if (nodeType == AbstractRoomNode.Type.SkyExit)
				{
					return new Color(0.2f, 0.85f, 1f);
				}
				else if (nodeType == AbstractRoomNode.Type.SeaExit)
				{
					return new Color(0f, 0f, 1f);
				}
				else if (nodeType == AbstractRoomNode.Type.BatHive)
				{
					return new Color(0f, 1f, 0.2f);
				}
				else if (nodeType == AbstractRoomNode.Type.GarbageHoles)
				{
					return new Color(1f, 0.5f, 0f);
				}
				return Color.white;
			}
		}

		private class SpawnCreatureSelectButton : ButtonWithSelectPanel
		{
			private readonly SpawnCreatureEvent evnt;
			public SpawnCreatureSelectButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, SpawnCreatureEvent evnt) : base(owner, IDstring, parentNode, pos, width, "Creature: " + evnt.creatureType.value, SelectPanelMaker)
			{
				this.evnt = evnt;
			}

			public override void OnValueChange(string value)
			{
				CreatureTemplate.Type type = new CreatureTemplate.Type(value, false);
				evnt.creatureType = type;
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
				return new SearchableSelectPanel(maker.owner, "SpawnCreatureSelectPanel", maker, new Vector2(250f, 15f) - maker.absPos, "Select Creature Type", [.. CreatureTemplate.Type.values.entries.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)], (maker as SpawnCreatureSelectButton)?.evnt.creatureType.value);
			}
		}
	}
}
