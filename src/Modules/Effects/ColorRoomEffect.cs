using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using DevInterface;
using System.Diagnostics.CodeAnalysis;

namespace RegionKit.Modules.Effects;

/// <summary>
/// Adds three sliders to room effects whose values range from 0 to 255.
/// By LB/M4rbleL1ne
/// </summary>
public static class ColorRoomEffect
{
	/// <summary>
	/// The list of effects that should have the three additional sliders.
	/// </summary>
	[AllowNull] public static HashSet<RoomSettings.RoomEffect.Type> colorEffectTypes = new();
	[AllowNull] private static ConditionalWeakTable<RoomSettings.RoomEffect, ColorSettings> __colorEffectSettings = new();

	internal static void Apply()
	{
		On.RoomSettings.RoomEffect.ctor += RoomEffectCtor;
		IL.RoomSettings.RoomEffect.FromString += RoomEffectFromString;
		IL.RoomSettings.RoomEffect.ToString += RoomEffectToString;
		On.DevInterface.EffectPanel.ctor += EffectPanelCtor;
	}

	internal static void Undo()
	{
		On.RoomSettings.RoomEffect.ctor -= RoomEffectCtor;
		IL.RoomSettings.RoomEffect.FromString -= RoomEffectFromString;
		IL.RoomSettings.RoomEffect.ToString -= RoomEffectToString;
		On.DevInterface.EffectPanel.ctor -= EffectPanelCtor;
	}

	internal static void Dispose()
	{
		colorEffectTypes.Clear();
		colorEffectTypes = null;
		__colorEffectSettings = null;
	}

	#region Hooks
	private static void RoomEffectCtor(On.RoomSettings.RoomEffect.orig_ctor orig, RoomSettings.RoomEffect self, RoomSettings.RoomEffect.Type type, float amount, bool inherited)
	{
		orig(self, type, amount, inherited);
		if (colorEffectTypes.Contains(type) && !__colorEffectSettings.TryGetValue(self, out _))
			__colorEffectSettings.Add(self, new());
	}

