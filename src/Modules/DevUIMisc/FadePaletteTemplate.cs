using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using static RoomSettings;

namespace RegionKit.Modules.DevUIMisc;

internal static class FadePaletteTemplate
{
	static ConditionalWeakTable<RoomSettings, List<bool>> _template = new();

	/// <summary>
	/// use -1 for the fade palette, 0+ for fade screen
	/// </summary>
	public static bool IsFadeTemplate(this RoomSettings p, int fade)
	{
		fade++;
		List<bool> list = _template.GetValue(p, _ => new());
		return list.Count > fade && list[fade];
	}

	/// <summary>
	/// use -1 for the fade palette, 0+ for fade screen
	/// </summary>
	public static void SetFadeTemplate(this RoomSettings p, int fade, bool value)
	{
		fade++;
		List<bool> list = _template.GetValue(p, _ => new());

		for (int i = list.Count; i <= fade; i++)
		{ list.Add(false); }

		list[fade] = value;
	}

	public static void Apply()
	{
		_CommonHooks.PreRoomLoad += _CommonHooks_PreRoomLoad;
		On.RoomSettings.FindParent += RoomSettings_FindParent;
		_CommonHooks.RoomSettingsSave += _CommonHooks_RoomSettingsSave;
		On.RoomSettings.Save_string_bool += RoomSettings_Save_string_bool;
		On.DevInterface.PaletteController.Increment += PaletteController_Increment;
		On.DevInterface.PaletteFadeSlider.NubDragged += PaletteFadeSlider_NubDragged;
		On.DevInterface.PaletteController.Refresh += PaletteController_Refresh;
	}

	public static void Undo()
	{
		_CommonHooks.PreRoomLoad -= _CommonHooks_PreRoomLoad;
		On.RoomSettings.FindParent -= RoomSettings_FindParent;
		_CommonHooks.RoomSettingsSave -= _CommonHooks_RoomSettingsSave;
		On.RoomSettings.Save_string_bool -= RoomSettings_Save_string_bool;
		On.DevInterface.PaletteController.Increment -= PaletteController_Increment;
		On.DevInterface.PaletteFadeSlider.NubDragged -= PaletteFadeSlider_NubDragged;
		On.DevInterface.PaletteController.Refresh -= PaletteController_Refresh;
	}

	private static void PaletteController_Refresh(On.DevInterface.PaletteController.orig_Refresh orig, DevInterface.PaletteController self)
	{
		if (self.controlPoint == 3 && self.RoomSettings.IsFadeTemplate(-1) && self.RoomSettings.parent.fadePalette != null)
		{
			self.NumberLabelText = "<T>" + self.RoomSettings.fadePalette.palette.ToString();
		}

		else { orig(self); }
	}

	private static void PaletteFadeSlider_NubDragged(On.DevInterface.PaletteFadeSlider.orig_NubDragged orig, DevInterface.PaletteFadeSlider self, float nubPos)
	{
		self.RoomSettings.SetFadeTemplate(self.index, false);
		orig(self, nubPos);
	}

	private static void PaletteController_Increment(On.DevInterface.PaletteController.orig_Increment orig, DevInterface.PaletteController self, int change)
	{
		if (self.controlPoint == 3 && self.RoomSettings.parent.fadePalette != null)
		{
			if (self.RoomSettings.fadePalette == null)
			{
				self.RoomSettings.fadePalette = new FadePalette(-1, self.owner.room.cameraPositions.Length);
				InheritTemplateFades(self.RoomSettings);
				change = 0;
			}
			else if (self.RoomSettings.fadePalette.palette == 0 && change < 0)
			{
				self.RoomSettings.fadePalette.palette = -1;
				InheritTemplateFades(self.RoomSettings);
				change = 0;
			}
			else if (self.RoomSettings.IsFadeTemplate(-1))
			{
				self.RoomSettings.fadePalette.palette = 0;
				self.RoomSettings.SetFadeTemplate(-1, false);
				change = 0;
			}
			else { self.RoomSettings.SetFadeTemplate(-1, false); }

			orig(self, change);
		}

		else
		{
			self.RoomSettings.SetFadeTemplate(-1, false);
			orig(self, change);
		}
	}

