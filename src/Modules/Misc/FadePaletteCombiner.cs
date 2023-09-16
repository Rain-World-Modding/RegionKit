using System.IO;
using DevInterface;

namespace RegionKit.Modules.Misc;

internal static class FadePaletteCombiner
{
	public static void Enable()
	{
		On.DevInterface.RoomSettingsPage.ctor += RoomSettingsPage_ctor;
	}

	public static void Disable()
	{
		On.DevInterface.RoomSettingsPage.ctor -= RoomSettingsPage_ctor;
	}

	private static void RoomSettingsPage_ctor(On.DevInterface.RoomSettingsPage.orig_ctor orig, RoomSettingsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
	{
		orig(self, owner, IDstring, parentNode, name);

		Panel panel = new FadePaletteCombinerPanel(owner, "Fade_Palette_Combiner_Panel", self, new Vector2(260f, 190f), new Vector2(230f, 85f), "Fade Palette Combiner");
		self.subNodes.Add(panel);
	}

	public class FadePaletteCombinerPanel : Panel, IDevUISignals
	{
		public FadePaletteCombinerPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, string title) : base(owner, IDstring, parentNode, pos, size, title)
		{
			subNodes.Add(new PaletteController(owner, "New_Palette", this, new Vector2(5, size.y - 20f), "New palette number:"));
			subNodes.Add(new Button(owner, "Save_New_Palette", this, new Vector2(5, size.y - 60f), 220f, "Save fade as new palette"));
			subNodes.Add(new DevUILabel(owner, "Label", this, new Vector2(5, size.y - 80f), 220f, "Output: palettes/combinedPalettes"));
		}

		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			if(sender.IDstring == "Save_New_Palette")
			{
				string path = Path.Combine(Custom.RootFolderDirectory(), "palettes", "combinedPalettes");
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
				
				Texture2D newPaletteTexture = new Texture2D(32, 16);
				
				RoomCamera roomCamera = owner.room.game.cameras[0];

				for (int i = 0; i < 32; i++)
				{
					for (int j = 0; j < 16; j++)
					{
						newPaletteTexture.SetPixel(i, j, Color.Lerp(roomCamera.fadeTexA.GetPixel(i, j), roomCamera.fadeTexB.GetPixel(i, j), roomCamera.fadeCoord.x));
					}
				}

				// Code borrowed from MoreFadePalettes
				foreach (RoomSettings.FadePalette fade in roomCamera.MoreFadeTextures().Keys)
				{
					Texture2D fadeTex = roomCamera.GetMoreFadeTexture(fade);
					if (fadeTex == null) continue;

					for (int i = 0; i < 32; i++)
					{
						for (int j = 0; j < 16; j++)
						{
							Color origColor = newPaletteTexture.GetPixel(i, j);
							if (fade.fades.Length > roomCamera.currentCameraPosition) //we're not throwing, even if it'll fail to render the fade
							{
								newPaletteTexture.SetPixel(i, j, Color.Lerp(origColor, fadeTex.GetPixel(i, j), fade.fades[roomCamera.currentCameraPosition]));
							}
						}
					}
				}

				// Don't save with the effect colors on the palette
				newPaletteTexture.SetPixels(30, 2, 2, 4, new Color[8] { Color.white, Color.white, Color.white, Color.white, Color.white, Color.white, Color.white, Color.white });
				newPaletteTexture.SetPixels(30, 10, 2, 4, new Color[8] { Color.white, Color.white, Color.white, Color.white, Color.white, Color.white, Color.white, Color.white });

				// Save the new palette
				PNGSaver.SaveTextureToFile(newPaletteTexture, Path.Combine(path, $"palette{(subNodes[0] as PaletteController).newPaletteNumber}.png"));
			}
		}
		private class PaletteController : IntegerControl
		{
			public int newPaletteNumber = 0;

			public PaletteController(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title)
			{
				for (int i = 0; i < subNodes.Count; i++)
				{
					subNodes[i].ClearSprites();
				}
				subNodes.Clear();
				
				// Wanted the field to be a little bit wider
				subNodes.Add(new DevUILabel(owner, "Title", this, new Vector2(0f, 0f), 130f, title));
				subNodes.Add(new DevUILabel(owner, "Number", this, new Vector2(160f, 0f), 36f, "0"));
				subNodes.Add(new ArrowButton(owner, "Less", this, new Vector2(140f, 0f), -90f));
				subNodes.Add(new ArrowButton(owner, "More", this, new Vector2(200f, 0f), 90f));
			}

			public override void Increment(int change)
			{
				newPaletteNumber += change;
				Refresh();
			}

			public override void Refresh()
			{
				base.Refresh();
				NumberLabelText = newPaletteNumber.ToString();
			}
		}
	}
}
