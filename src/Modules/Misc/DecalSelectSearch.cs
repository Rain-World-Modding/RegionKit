using System.Runtime.CompilerServices;
using DevInterface;
using Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RegionKit.Modules.DevUIMisc.GenericNodes;

using SelectDecalPanel = DevInterface.CustomDecalRepresentation.SelectDecalPanel;

namespace RegionKit.Modules.Misc
{
	public static class DecalSelectSearch
	{
		private const string SEARCH_LABEL = "Search: ";
		private static readonly float widthOfSearchText = LabelTest.GetWidth(SEARCH_LABEL);
		private static readonly ConditionalWeakTable<SelectDecalPanel, DecalSelectSearchBar> _searchCWT = new();

		internal static void Apply()
		{
			IL.DevInterface.CustomDecalRepresentation.SelectDecalPanel.PopulateDecals += DontClearSearchBar;
			On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.PopulateDecals += AddSearchBar;
		}

		internal static void Undo()
		{
			IL.DevInterface.CustomDecalRepresentation.SelectDecalPanel.PopulateDecals -= DontClearSearchBar;
			On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.PopulateDecals -= AddSearchBar;
		}

		private static void DontClearSearchBar(ILContext il)
		{
			var c = new ILCursor(il);

			// Don't clear sprites of decal search bars
			c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<DevUINode>(nameof(DevUINode.ClearSprites)));
			Instruction brTo = c.Next;
			c.Index--;
			c.MoveAfterLabels();
			c.Emit(OpCodes.Ldloc_2);
			c.EmitDelegate((List<DevUINode>.Enumerator enumerator) =>
			{
				return enumerator.Current is DecalSelectSearchBar;
			});
			c.Emit(OpCodes.Brtrue_S, brTo);

			// Don't remove decal search bars
			c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(List<DevUINode>).GetMethod("Clear")));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((SelectDecalPanel self) =>
			{
				if (_searchCWT.TryGetValue(self, out DecalSelectSearchBar searchBar))
				{
					self.subNodes.Add(searchBar);
				}
			});
		}

		private static void AddSearchBar(On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.orig_PopulateDecals orig, SelectDecalPanel self, int offset)
		{
			orig(self, offset);

			if (!_searchCWT.TryGetValue(self, out DecalSelectSearchBar searchBar))
			{
				// Add search bar if necessary
				self.size.y += 20f;

				foreach (DevUINode node in self.subNodes)
				{
					if (node is PositionedDevUINode posNode and not DecalSelectSearchBar && !self.IDstring.EndsWith("99289..?/~"))
					{
						posNode.pos.y -= 10f;
					}
				}
				self.Refresh();

				searchBar = new DecalSelectSearchBar(self.owner, "DecalSearch99289..?/~", self, new Vector2(10f + widthOfSearchText, self.size.y - 25f), self.size.x - 15f - widthOfSearchText);
				self.subNodes.Add(searchBar);
				_searchCWT.Add(self, searchBar);
			}
			else
			{
				// Reposition other nodes
				foreach (DevUINode node in self.subNodes)
				{
					if (node is PositionedDevUINode posNode and not DecalSelectSearchBar)
					{
						posNode.pos.y -= 20f;
						if (!self.IDstring.EndsWith("99289..?/~"))
						{
							posNode.pos.y -= 10f;
						}
					}
				}
			}

			// Add search label
			self.subNodes.Add(new DevUILabel(self.owner, "DecalSearchLabel99289..?/~", self, new Vector2(5f, self.size.y - 25f), widthOfSearchText, SEARCH_LABEL));
		}

		public class DecalSelectSearchBar : StringControl
		{
			private readonly SelectDecalPanel _panel;
			private readonly string[] _originalDecals;

			public string[] FilteredItems
			{
				get
				{
					if (actualValue.Length == 0)
					{
						return _originalDecals;
					}
					return [.. _originalDecals.Where(
						x => x.IndexOf(actualValue, StringComparison.InvariantCultureIgnoreCase) > -1 
							|| (parentNode?.parentNode?.parentNode is CustomDecalRepresentation && DecalPreview.GetDecalSource(x).IndexOf(actualValue, StringComparison.InvariantCultureIgnoreCase) > -1))];
				}
			}

			public DecalSelectSearchBar(DevUI owner, string IDstring, SelectDecalPanel parentNode, Vector2 pos, float width) : base(owner, IDstring, parentNode, pos, width, "", TextIsAny)
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
					_panel.decalNames = FilteredItems;
					_panel.PopulateDecals(0);
				}
			}
		}
	}
}