	private static void _CommonHooks_PreRoomLoad(Room obj)
	{
		RoomSettings settings = obj.roomSettings;
		int cams = obj.cameraPositions.Length;

		if (settings.fadePalette == null)
		{
			if (settings.parent.fadePalette != null && settings.parent.fadePalette.fades.Length > 0)
			{
				settings.fadePalette = new FadePalette(-1, cams);

				for (int i = 0; i < cams; i++)
				{ settings.fadePalette.fades[i] = -1f; }
			}
		}

		else if (settings.fadePalette.fades.Length < cams)
		{
			List<float> newFades = settings.fadePalette.fades.ToList();

			for (int i = settings.fadePalette.fades.Length; i < cams; i++)
			{ newFades.Add(-1f); }

			settings.fadePalette.fades = newFades.ToArray();
		}

		else if (settings.fadePalette.fades.Length > cams)
		{
			Array.Resize(ref settings.fadePalette.fades, cams);
		}

		InheritTemplateFades(settings);
	}

	private static void RoomSettings_Save_string_bool(On.RoomSettings.orig_Save_string_bool orig, RoomSettings self, string path, bool saveAsTemplate)
	{
		FadePalette origPalette = self.fadePalette;
		FadePalette tempPalette = new(self.fadePalette.palette, self.fadePalette.fades.Length)
		{ fades = origPalette.fades };

		bool template = false;
		int c = 0;

		if (self.IsFadeTemplate(-1))
		{ tempPalette.palette = -1; template = true; c++; }

		for (int i = 0; i < tempPalette.fades.Length; i++)
		{
			if (self.IsFadeTemplate(i)) tempPalette.fades[i] = -1;
			template = true;
			c++;
		}

		if (template) { self.fadePalette = tempPalette; }
		if (c == tempPalette.fades.Length + 1) { self.fadePalette = null; } //everything is template, don't bother writing

		orig(self, path, saveAsTemplate);

		if (template) { self.fadePalette = origPalette; }
	}

	private static List<string> _CommonHooks_RoomSettingsSave(RoomSettings self, bool saveAsTemplate)
	{
		List<string> result = new();

		if (self.fadePalette != null && self.fadePalette.fades.Length > 0 && saveAsTemplate)
		{
			result.Add($"FadePalette: {self.fadePalette.palette}, {self.fadePalette.fades[0]}");
		}
		return result;
	}

	private static void RoomSettings_FindParent(On.RoomSettings.orig_FindParent orig, RoomSettings self, Region region)
	{
		orig(self, region);
	}

	public static void InheritTemplateFades(RoomSettings self)
	{
		if (self.fadePalette == null) return;

		if (self.parent.fadePalette == null || self.fadePalette.palette != -1)
		{
			FillMissingFades(self);
			return;
		}

		self.fadePalette.palette = self.parent.fadePalette.palette;
		self.SetFadeTemplate(-1, true);

		int num = 0;
		for (int i = 0; i < self.fadePalette.fades.Length; i++)
		{

			if (i < self.parent.fadePalette.fades.Length)
			{ num = i; }

			if (self.fadePalette.fades[i] == -1f)
			{
				self.fadePalette.fades[i] = self.parent.fadePalette.fades[num];
				self.SetFadeTemplate(i, true);
			}
		}
	}

	public static void FillMissingFades(RoomSettings self)
	{
		float amount = 0f;
		for (int i = 0; i < self.fadePalette.fades.Length; i++)
		{
			if (self.fadePalette.fades[i] == -1f)
			{ self.fadePalette.fades[i] = amount; }

			else { amount = self.fadePalette.fades[i]; }
		}
	}
}
