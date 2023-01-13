using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Menu;
using DevInterface;

namespace RegionKit.Modules.Effects;

public static class ConditionalEffects
{
	public static void Enable()
	{
		On.RoomSettings.RoomEffect.ToString += RoomEffect_ToString;
		On.RoomSettings.RoomEffect.FromString += RoomEffect_FromString;
		On.RainWorld.Update += RainWorld_Update;
		On.DevInterface.EffectPanel.ctor += EffectPanel_ctor;
		On.DevInterface.EffectPanel.EffectPanelSlider.NubDragged += EffectPanelSlider_NubDragged;
	}

	public static void Disable()
	{
		On.RoomSettings.RoomEffect.ToString -= RoomEffect_ToString;
		On.RoomSettings.RoomEffect.FromString -= RoomEffect_FromString;
		On.RainWorld.Update -= RainWorld_Update;
		On.DevInterface.EffectPanel.ctor -= EffectPanel_ctor;
		On.DevInterface.EffectPanel.EffectPanelSlider.NubDragged -= EffectPanelSlider_NubDragged;
	}

	#region hooks
	public static Dictionary<WeakReference, bool[]> filterFlags = new Dictionary<WeakReference, bool[]>();
	public static Dictionary<WeakReference, float> baseIntensities = new Dictionary<WeakReference, float>();

	private static void EffectPanelSlider_NubDragged(On.DevInterface.EffectPanel.EffectPanelSlider.orig_NubDragged orig, EffectPanel.EffectPanelSlider self, float nubPos)
	{
		if (TryGetWeak(filterFlags, self.effect, out bool[] flags))
		{
			if ((self.owner.game.StoryCharacter < flags.Length) && (self.owner.game.StoryCharacter >= 0))
				if (!flags[self.owner.game.StoryCharacter])
					return;
		}
		orig.Invoke(self, nubPos);
	}

	private static int scanFiltersIndex = 0;
	private static int scanIntensitiesIndex = 0;
	private static void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
	{
		orig.Invoke(self);
		if (filterFlags.Count > 0)
		{
			if (++scanFiltersIndex >= filterFlags.Count) scanFiltersIndex = 0;
			WeakReference key = filterFlags.ElementAt(scanFiltersIndex).Key;
			if (!key.IsAlive)
				filterFlags.Remove(key);
		}
		if (baseIntensities.Count > 0)
		{
			if (++scanIntensitiesIndex >= baseIntensities.Count) scanIntensitiesIndex = 0;
			WeakReference key = baseIntensities.ElementAt(scanIntensitiesIndex).Key;
			if (!key.IsAlive)
				baseIntensities.Remove(key);
		}
	}


	private static void RoomEffect_FromString(On.RoomSettings.RoomEffect.orig_FromString orig, RoomSettings.RoomEffect self, string[] s)
	{
		orig.Invoke(self, s);
		try
		{
			if (s.Length > 4 && s[4] is not "Color")
			{
				bool[] flags = new bool[3] { false, false, false };
				SetWeak(filterFlags, self, flags);
				int bitMask = int.Parse(s[4]);
				for (int i = 0; i < flags.Length; i++)
				{
					if ((bitMask & (1 << i)) > 0)
						flags[i] = true;
				}
			}
		}
		catch
		{
			PetrifiedWood.WriteLine("Wrong syntax effect loaded for filter: " + s[0]);
		}

		RainWorld rw = UnityEngine.Object.FindObjectOfType<RainWorld>();
		if (rw.progression.currentSaveState == null)
		{ return; }
		if (TryGetWeak(filterFlags, self, out bool[] testFlags))
		{
			if (!testFlags[rw.progression.currentSaveState.saveStateNumber])
			{
				SetWeak(baseIntensities, self, self.amount);
				self.amount = 0;
			}
		}
	}

	private static string RoomEffect_ToString(On.RoomSettings.RoomEffect.orig_ToString orig, RoomSettings.RoomEffect self)
	{
		float oldAmount = -1f;
		if (TryGetWeak(baseIntensities, self, out float savedAmount))
		{
			oldAmount = self.amount;
			self.amount = savedAmount;
		}
		string ret = orig.Invoke(self);
		if (TryGetWeak(filterFlags, self, out bool[] flags))
		{
			int bitMask = 0;
			bool allTrue = true;
			for (int i = 0; i < flags.Length; i++)
				if (!flags[i])
					allTrue = false;
				else
					bitMask |= 1 << i;
			if (!allTrue)
				ret += "-" + bitMask;
		}
		if (oldAmount != -1f)
			self.amount = oldAmount;
		return ret;
	}

	private static void EffectPanel_ctor(On.DevInterface.EffectPanel.orig_ctor orig, EffectPanel self, DevUI owner, DevUINode parentNode, Vector2 pos, RoomSettings.RoomEffect effect)
	{
		orig.Invoke(self, owner, parentNode, pos, effect);
		if (!TryGetWeak(filterFlags, effect, out _))
			SetWeak(filterFlags, effect, new bool[] { true, true, true });
		self.subNodes.Add(new EffectPanelFilters(owner, "Filter_Toggles", self, new Vector2(5f, 0f + 20f)));
		self.size.y += 8f;
	}

	public class EffectPanelFilters : PositionedDevUINode
	{
		public EffectPanelFilters(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
		{
			for (int i = 0; i < 3; i++)
			{
				FSprite sprite = new FSprite("Circle20", true)
				{
					color = PlayerGraphics.SlugcatColor(i),
					anchorX = 0f,
					anchorY = 0.5f,
					scaleX = 6f / 10f,
					scaleY = 6f / 10f
				};
				fSprites.Add(sprite);
				Futile.stage.AddChild(sprite);
			}
		}

		public bool lastMouseDown = false;
		public override void Update()
		{
			base.Update();
			if (!lastMouseDown && Input.GetMouseButton(0))
			{
				RoomSettings.RoomEffect effect = (parentNode as EffectPanel).effect;
				if (!TryGetWeak(filterFlags, effect, out bool[] filters))
				{
					filters = new bool[] { true, true, true };
					SetWeak(filterFlags, effect, filters);
				}
				for (int i = 0; i < 3; i++)
				{
					Vector2 lPos = owner.mousePos - fSprites[i].GetPosition();
					if (lPos.x > 0f && lPos.x < 16f && lPos.y < 8f && lPos.y > -8f)
					{
						filters[i] = !filters[i];
						if (i == owner.game.StoryCharacter)
						{
							if (filters[i])
							{
								effect.amount = GetWeak(baseIntensities, effect);
								RemoveWeak(baseIntensities, effect);
							}
							else
							{
								SetWeak(baseIntensities, effect, effect.amount);
								effect.amount = 0f;
							}
						}
						parentNode.Refresh();
					}
				}
			}
			lastMouseDown = Input.GetMouseButton(0);
		}

		public override void Refresh()
		{
			base.Refresh();
			TryGetWeak(filterFlags, (parentNode as EffectPanel).effect, out bool[] filters);
			for (int i = 0; i < 3; i++)
			{
				fSprites[i].color = ((filters == null) || filters[i]) ? PlayerGraphics.SlugcatColor(i) : Color.Lerp(PlayerGraphics.SlugcatColor(i), Color.black, 0.5f);
				MoveSprite(i, absPos + new Vector2(5f + i * 21f, 10f));
			}
		}
	}

	#endregion hooks

}
