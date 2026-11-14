using System.IO;
using DevInterface;
using Menu.Remix.MixedUI;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using SelectDecalPanel = DevInterface.CustomDecalRepresentation.SelectDecalPanel;

namespace RegionKit.Modules.DevUIMisc
{
	public static class DecalSelectTweaks
	{
		private const string SEARCH_LABEL = "Search: ";
		private static readonly float widthOfSearchText = LabelTest.GetWidth(SEARCH_LABEL);

		internal static void Apply()
		{
			IL.DevInterface.CustomDecalRepresentation.SelectDecalPanel.PopulateDecals += DontClearManager;
			On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.PopulateDecals += AddManager;
		}

		internal static void Undo()
		{
			IL.DevInterface.CustomDecalRepresentation.SelectDecalPanel.PopulateDecals -= DontClearManager;
			On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.PopulateDecals -= AddManager;
		}

		private static void DontClearManager(ILContext il)
		{
			var c = new ILCursor(il);

			// Init local variable
			TypeReference searchBar_typeDef = il.Module.ImportReference(typeof(DecalSelectManager));
			VariableDefinition searchBar_var = new VariableDefinition(searchBar_typeDef);
			il.Body.Variables.Add(searchBar_var);

			// Don't clear sprites of decal search bars
			c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<DevUINode>(nameof(DevUINode.ClearSprites)));
			Instruction brTo = c.Next;
			c.Index--;
			c.MoveAfterLabels();
			c.Emit(OpCodes.Ldloc_2);
			c.Emit(OpCodes.Ldloca, searchBar_var);
			c.EmitDelegate((List<DevUINode>.Enumerator enumerator, ref DecalSelectManager? manager) =>
			{
				if (enumerator.Current is DecalSelectManager toStore)
				{
					manager = toStore;
					return true;
				}
				return false;
			});
			c.Emit(OpCodes.Brtrue_S, brTo);