	private static void RoomEffectFromString(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(
			i => i.MatchLdarg(0),
			i => i.MatchLdarg(1),
			i => i.MatchLdcI4(4)))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate((RoomSettings.RoomEffect self, string[] s) =>
			{
				if (colorEffectTypes.Contains(self.type))
				{
					if (__colorEffectSettings.TryGetValue(self, out ColorSettings settings))
					{
						settings._colorParsed = false;
						for (var i = 0; i < s.Length; i++)
						{
							if (s[i] == "Color" && i + 3 < s.Length)
							{
								float.TryParse(s[i + 1], out settings._r);
								float.TryParse(s[i + 2], out settings._g);
								float.TryParse(s[i + 3], out settings._b);
								settings._colorParsed = true;
								break;
							}
						}
					}
					else
					{
						var newSettings = new ColorSettings();
						for (var i = 0; i < s.Length; i++)
						{
							if (s[i] == "Color" && i + 3 < s.Length)
							{
								float.TryParse(s[i + 1], out newSettings._r);
								float.TryParse(s[i + 2], out newSettings._g);
								float.TryParse(s[i + 3], out newSettings._b);
								newSettings._colorParsed = true;
								break;
							}
						}
						__colorEffectSettings.Add(self, newSettings);
					}
				}
			});
			c.Index += 3;
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((int unrecognizedIndex, RoomSettings.RoomEffect self) =>
			{
				if (colorEffectTypes.Contains(self.type) && __colorEffectSettings.TryGetValue(self, out ColorSettings settings) && settings._colorParsed)
					unrecognizedIndex += 4;
				return unrecognizedIndex;
			});
		}
		else
			__logger.LogError("Couldn't ILHook RoomSettings.RoomEffect.FromString!");
	}

	private static void RoomEffectToString(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After,
			i => i.MatchCall(typeof(string).GetMethod("Format", new[] { typeof(IFormatProvider), typeof(string), typeof(object[]) }))))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((string originalString, RoomSettings.RoomEffect self) =>
			{
				if (colorEffectTypes.Contains(self.type) && __colorEffectSettings.TryGetValue(self, out ColorSettings settings))
				{
					var sBuilder = new StringBuilder(originalString);
					sBuilder.Append("-Color-");
					sBuilder.Append(settings._r.ToString(CultureInfo.InvariantCulture));
					sBuilder.Append('-');
					sBuilder.Append(settings._g.ToString(CultureInfo.InvariantCulture));
					sBuilder.Append('-');
					sBuilder.Append(settings._b.ToString(CultureInfo.InvariantCulture));
					return sBuilder.ToString();
				}
				return originalString;
			});
		}
		else
			__logger.LogError("Couldn't ILHook RoomSettings.RoomEffect.ToString!");
	}

	private static void EffectPanelCtor(On.DevInterface.EffectPanel.orig_ctor orig, EffectPanel self, DevUI owner, DevUINode parentNode, Vector2 pos, RoomSettings.RoomEffect effect)
	{
		orig(self, owner, parentNode, pos, effect);
		if (colorEffectTypes.Contains(effect.type) && __colorEffectSettings.TryGetValue(effect, out ColorSettings settings) && self.subNodes.FirstOrDefault(s => s.IDstring == "Amount_Slider") is EffectPanel.EffectPanelSlider effectSlider)
		{
			self.size.y += 60f;
			effectSlider.pos.y += 60f;
			float posX = effectSlider.pos.x, posY = effectSlider.pos.y;
			self.subNodes.Add(new EffectColorSlider(owner, "ColorR_Slider", self, new(posX, posY - 20f), "Red: "));
			self.subNodes.Add(new EffectColorSlider(owner, "ColorG_Slider", self, new(posX, posY - 40f), "Green: "));
			self.subNodes.Add(new EffectColorSlider(owner, "ColorB_Slider", self, new(posX, posY - 60f), "Blue: "));
		}
	}
	#endregion

	#region Extensions
	/// <summary>
	/// Gets the amount of red of the specified effect.
	/// </summary>
	/// <returns>The amount of red of the specified effect.</returns>
	public static float GetRedAmount(this RoomSettings self, RoomSettings.RoomEffect.Type type)
	{
		for (var i = 0; i < self.effects.Count; i++)
		{
			RoomSettings.RoomEffect effect = self.effects[i];
			if (effect.type == type && colorEffectTypes.Contains(effect.type) && __colorEffectSettings.TryGetValue(effect, out ColorSettings settings))
				return settings._r;
		}
		return 0f;
	}

	/// <summary>
	/// Gets the amount of green of the specified effect.
	/// </summary>
	/// <returns>The amount of green of the specified effect.</returns>
	public static float GetGreenAmount(this RoomSettings self, RoomSettings.RoomEffect.Type type)
	{
		for (var i = 0; i < self.effects.Count; i++)
		{
			RoomSettings.RoomEffect effect = self.effects[i];
			if (effect.type == type && colorEffectTypes.Contains(effect.type) && __colorEffectSettings.TryGetValue(effect, out ColorSettings settings))
				return settings._g;
		}
		return 0f;
	}

	/// <summary>
	/// Gets the amount of blue of the specified effect.
	/// </summary>
	/// <returns>The amount of blue of the specified effect.</returns>
	public static float GetBlueAmount(this RoomSettings self, RoomSettings.RoomEffect.Type type)
	{
		for (var i = 0; i < self.effects.Count; i++)
		{
			RoomSettings.RoomEffect effect = self.effects[i];
			if (effect.type == type && colorEffectTypes.Contains(effect.type) && __colorEffectSettings.TryGetValue(effect, out ColorSettings settings))
				return settings._b;
		}
		return 0f;
	}

	/// <summary>
	/// Gets the color made by the three color sliders of the specified effect.
	/// </summary>
	/// <returns>The color made by the three color sliders of the specified effect.</returns>
	public static Color GetColor(this RoomSettings self, RoomSettings.RoomEffect.Type type)
	{
		for (var i = 0; i < self.effects.Count; i++)
		{
			RoomSettings.RoomEffect effect = self.effects[i];
			if (effect.type == type && colorEffectTypes.Contains(effect.type) && __colorEffectSettings.TryGetValue(effect, out ColorSettings settings))
				return settings.Color;
		}
		return Color.black;
	}

	/// <summary>
	/// Checks if the specified effect is in the room.
	/// </summary>
	/// <returns>True if the specified effect is in the room, false otherwise.</returns>
	public static bool IsEffectInRoom(this RoomSettings self, RoomSettings.RoomEffect.Type type) => self.GetEffect(type) != null;
	#endregion

	private class ColorSettings
	{
		internal bool _colorParsed;
		internal float _r;
		internal float _g;
		internal float _b;

		internal Color Color => new(_r, _g, _b);
	}

	private class EffectColorSlider : Slider
	{
		private RoomSettings.RoomEffect Effect => ((EffectPanel)parentNode).effect;

		internal EffectColorSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title, false, 110f) { }

		public override void Refresh()
		{
			base.Refresh();
			var num = 0f;
			if (Effect is RoomSettings.RoomEffect effect && colorEffectTypes.Contains(effect.type) && __colorEffectSettings.TryGetValue(effect, out ColorSettings settings))
			{
				switch (IDstring)
				{
				case "ColorR_Slider":
					num = settings._r;
					break;
				case "ColorG_Slider":
					num = settings._g;
					break;
				case "ColorB_Slider":
					num = settings._b;
					break;
				}
			}
			NumberText = ((int)(num * 255f)).ToString();
			RefreshNubPos(num);
		}

		public override void NubDragged(float nubPos)
		{
			if (Effect is RoomSettings.RoomEffect effect && !effect.inherited && colorEffectTypes.Contains(effect.type) && __colorEffectSettings.TryGetValue(effect, out ColorSettings settings))
			{
				switch (IDstring)
				{
				case "ColorR_Slider":
					settings._r = nubPos;
					break;
				case "ColorG_Slider":
					settings._g = nubPos;
					break;
				case "ColorB_Slider":
					settings._b = nubPos;
					break;
				}
			}
			Refresh();
		}
	}
}
