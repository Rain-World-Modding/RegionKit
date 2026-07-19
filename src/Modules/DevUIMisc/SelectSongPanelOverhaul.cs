using DevInterface;
using Menu.Remix.MixedUI;
using Music;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.DevUIMisc
{
	public static class SelectSongPanelOverhaul
	{
		const float BUTTON_WIDTH = 200f;
		const int COLUMNS = 3;
		const int PER_COLUMN = 30;

		public static readonly MusicPlayer.MusicContext DevToolsContext = new MusicPlayer.MusicContext("DevTools", true);

		internal static void Apply()
		{
			On.DevInterface.SelectSongPanel.ctor += SelectSongPanel_ctor;
		}

		internal static void Undo()
		{
			On.DevInterface.SelectSongPanel.ctor -= SelectSongPanel_ctor;
		}

		private static void SelectSongPanel_ctor(On.DevInterface.SelectSongPanel.orig_ctor orig, SelectSongPanel self, DevUI owner, DevUINode parentNode, Vector2 pos, string[] songNames)
		{
			orig(self, owner, parentNode, pos, songNames);
			foreach (DevUINode subNode in self.subNodes)
			{
				subNode.ClearSprites();
			}
			self.subNodes.Clear();

			self.size = new Vector2((COLUMNS + 1) * 5f + COLUMNS * BUTTON_WIDTH, 60f + PER_COLUMN * 20f);
			self.Refresh();

			var manager = new SelectSongPanelManager(owner, "Select_Song_Manager", self, songNames);
			self.subNodes.Add(manager);
			manager.PopulateItems(0);
		}

		public sealed class SelectSongPanelManager : PositionedDevUINode, IDevUISignals
		{
			private const string SEARCH_LABEL = "Search: ";
			private static readonly float widthOfSearchText = LabelTest.GetWidth(SEARCH_LABEL);

			private readonly SelectSongPanel songPanel;
			private readonly string[] songList;
			public int currentOffset = 0;
			private readonly int perpage;
			private readonly int columns;
			private readonly StringControl searchBar;
			private readonly Button prevButton, nextButton;
			public string filterBy = "";

			private readonly List<DevUINode> tempNodes = [];

			public string[] FilteredItems
			{
				get
				{
					if (filterBy.Length == 0)
					{
						return songList;
					}
					return [.. songList.Where(x => x.IndexOf(filterBy, StringComparison.InvariantCultureIgnoreCase) > -1)];
				}
			}

			public SelectSongPanelManager(DevUI owner, string IDstring, SelectSongPanel songPanel, string[] songList) : base(owner, IDstring, songPanel, Vector2.zero)
			{
				this.songPanel = songPanel;
				this.songList = songList;
				currentOffset = 0;
				columns = COLUMNS;
				perpage = PER_COLUMN * COLUMNS;

				// Note: we add the buttons, search bar, etc *here* instead of in the song panel so that parentNode does not equal the song panel, which is important

				// Add buttons
				float navButtonWidth = (songPanel.size.x - 15f) / 2f;
				subNodes.Add(prevButton = new Button(owner, "SongBackPage99289..?/~", this, new Vector2(5f, 5f), navButtonWidth, "Previous"));
				subNodes.Add(nextButton = new Button(owner, "SongNextPage99289..?/~", this, new Vector2(10f + navButtonWidth, 5f), navButtonWidth, "Next"));

				// Add search thingy
				subNodes.Add(new DevUILabel(owner, "SongSearchLabel99289..?/~", this, new Vector2(5f, songPanel.size.y - 25f), widthOfSearchText, SEARCH_LABEL));
				if (searchBar == null)
				{
					searchBar = new StringControl(owner, "SongSearch99289..?/~", this, new Vector2(10f + widthOfSearchText, songPanel.size.y - 25f), songPanel.size.x - 15f - widthOfSearchText, filterBy, StringControl.TextIsAny) { sendSignal = false };
					subNodes.Add(searchBar);
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

			public void PopulateItems(int offset)
			{
				currentOffset = offset;

				// Clear out previous sprites
				foreach (DevUINode node in tempNodes)
				{
					node.ClearSprites();
					node.parentNode.subNodes.Remove(node);
				}
				tempNodes.Clear();

				// Get filtered items
				string[] filteredItems = FilteredItems;

				// Add buttons
				IntVector2 intVector = new IntVector2(0, 0);
				int off = currentOffset;
				bool addPlayMusicButtons = owner.game.manager.musicPlayer != null && owner.game.rainWorld.setup.playMusic;
				float useButtonWidth = BUTTON_WIDTH - (addPlayMusicButtons ? 25f : 0f);
				while (off < filteredItems.Length && off < currentOffset + perpage)
				{
					var buttonPos = new Vector2(5f + intVector.x * (BUTTON_WIDTH + 5f), songPanel.size.y - 50f - 20f * intVector.y);
					var button = new Button(owner, filteredItems[off], songPanel, buttonPos, useButtonWidth, filteredItems[off]);
					tempNodes.Add(button);
					songPanel.subNodes.Add(button);
					if (addPlayMusicButtons)
					{
						var playMusicButton = new PlaySongButton(owner, "Play_" + filteredItems[off], this, buttonPos + new Vector2(5f + useButtonWidth, 0f), 15f, filteredItems[off]);
						tempNodes.Add(playMusicButton);
						subNodes.Add(playMusicButton);
					}
					intVector.y++;
					if (intVector.y >= (int)Mathf.Floor((float)perpage / columns))
					{
						intVector.x++;
						intVector.y = 0;
					}
					off++;
				}
			}

			public void PrevPage()
			{
				currentOffset -= perpage;
				if (currentOffset < 0)
				{
					currentOffset = 0;
				}
				PopulateItems(currentOffset);
			}

			public void NextPage()
			{
				currentOffset += perpage;
				if (currentOffset > songList.Length)
				{
					currentOffset = perpage * (int)Mathf.Floor((float)songList.Length / perpage);
				}
				PopulateItems(currentOffset);
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (sender == prevButton)
				{
					PrevPage();
				}
				else if (sender == nextButton)
				{
					NextPage();
				}
			}

			public class PlaySongButton : Button
			{
				private readonly string songName;
				private readonly FSprite symbol;
				private readonly FLabel label;

				public PlaySongButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string songName) : base(owner, IDstring, parentNode, pos, width, "")
				{
					this.songName = songName;
					label = fLabels[0];
					symbol = new FSprite("musicSymbol") { anchorX = 0.5f, anchorY = 0.5f, scale = 0.75f };
					fSprites.Add(symbol);
					Futile.stage.AddChild(symbol);
				}

				public override void Update()
				{
					base.Update();
					symbol.color = label.color;
				}

				public override void Refresh()
				{
					base.Refresh();
					symbol.SetPosition(absPos + size / 2f + new Vector2(0.01f, 0.01f));
				}

				public override void Clicked()
				{
					MusicPlayer musicPlayer = owner.game.manager.musicPlayer;
					if (musicPlayer != null)
					{
						// Stop any current songs
						string? oldSongName = musicPlayer.song?.name;
						musicPlayer.FadeOutAllSongs(0f);
						musicPlayer.song?.StopAndDestroy();
						musicPlayer.song = null;

						// Play new song if it wasn't already playing
						if (oldSongName == null || !oldSongName.Equals(songName, StringComparison.OrdinalIgnoreCase))
						{
							var song = new Song(musicPlayer, songName, DevToolsContext)
							{
								playWhenReady = true,
								fadeInTime = 0f,
								baseVolume = owner.game.manager.rainWorld.options.musicVolume,
								priority = 1.5f,
								stopAtGate = true,
								stopAtDeath = true,
								Loop = false,
							};
							musicPlayer.song = song;
						}
					}
				}
			}
		}
	}
}
