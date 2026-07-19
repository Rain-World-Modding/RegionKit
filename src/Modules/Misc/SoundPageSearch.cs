using System.Runtime.CompilerServices;
using DevInterface;
using Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using static RegionKit.Modules.Misc.DecalSelectSearch;

namespace RegionKit.Modules.Misc
{
	internal static class SoundPageSearch
	{
		private const string SEARCH_LABEL = "Search: ";
		private static readonly float widthOfSearchText = LabelTest.GetWidth(SEARCH_LABEL);
		private static readonly ConditionalWeakTable<SoundPage, SoundPageSearchBar> _searchCWT = new();

		internal static void Apply()
		{
			IL.DevInterface.SoundPage.RefreshFilesPage += DontClearSearchBar;
			On.DevInterface.SoundPage.RefreshFilesPage += AddSearchBar;
		}

		internal static void Undo()
		{
			IL.DevInterface.SoundPage.RefreshFilesPage -= DontClearSearchBar;
			On.DevInterface.SoundPage.RefreshFilesPage -= AddSearchBar;
		}

		private static void DontClearSearchBar(ILContext il)
		{
			var c = new ILCursor(il);

			c.GotoNext(MoveType.After, x => x.MatchBr(out _));
			c.MoveAfterLabels();

			c.FindNext(out ILCursor[] cursors, x => x.Previous.MatchCallOrCallvirt(typeof(List<DevUINode>).GetMethod("RemoveAt")));
			ILCursor brTo = cursors[0];

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc_1);
			c.EmitDelegate((SoundPage self, int index) => self.soundsPanel.subNodes[index] is SoundPageSearchBar);
			c.Emit(OpCodes.Brtrue, brTo.Next);
		}

		private static void AddSearchBar(On.DevInterface.SoundPage.orig_RefreshFilesPage orig, SoundPage self)
		{
			orig(self);
			
			float searchY = self.soundsPanel.size.y - 16f - 55f;

			// Add search bar if necessary
			if (!_searchCWT.TryGetValue(self, out SoundPageSearchBar searchBar))
			{
				self.soundsPanel.size.y += 20f;
				foreach (DevUINode? node in self.soundsPanel.subNodes)
				{
					if (node is PositionedDevUINode posNode and not AddSoundButton)
					{
						posNode.pos.y += 20f;
					}
				}
				self.soundsPanel.Refresh();
				searchY += 20f;

				searchBar = new SoundPageSearchBar(self.soundsPanel.owner, "SoundSearch99289..?/~", self, new Vector2(10f + widthOfSearchText, searchY), self.soundsPanel.size.x - 15f - widthOfSearchText);
				self.soundsPanel.subNodes.Add(searchBar);
				_searchCWT.Add(self, searchBar);
				orig(self); // refresh to reposition in list
			}

			// Reposition other nodes
			foreach (DevUINode node in self.soundsPanel.subNodes)
			{
				if (node is AddSoundButton soundButton)
				{
					soundButton.pos.y -= 20f;
				}
			}

			// Add search label
			self.soundsPanel.subNodes.Add(new DevUILabel(self.owner, "SearchSearchLabel99289..?/~", self.soundsPanel, new Vector2(5f, searchY), widthOfSearchText, SEARCH_LABEL));
		}

		public class SoundPageSearchBar : StringControl
		{
			private readonly SoundPage _panel;
			private readonly string[] _originalFiles;
			private readonly int _originalTotalPages;

			public string[] FilteredItems
			{
				get
				{
					if (actualValue.Length == 0)
					{
						return _originalFiles;
					}
					return [.. _originalFiles.Where(x => x.IndexOf(actualValue, StringComparison.InvariantCultureIgnoreCase) > -1)];
				}
			}

			public int TotalFilePages
			{
				get
				{
					if (actualValue.Length == 0)
					{
						return _originalTotalPages;
					}
					return 1 + (int)((float)_panel.fileNames.Length / _panel.maxFilesPerPage + 0.5f);
				}
			}

			public SoundPageSearchBar(DevUI owner, string IDstring, SoundPage soundsPage, Vector2 pos, float width) : base(owner, IDstring, soundsPage.soundsPanel, pos, width, "", TextIsAny)
			{
				_panel = soundsPage;
				_originalFiles = soundsPage.fileNames;
				_originalTotalPages = soundsPage.totalFilePages;
				sendSignal = false;

				OnValueChanged += ValueChanged;
			}

			private void ValueChanged(string value, string oldValue)
			{
				if (value != oldValue)
				{
					_panel.fileNames = FilteredItems;
					_panel.currFilesPage = 0;
					_panel.totalFilePages = TotalFilePages;
					_panel.RefreshFilesPage();
				}
			}
		}
	}
}
