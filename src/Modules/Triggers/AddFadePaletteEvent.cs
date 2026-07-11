using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DevInterface;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using RegionKit.Modules.Misc;

namespace RegionKit.Modules.Triggers
{
	public class AddFadePaletteEvent :  TriggeredEvent, ICustomEvent
	{
		public int palette;
		public float fadeAmount = 1f;
		public float fadeTime = 0;
		public bool untilPlayerLeaves = false;

		public AddFadePaletteEvent() : base(_Enums.AddFadePaletteEvent)
		{
			Implementation.ApplyHooks();
		}

		public override string ToString()
		{
			string text = base.ToString() + string.Format(CultureInfo.InvariantCulture,
				"<eA>{0}<eB>{1}<eA>{2}<eB>{3}<eA>{4}<eB>{5}<eA>{6}<eB>{7}",
				nameof(palette),
				palette,
				nameof(fadeAmount),
				fadeAmount,
				nameof(fadeTime),
				fadeTime,
				nameof(untilPlayerLeaves),
				untilPlayerLeaves ? "1" : "0"
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
					case nameof(palette):
						palette = int.Parse(array[1], CultureInfo.InvariantCulture);
						break;
					case nameof(fadeAmount):
						fadeAmount = float.Parse(array[1], CultureInfo.InvariantCulture);
						break;
					case nameof(fadeTime):
						fadeTime = float.Parse(array[1], CultureInfo.InvariantCulture);
						break;
					case nameof(untilPlayerLeaves):
						untilPlayerLeaves = array[1] == "1";
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
			room.AddObject(new FadePaletteApplier(room, palette, fadeAmount, fadeTime, untilPlayerLeaves));
			if (!untilPlayerLeaves)
			{
				Implementation.SaveFadeForLater(room, palette, fadeAmount);
			}
		}

		public StandardEventPanel? InitDevUIPanel(TriggerPanel triggerPanel)
		{
			return new AddFadePaletteEventPanel(triggerPanel.owner, triggerPanel, this);
		}

		private class FadePaletteApplier : CosmeticSprite
		{
			private readonly RoomSettings.FadePalette fadePalette;
			private readonly bool untilPlayerLeaves;
			private readonly float finalFadeAmount;
			private readonly float totalTime;
			private float currentTime = 0f;

			public FadePaletteApplier(Room room, int palette, float fadeAmount, float fadeTime, bool untilPlayerLeaves)
			{
				finalFadeAmount = fadeAmount;
				totalTime = fadeTime;
				this.room = room;
				this.untilPlayerLeaves = untilPlayerLeaves;

				fadePalette = new RoomSettings.FadePalette(palette, room.cameraPositions.Length);
				room.roomSettings.AddMoreFade(fadePalette);
				room.game.cameras[0].RefreshMoreFade();
			}

			public override void Update(bool eu)
			{
				base.Update(eu);

				currentTime += 1f / 40f;
				if (currentTime > totalTime)
				{
					for (int i = 0; i < fadePalette.fades.Length; i++)
					{
						fadePalette.fades[i] = finalFadeAmount;
					}
					room.game.cameras[0].ApplyFade();
					Destroy();
					return;
				}

				if (!room.game.Players.Any(x => x.Room == room.abstractRoom) && untilPlayerLeaves)
				{
					Destroy();
				}
			}

			public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
			{
				base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
				if (!slatedForDeletetion)
				{
					float fadeAmount = totalTime == 0 ? finalFadeAmount : (currentTime + timeStacker / 40f) / totalTime * finalFadeAmount;
					for (int i = 0; i < fadePalette.fades.Length; i++)
					{
						fadePalette.fades[i] = fadeAmount;
					}
					rCam.ApplyFade();
				}
			}
		}

		public static class Implementation
		{
			private static readonly ConditionalWeakTable<RainWorldGame, Dictionary<string, List<Fade>>> _fadeHandler = new();
			private static bool _appliedHooks = false;
			internal static void ApplyHooks()
			{
				if (_appliedHooks) return;
				_appliedHooks = true;
				On.Room.ctor += Room_ctor;
			}

			private static void Room_ctor(On.Room.orig_ctor orig, Room self, RainWorldGame game, World world, AbstractRoom abstractRoom, bool devUI)
			{
				orig(self, game, world, abstractRoom, devUI);
				if (self.game != null && _fadeHandler.TryGetValue(self.game, out Dictionary<string, List<Fade>> dict) && dict.TryGetValue(abstractRoom.name, out List<Fade> fades))
				{
					foreach (Fade fade in fades)
					{
						var fadePal = new RoomSettings.FadePalette(fade.palette, self.cameraPositions.Length);
						for (int i = 0; i < fadePal.fades.Length; i++)
						{
							fadePal.fades[i] = fade.fadeAmount;
						}
						self.roomSettings.AddMoreFade(fadePal);
					}
					self.game.cameras[0].RefreshMoreFade();
				}
			}

			public static void SaveFadeForLater(Room room, int palette, float fadeAmount)
			{
				if (!_fadeHandler.TryGetValue(room.game, out var dict))
				{
					_fadeHandler.Add(room.game, dict = []);
				}

				if (!dict.TryGetValue(room.abstractRoom.name, out var list))
				{
					dict.Add(room.abstractRoom.name, list = []);
				}

				list.Add(new Fade() { palette = palette, fadeAmount = fadeAmount });
			}

			private struct Fade
			{
				public int palette;
				public float fadeAmount;
			}
		}

		private class AddFadePaletteEventPanel : StandardEventPanel, IDevUISignals
		{
			private readonly AddFadePaletteEvent evnt;
			private bool hasInit = false;

			private Button playerLeaveButton;

			public AddFadePaletteEventPanel(DevUI owner, DevUINode parentNode, AddFadePaletteEvent evnt) : base(owner, parentNode, 110f)
			{
				this.evnt = evnt;

				GenericIntegerControl palettePicker = new GenericIntegerControl(owner, "Palette", this, new Vector2(5f, size.y - 45f), "Palette", evnt.palette) { minValue = 0 };
				palettePicker.OnValueChanged += PalettePicker_OnValueChanged;
				subNodes.Add(palettePicker);
				GenericSlider fadeAmountSlider = new GenericSlider(owner, "Amount", this, new Vector2(5f, size.y - 65f), "Fade Amount", false, 75f, evnt.fadeAmount, 0f, 1f, stringControl: true, stringWidth: 48)
				{
					displayRounding = 2
				};
				fadeAmountSlider.OnValueChanged += FadeAmountSlider_OnValueChanged;
				subNodes.Add(fadeAmountSlider);
				GenericSlider fadeTimeSlider = new GenericSlider(owner, "FadeTime", this, new Vector2(5f, size.y - 85f), "Fade Time", false, 75f, evnt.fadeTime, 0f, 60f, stringControl: true, stringWidth: 48)
				{
					displayRounding = 1,
				};
				fadeTimeSlider.OnValueChanged += FadeTimeSlider_OnValueChanged;
				subNodes.Add(fadeTimeSlider);

				subNodes.Add(playerLeaveButton = new Button(owner, "UntilPlayerLeaves", this, new Vector2(5f, size.y - 105f), size.x - 10f, ""));

				hasInit = true;
			}

			private void PalettePicker_OnValueChanged(int value, int oldValue) => evnt.palette = value;
			private void FadeAmountSlider_OnValueChanged(float value, float oldValue) => evnt.fadeAmount = value;
			private void FadeTimeSlider_OnValueChanged(float value, float oldValue) => evnt.fadeTime = value;

			public override void Refresh()
			{
				base.Refresh();
				if (!hasInit) return;

				playerLeaveButton.Text = evnt.untilPlayerLeaves ? "Until player leaves room" : "For entire cycle";
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (sender == playerLeaveButton)
				{
					evnt.untilPlayerLeaves = !evnt.untilPlayerLeaves;
				}
			}
		}
	}
}