			// Don't remove decal search bars
			c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(List<DevUINode>).GetMethod("Clear")));
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, searchBar_var);
			c.EmitDelegate((SelectDecalPanel self, DecalSelectManager? manager) =>
			{
				if (manager != null)
				{
					self.subNodes.Add(manager);
				}
			});
		}

		private static void AddManager(On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.orig_PopulateDecals orig, SelectDecalPanel self, int offset)
		{
			DecalSelectManager? selectManager = self.subNodes.FirstOrDefaultOfType<DecalSelectManager>();
			orig(self, offset);

			// Add search bar if necessary
			if (selectManager == null)
			{
				self.size.y += 20f;

				foreach (DevUINode node in self.subNodes)
				{
					if (node is PositionedDevUINode posNode and not DecalSelectManager && !self.IDstring.EndsWith("99289..?/~"))
					{
						posNode.pos.y -= 10f;
					}
				}
				self.Refresh();

				selectManager = new DecalSelectManager(self.owner, "DecalSearch99289..?/~", self, new Vector2(10f + widthOfSearchText, self.size.y - 25f), self.size.x - 15f - widthOfSearchText);
				self.subNodes.Add(selectManager);
				//orig(self, offset); // refresh to reposition in list

				// Refresh
				selectManager.UpdateListing();
				return; // UpdateListing() will cause a recursion so we return early as the latter stuff will be applied anyway
			}

			// Reposition other nodes
			foreach (DevUINode node in self.subNodes)
			{
				if (node is PositionedDevUINode posNode and not DecalSelectManager)
				{
					posNode.pos.y -= 20f;
					if (!self.IDstring.EndsWith("99289..?/~"))
					{
						posNode.pos.y -= 10f;
					}
				}
			}

			// Extra setup
			selectManager.SetUpPanel();
		}

		public class DecalSelectManager : StringControl
		{
			private const string DIRECTORY_IDENTIFIER = ":::";

			private readonly SelectDecalPanel _panel;
			private readonly string[] _originalDecals;
			public string currentDirectory = "";

			public bool SearchingForSomething => !string.IsNullOrEmpty(actualValue);
			public bool InSubdirectory => !string.IsNullOrEmpty(currentDirectory) && !SearchingForSomething;

			private bool lastInSubdirectory = false;

			public string[] FilteredItems
			{
				get
				{
					if (SearchingForSomething)
					{
						return [.. _originalDecals.Where(SearchFilter)];
					}
					if (InSubdirectory)
					{
						return [.. _originalDecals.Where(SubdirectoryFilter)];
					}
					return _originalDecals;
				}
			}

			private string[] TransformedNames
			{
				get
				{
					string[] names = FilteredItems;
					if (!SearchingForSomething)
					{
						List<string> returnNames = [];
						HashSet<string> directoriesReturned = [];
						for (int i = 0; i < names.Length; i++)
						{
							string name = InSubdirectory ? names[i][(currentDirectory.Length)..] : names[i];
							int separatorIndex = name.IndexOf('/');
							if (separatorIndex > -1)
							{
								string dir = name[..separatorIndex];
								if (directoriesReturned.Add(dir))
								{
									returnNames.Add(DIRECTORY_IDENTIFIER + dir);
								}
							}
							else
							{
								returnNames.Add(names[i]);
							}
						}
						return returnNames.ToArray();
					}
					return names;
				}
			}

			private bool SearchFilter(string value)
			{
				if (value.IndexOf(actualValue, StringComparison.OrdinalIgnoreCase) > -1)
				{
					return true;
				}
				if (parentNode?.parentNode?.parentNode is CustomDecalRepresentation 
					&& DecalPreview.GetDecalSource(value).IndexOf(actualValue, StringComparison.OrdinalIgnoreCase) > -1)
				{
					return true;
				}
				return false;
			}

			private bool SubdirectoryFilter(string value)
			{
				return value.StartsWith(currentDirectory, StringComparison.OrdinalIgnoreCase);
			}

			public DecalSelectManager(DevUI owner, string IDstring, SelectDecalPanel parentNode, Vector2 pos, float width) : base(owner, IDstring, parentNode, pos, width, "", TextIsAny)
			{
				_panel = parentNode;
				_originalDecals = parentNode.decalNames;
				sendSignal = false;

				OnValueChanged += ValueChanged;
			}

			private void ValueChanged(string value, string oldValue)
			{
				if (value != oldValue)
				{
					UpdateListing();
				}
			}

			public void UpdateListing()
			{
				_panel.decalNames = TransformedNames;
				_panel.PopulateDecals(0);
			}

			public void SetUpPanel()
			{
				// Adjust size
				if (lastInSubdirectory != InSubdirectory)
				{
					if (InSubdirectory)
					{
						_panel.size.y += 20f;
						pos.y += 20f;
					}
					else
					{
						_panel.size.y -= 20f;
						pos.y -= 20f;

						foreach (DevUINode node in _panel.subNodes)
						{
							if (node is PositionedDevUINode posNode and not DecalSelectManager)
							{
								posNode.pos.y -= 20f;
							}
						}
					}
					_panel.Refresh(); // to adjust panel size
				}
				else if (InSubdirectory)
				{
					foreach (DevUINode node in _panel.subNodes)
					{
						if (node is PositionedDevUINode posNode and not DecalSelectManager)
						{
							posNode.pos.y -= 20f;
						}
					}
				}
				lastInSubdirectory = InSubdirectory;

				// Add search label
				_panel.subNodes.Add(new DevUILabel(owner, "DecalSearchLabel99289..?/~", _panel, new Vector2(5f, _panel.size.y - 25f), widthOfSearchText, SEARCH_LABEL));

				// Parent directory button
				if (InSubdirectory)
				{
					_panel.subNodes.Add(new ParentDirectoryButton(owner, "Parent99289..?/~", _panel, new Vector2(5f, _panel.size.y - 50f), _panel.size.x - 10f, this));
				}
				
				// Subdirectory buttons, if applicable
				for (int i = 0; i < _panel.subNodes.Count; i++)
				{
					if (_panel.subNodes[i] is Button button and not (ParentDirectoryButton or SubdirectoryButton) && !button.IDstring.EndsWith("99289..?/~"))
					{
						if (button.IDstring.StartsWith(DIRECTORY_IDENTIFIER))
						{
							button.ClearSprites();
							_panel.subNodes[i] = new SubdirectoryButton(this, button, button.IDstring[DIRECTORY_IDENTIFIER.Length..]);
						}
						else
						{
							button.Text = Path.GetFileName(button.Text);
						}
					}
				}
			}

			private class ParentDirectoryButton : Button
			{
				private readonly DecalSelectManager _manager;

				public ParentDirectoryButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, DecalSelectManager manager) 
					: base(owner, IDstring, parentNode, pos, width, "Parent folder")
				{
					_manager = manager;
				}

				public override void Clicked()
				{
					owner.mouseClick = false; // make sure we don't trigger any other buttons after updating
					_manager.currentDirectory = Path.GetDirectoryName(_manager.currentDirectory[..^1]) ?? ""; // have to trim off the last char as it will be a /
					if (!string.IsNullOrEmpty(_manager.currentDirectory))
					{
						_manager.currentDirectory += "/"; // and then have to add this because GetDirectoryName doesn't keep the /
					}
					_manager.UpdateListing();
				}
			}

			private class SubdirectoryButton : Button
			{
				private readonly DecalSelectManager _manager;
				private readonly FSprite _folderSprite;
				private readonly string _directoryName;

				public SubdirectoryButton(DecalSelectManager manager, Button inheritFromButton, string name) 
					: base(inheritFromButton.owner, inheritFromButton.IDstring, inheritFromButton.parentNode, inheritFromButton.pos, inheritFromButton.size.x, name)
				{
					_manager = manager;
					_directoryName = name;

					_folderSprite = new FSprite("assets/regionkit/sprites/rk_folder_symbol")
					{
						anchorX = 0f,
						anchorY = 0f,
					};
					fSprites.Add(_folderSprite);
					Futile.stage.AddChild(_folderSprite);
				}

				public override void Refresh()
				{
					base.Refresh();
					MoveLabel(0, absPos + Vector2.right * 20f);
					_folderSprite.SetPosition(absPos + new Vector2(2.01f, 1.01f));
					_folderSprite.color = fLabels[0].color;
				}

				public override void Clicked()
				{
					owner.mouseClick = false; // make sure we don't trigger any other buttons after updating
					_manager.currentDirectory += _directoryName + "/";
					_manager.UpdateListing();
				}
			}
		}
	}
}
