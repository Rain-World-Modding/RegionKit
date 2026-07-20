using System.Globalization;
using System.Text.RegularExpressions;
using DevInterface;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.Triggers
{
	public class PlaySoundEvent : TriggeredEvent, ICustomEvent
	{
		public SoundID sound;
		public float pan = 0f;
		public float volume = 1f;
		public float pitchMin = 1f;
		public float pitchMax = 1f;

		public PlaySoundEvent() : base(_Enums.PlaySoundEvent)
		{
			sound = SoundID.None;
		}

		public override string ToString()
		{
			string text = base.ToString() + string.Format(CultureInfo.InvariantCulture,
				"<eA>{0}<eB>{1}<eA>{2}<eB>{3}<eA>{4}<eB>{5}<eA>{6}<eB>{7}<eA>{8}<eB>{9}",
				nameof(sound),
				sound.value,
				nameof(pan),
				pan,
				nameof(volume),
				volume,
				nameof(pitchMin),
				pitchMin,
				nameof(pitchMax),
				pitchMax
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
					case nameof(sound):
						sound = new SoundID(array[1], false);
						break;
					case nameof(pan):
						pan = float.Parse(array[1], CultureInfo.InvariantCulture);
						break;
					case nameof(volume):
						volume = float.Parse(array[1], CultureInfo.InvariantCulture);
						break;
					case nameof(pitchMin):
						pitchMin = float.Parse(array[1], CultureInfo.InvariantCulture);
						break;
					case nameof(pitchMax):
						pitchMax = float.Parse(array[1], CultureInfo.InvariantCulture);
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
			LogInfo("Played sound");
			room.PlaySound(sound, pan, volume, UnityEngine.Random.Range(pitchMin, pitchMax));
		}

		public StandardEventPanel? InitDevUIPanel(TriggerPanel triggerPanel)
		{
			return new PlaySoundEventPanel(triggerPanel.owner, triggerPanel, this);
		}

		private class PlaySoundEventPanel : StandardEventPanel
		{
			private readonly PlaySoundEvent evnt;

			public PlaySoundEventPanel(DevUI owner, DevUINode parentNode, PlaySoundEvent evnt) : base(owner, parentNode, 130f)
			{
				this.evnt = evnt;

				subNodes.Add(new PickSoundButton(owner, "Picker", this, new Vector2(5f, size.y - 45f), size.x - 10f, evnt));

				GenericSlider pan, volume, pitchMin, pitchMax;
				pan = new GenericSlider(owner, "Pan", this, new Vector2(5f, size.y - 65f), "Pan", false, 60f, evnt.pan, -1f, 1f, stringControl: false, stringWidth: 48) { displayRounding = 3 };
				volume = new GenericSlider(owner, "Vol", this, new Vector2(5f, size.y - 85f), "Volume", false, 60f, evnt.volume, 0f, 2f, stringControl: false, stringWidth: 48) { displayRounding = 3 };
				pitchMin = new GenericSlider(owner, "PitchMin", this, new Vector2(5f, size.y - 105f), "Pitch Min", false, 60f, evnt.pitchMin, 0f, 2f, stringControl: false, stringWidth: 48) { displayRounding = 3 };
				pitchMax = new GenericSlider(owner, "PitchMax", this, new Vector2(5f, size.y - 125f), "Pitch Max", false, 60f, evnt.pitchMax, 0f, 2f, stringControl: false, stringWidth: 48) { displayRounding = 3 };
				pan.OnValueChanged += Pan_OnValueChanged;
				volume.OnValueChanged += Volume_OnValueChanged;
				pitchMin.OnValueChanged += PitchMin_OnValueChanged;
				pitchMax.OnValueChanged += PitchMax_OnValueChanged;
				subNodes.Add(pan);
				subNodes.Add(volume);
				subNodes.Add(pitchMin);
				subNodes.Add(pitchMax);
			}

			private void Pan_OnValueChanged(float value, float oldValue) => evnt.pan = value;
			private void Volume_OnValueChanged(float value, float oldValue) => evnt.volume = value;
			private void PitchMin_OnValueChanged(float value, float oldValue) => evnt.pitchMin = value;
			private void PitchMax_OnValueChanged(float value, float oldValue) => evnt.pitchMax = value;

		}

		private class PickSoundButton : ButtonWithSelectPanel
		{
			private readonly PlaySoundEvent evnt;
			public PickSoundButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, PlaySoundEvent evnt) : base(owner, IDstring, parentNode, pos, width, "Sound: " + evnt.sound, SelectPanelMaker)
			{
				this.evnt = evnt;
			}

			public override void OnValueChange(string value)
			{
				SoundID type = new SoundID(value, false);
				evnt.sound = type;
				Text = $"Sound: {type}";
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
				return new SearchableSelectPanel(maker.owner, "PlaySoundEventSelectPanel", maker, new Vector2(250f, 15f) - maker.absPos, "Select Sound ID", [.. SoundID.values.entries.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)], (maker as PickSoundButton)?.evnt.sound.value, 185f);
			}
		}
	}
}
