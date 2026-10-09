using DevInterface;

namespace RegionKit.Modules.DevUIMisc
{
	public static class RoomAttractivenessPanelOverhaul
	{
		internal static void Apply()
		{
			On.DevInterface.RoomAttractivenessPanel.ctor += RoomAttractivenessPanel_ctor;
			On.DevInterface.RoomAttractivenessPanel.RoomClicked += RoomAttractivenessPanel_RoomClicked;
		}

		internal static void Undo()
		{
			On.DevInterface.RoomAttractivenessPanel.ctor -= RoomAttractivenessPanel_ctor;
			On.DevInterface.RoomAttractivenessPanel.RoomClicked -= RoomAttractivenessPanel_RoomClicked;
		}

		private static void RoomAttractivenessPanel_RoomClicked(On.DevInterface.RoomAttractivenessPanel.orig_RoomClicked orig, RoomAttractivenessPanel self, int r)
		{
			// don't click rooms beneath the panel
			if (self.MouseOver) return;
			orig(self, r);
		}

		private static void RoomAttractivenessPanel_ctor(On.DevInterface.RoomAttractivenessPanel.orig_ctor orig, RoomAttractivenessPanel self, DevUI owner, World world, string IDstring, DevUINode parentNode, Vector2 pos, string title, DevInterface.MapPage mapPage)
		{
			orig(self, owner, world, IDstring, parentNode, pos, title, mapPage);

			// Don't create unless necessary
			if (self.subNodes.OfType<RoomAttractivenessPanel.CreatureButton>().Count() > 32 * 3 - 2)
			{
				self.subNodes.Add(new RoomAttractivenessPanelManager(owner, IDstring, self, Vector2.zero));
			}
		}

		public class RoomAttractivenessPanelManager : PositionedDevUINode
		{
			public RoomAttractivenessPanel RAP => (parentNode as RoomAttractivenessPanel)!;

			public IEnumerable<RoomAttractivenessPanel.CreatureButton> CreatureButtons => RAP.subNodes.OfType<RoomAttractivenessPanel.CreatureButton>().Where(x => !x.Category);
			public IEnumerable<RoomAttractivenessPanel.CreatureButton> CategoryButtons => RAP.subNodes.OfType<RoomAttractivenessPanel.CreatureButton>().Where(x => x.Category);

			public int ButtonSpace => 32 * 3 - 2 - CategoryButtons.Count();
			public int TotalPages
			{
				get
				{
					int totalSpace = ButtonSpace;
					int totalButtons = CreatureButtons.Count();
					return (totalButtons + totalSpace - 1) / totalSpace;
				}
			}

			public int page;
			public Button nextButton;
			public Button prevButton;

			public RoomAttractivenessPanelManager(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
			{
				page = 0;
				subNodes.Add(nextButton = new PageButton(owner, "Next", this, new Vector2(), 100f, "Next page", 1));
				subNodes.Add(prevButton = new PageButton(owner, "Prev", this, new Vector2(), 100f, "Previous page", -1));
				OrganizeButtons();
			}

			public void TurnPage(int dir)
			{
				page += Math.Sign(dir);
				if (page < 0)
				{
					page += TotalPages;
				}
				else
				{
					page %= TotalPages;
				}
				OrganizeButtons();
			}

			public void OrganizeButtons()
			{
				int totalSpace = ButtonSpace;
				RAP.Refresh();
				int startIndex = totalSpace * page;
				int endIndex = startIndex + totalSpace;
				int i = 0;
				int row = 0;
				int col = 0;
				foreach (RoomAttractivenessPanel.CreatureButton button in CreatureButtons)
				{
					if (i >= startIndex && i < endIndex)
					{
						button.Move(new Vector2(5f + col * 120f, 680f - 20f * row));
						row++;
						if (row > 31)
						{
							row = 0;
							col++;
						}
					}
					else
					{
						button.Move(new Vector2(-9999f, -9999f));
					}
					i++;
				}

				row = totalSpace % 32;
				col = totalSpace / 32;
				foreach (RoomAttractivenessPanel.CreatureButton button in CategoryButtons)
				{
					button.Move(new Vector2(5f + col * 120f, 680f - 20f * row));
					row++;
					if (row > 31)
					{
						row = 0;
						col++;
					}
				}
				prevButton.Move(new Vector2(5f + col * 120f, 680f - 20f * row));
				row++;
				nextButton.Move(new Vector2(5f + col * 120f, 680f - 20f * row));
			}

			public class PageButton : Button
			{
				public RoomAttractivenessPanelManager Manager => (parentNode as RoomAttractivenessPanelManager)!;
				public int turn;

				public PageButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text, int turn) : base(owner, IDstring, parentNode, pos, width, text)
				{
					this.turn = turn;
				}

				public override void Update()
				{
					base.Update();
					colorA = MouseOver ? new Color(1f, 1f, 1f) : new Color(0f, 0f, 0f);
					if (owner != null && owner.mouseClick && MouseOver)
					{
						colorA = new Color(0.5f, 0f, 0f);
						colorB = new Color(1f, 1f, 1f);
					}
					colorB = new Color(1f, 0f, 0f);
				}

				public override void Clicked()
				{
					Manager.TurnPage(turn);
				}
			}
		}
	}
}
