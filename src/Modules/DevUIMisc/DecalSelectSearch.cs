using System.Runtime.CompilerServices;
using DevInterface;
using Menu.Remix.MixedUI;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Cil;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using SelectDecalPanel = DevInterface.CustomDecalRepresentation.SelectDecalPanel;

namespace RegionKit.Modules.DevUIMisc
{
	public static class DecalSelectSearch
	{
		private const string SEARCH_LABEL = "Search: ";
		private static readonly float widthOfSearchText = LabelTest.GetWidth(SEARCH_LABEL);

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

			// Init local variable
			TypeReference searchBar_typeDef = il.Module.ImportReference(typeof(DecalSelectSearchBar));
			VariableDefinition searchBar_var = new VariableDefinition(searchBar_typeDef);
			il.Body.Variables.Add(searchBar_var);

			// Don't clear sprites of decal search bars
			c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<DevUINode>(nameof(DevUINode.ClearSprites)));
			Instruction brTo = c.Next;
			c.Index--;
			c.MoveAfterLabels();
			c.Emit(OpCodes.Ldloc_2);
			c.Emit(OpCodes.Ldloca, searchBar_var);
			c.EmitDelegate((List<DevUINode>.Enumerator enumerator, ref DecalSelectSearchBar? searchBar) =>
			{
				if (enumerator.Current is DecalSelectSearchBar toStore)
				{
					searchBar = toStore;
					return true;
				}
				return false;
			});
			c.Emit(OpCodes.Brtrue_S, brTo);

			// Don't remove decal search bars
			c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(List<DevUINode>).GetMethod("Clear")));
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, searchBar_var);
			c.EmitDelegate((SelectDecalPanel self, DecalSelectSearchBar? searchBar) =>
			{
				if (searchBar != null)
				{
					self.subNodes.Add(searchBar);
				}
			});
		}

		private static void AddSearchBar(On.DevInterface.CustomDecalRepresentation.SelectDecalPanel.orig_PopulateDecals orig, SelectDecalPanel self, int offset)
		{
			DecalSelectSearchBar? searchBar = self.subNodes.FirstOrDefaultOfType<DecalSelectSearchBar>();
			orig(self, offset);

			// Add search bar if necessary
			if (searchBar == null)
			{
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
				orig(self, offset); // refresh to reposition in list
			}

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
							|| parentNode?.parentNode?.parentNode is CustomDecalRepresentation && DecalPreview.GetDecalSource(x).IndexOf(actualValue, StringComparison.InvariantCultureIgnoreCase) > -1)];
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
