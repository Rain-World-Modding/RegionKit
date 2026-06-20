using System.Runtime.CompilerServices;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc
{
	internal static class PagedFadePalettes
	{
		private static readonly ConditionalWeakTable<RoomSettingsPage, CWTData> dataCWT = new();
		public const string PrevButtonMessage = "RK_MainFadePalette_Prev";
		public const string NextButtonMessage = "RK_MainFadePalette_Next";
		public static readonly int SlidersPerPage = 20;

		public static bool Enabled => ModOptions.PagedFadePalettes.Value;

		internal static void Apply()
		{
			On.DevInterface.RoomSettingsPage.ctor += RoomSettingsPage_ctor;
			On.DevInterface.RoomSettingsPage.Signal += RoomSettingsPage_Signal;
		}

		internal static void Undo()
		{
			On.DevInterface.RoomSettingsPage.ctor -= RoomSettingsPage_ctor;
			On.DevInterface.RoomSettingsPage.Signal -= RoomSettingsPage_Signal;
		}

		private static void RoomSettingsPage_ctor(On.DevInterface.RoomSettingsPage.orig_ctor orig, RoomSettingsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
		{
			orig(self, owner, IDstring, parentNode, name);
			
			int camCount = owner.room.cameraPositions.Length;
			if (Enabled && camCount > SlidersPerPage)
			{
				if (self.subNodes.FirstOrDefault(x => x is Panel && x.IDstring == "Palette_Panel") is Panel panel)
				{
					// Find the fade sliders
					var data = new CWTData
					{
						panel = panel,
						fadeSliders = new PaletteFadeSlider[camCount],
						cameraCount = camCount,
					};
					foreach (DevUINode node in panel.subNodes)
					{
						if (node is PaletteFadeSlider fadeSlider)
						{
							data.fadeSliders[fadeSlider.index] = fadeSlider;
						}
					}

					// Resize and reposition other nodes
					float oldSizeY = panel.size.y;
					panel.size = new Vector2(210f, 115f + 20f * SlidersPerPage);
					float yDiff = panel.size.y - oldSizeY;
					foreach (DevUINode node in panel.subNodes)
					{
						if (node is PositionedDevUINode positionedNode)
						{
							positionedNode.pos.y += yDiff;
						}
					}
					for (int i = SlidersPerPage; i < camCount; i++)
					{
						// place it way the fuck in the middle of nowhere so it won't bother us :3
						data.fadeSliders[i].pos = new Vector2(-99999f, -99999f);
					}

					// Add new buttons
					float buttonWidth = (panel.size.x - 15f) / 2f;
					panel.subNodes.Add(data.prevButton = new Button(owner, PrevButtonMessage, panel, new Vector2(5f, 5f), buttonWidth, "Previous Page"));
					panel.subNodes.Add(data.nextButton = new Button(owner, NextButtonMessage, panel, new Vector2(10f + buttonWidth, 5f), buttonWidth, "Next Page"));

					// Add to CWT
					dataCWT.Add(self, data);
				}
			}
		}

		private static void RoomSettingsPage_Signal(On.DevInterface.RoomSettingsPage.orig_Signal orig, RoomSettingsPage self, DevUISignalType type, DevUINode sender, string message)
		{
			LogDebug($"PagedFadePalettes -> type: {type} | id: {sender.IDstring} | message: {message}");
			if (type == DevUISignalType.ButtonClick && Enabled && dataCWT.TryGetValue(self, out CWTData data))
			{
				bool switchPage = false;
				if (sender.IDstring == PrevButtonMessage)
				{
					if (data.currentPage > 0)
					{
						data.currentPage--;
						switchPage = true;
					}
				}
				else if (sender.IDstring == NextButtonMessage)
				{
					if (data.currentPage < data.TotalPages - 1)
					{
						data.currentPage++;
						switchPage = true;
					}
				}

				if (switchPage)
				{
					foreach (PaletteFadeSlider slider in data.fadeSliders)
					{
						slider.pos = new Vector2(-99999f, -99999f);
					}

					for (int i = 0; i < SlidersPerPage; i++)
					{
						int j = data.currentPage * SlidersPerPage + i;
						if (j >= data.fadeSliders.Length) break;
						data.fadeSliders[j].pos = new Vector2(5f, data.panel.size.y - 110f - i * 20f);
					}

					data.panel.Refresh();
					return;
				}
			}
			orig(self, type, sender, message);
		}

		private class CWTData
		{
			public Panel panel = null!;
			public PaletteFadeSlider[] fadeSliders = [];
			public Button nextButton = null!;
			public Button prevButton = null!;
			public int cameraCount = 0;
			public int currentPage = 0;

			public int TotalPages => (cameraCount + SlidersPerPage - 1) / SlidersPerPage;
		}
	}
}
