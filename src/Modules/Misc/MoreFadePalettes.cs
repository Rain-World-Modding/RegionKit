using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;
using FadePalette = RoomSettings.FadePalette;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using DevInterface;

namespace RegionKit.Modules.Misc;

internal static class MoreFadePalettes
{
	#region FadeExtensions
	private static ConditionalWeakTable<RoomSettings, List<FadePalette>> _AllFadePalettes = new();

	private static List<FadePalette> AllFadePalettes(this RoomSettings rs) => _AllFadePalettes.GetValue(rs, _ => new());

	public static FadePalette[] GetAllFades(this RoomSettings rs)
	{
		List<FadePalette> fades = new();
		for (int i = 0; ; i++)
		{
			if (rs.GetMoreFade(i) == null) break;
			fades.Add(rs.GetMoreFade(i));
		}
		return fades.ToArray();
	}

	public static FadePalette GetMoreFade(this RoomSettings rs, int index)
	{
		if (index < 0) throw new IndexOutOfRangeException("Palette infex below zero");

		if (rs.AllFadePalettes().Count > index && rs.AllFadePalettes()[index] != null)
		{ return rs.AllFadePalettes()[index]; }

		if (rs.parent.AllFadePalettes().Count > 0 && rs.parent.AllFadePalettes()[0] != null)
		{ return rs.parent.AllFadePalettes()[0]; }

		return null!;
	}

	public static void SetMoreFade(this RoomSettings rs, int index, FadePalette palette)
	{
		if (index < 0) throw new IndexOutOfRangeException("Palette infex below zero");
		if (rs.AllFadePalettes().Count > index)
		{
			rs.AllFadePalettes()[index] = palette;
		}
		else
		{
			rs.AllFadePalettes().Add(palette);
		}
	}

	public static void DeleteMoreFade(this RoomSettings rs, int index)
	{
		if (index < 0) throw new IndexOutOfRangeException("Palette infex below zero");
		if (rs.AllFadePalettes().Count > index && index >= 0)
		{
			rs.AllFadePalettes().RemoveAt(index);
		}
		LogMessage($"removing at index {index}, count is now {rs.AllFadePalettes().Count()}");
	}

	public static FadePalette GetParentFade(this RoomSettings rs, Room room)
	{
		//for later...
		if (rs.parent.fadePalette == null) return null!;
		var palette = new FadePalette(rs.parent.fadePalette.palette, room.cameraPositions.Length);
		for (int i = 0; i < room.cameraPositions.Length - 1; i++)
		{ palette.fades[i] = rs.parent.fadePalette.fades[0]; }
		return palette;
	}

	public static ConditionalWeakTable<RoomCamera, Dictionary<FadePalette, Texture2D>> _MoreFadeTextures = new();

	public static Dictionary<FadePalette, Texture2D> MoreFadeTextures(this RoomCamera rc) => _MoreFadeTextures.GetValue(rc, _ => new());

	public static void ClearMoreFadeTextures(this RoomCamera rc)
	{
		foreach (Texture2D tex in MoreFadeTextures(rc).Values)
		{ UnityEngine.Object.Destroy(tex); }
		MoreFadeTextures(rc).Clear();
	}

	public static Texture2D GetMoreFadeTexture(this RoomCamera rc, FadePalette palette)
	{
		if (rc.MoreFadeTextures().ContainsKey(palette))
		{ return rc.MoreFadeTextures()[palette]; }
		else { return null!; }
	}

	public static void ChangeMoreFade(this RoomCamera self, FadePalette[] newFades)
	{
		self.ClearMoreFadeTextures();
		foreach (FadePalette fade in newFades)
		{
			Texture2D moreTex = null!;
			self.LoadPalette(fade.palette, ref moreTex);
			self.MoreFadeTextures()[fade] = moreTex;
		}
		self.ApplyFade();
	}

	#endregion


	public static void Apply()
	{
		On.RoomCamera.ChangeRoom += RoomCamera_ChangeRoom;
		On.RoomCamera.ApplyEffectColorsToAllPaletteTextures += RoomCamera_ApplyEffectColorsToAllPaletteTextures;
		try
		{
			IL.RoomCamera.ApplyFade += RoomCamera_ApplyFade;
		}
		catch (Exception e) { LogError($"[MoreFadePalettes] ApplyFade IL Failed!\n{e}"); }
		//On.HUD.RoomTransition.PlayerEnterShortcut += nah not important enough for the hassle

		On.RoomSettings.Load_Timeline += RoomSettings_Load;
		_CommonHooks.RoomSettingsSave += _CommonHooks_RoomSettingsSave;
		On.DevInterface.RoomSettingsPage.ctor += RoomSettingsPage_ctor;
		On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
	}

	public static void Undo()
	{
		On.RoomCamera.ChangeRoom -= RoomCamera_ChangeRoom;
		On.RoomCamera.ApplyEffectColorsToAllPaletteTextures -= RoomCamera_ApplyEffectColorsToAllPaletteTextures;
		IL.RoomCamera.ApplyFade -= RoomCamera_ApplyFade;
		//On.HUD.RoomTransition.PlayerEnterShortcut -=

		On.RoomSettings.Load_Timeline -= RoomSettings_Load;
		_CommonHooks.RoomSettingsSave -= _CommonHooks_RoomSettingsSave;
		On.DevInterface.RoomSettingsPage.ctor -= RoomSettingsPage_ctor;
		On.RainWorldGame.ShutDownProcess -= RainWorldGame_ShutDownProcess;
	}

	private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
	{
		orig(self);
		foreach (RoomCamera i in self.cameras)
		{
			i.ClearMoreFadeTextures();
		}
	}

	#region importantHooks
	private static void RoomCamera_ApplyFade(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.Before,
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<RoomCamera>(nameof(RoomCamera.ghostMode)),
			x => x.MatchLdcR4(0)))
		{
			c.MoveAfterLabels();
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate(ApplyMoreFades);
		}
		else { LogWarning("il hook for RoomCamera.ApplyFade failed"); }
	}

	private static void ApplyMoreFades(RoomCamera self)
	{
		if (self.MoreFadeTextures().Keys.Count <= 0) return;
		foreach (FadePalette fade in self.MoreFadeTextures().Keys)
		{
			Texture2D fadeTex = self.GetMoreFadeTexture(fade);
			if (fadeTex == null) continue;

			for (int i = 0; i < 32; i++)
			{
				for (int j = 8; j < 16; j++)
				{
					Color origColor = self.paletteTexture.GetPixel(i, j - 8);
					var newColor = Color.Lerp(fadeTex.GetPixel(i, j), fadeTex.GetPixel(i, j - 8), self.fadeCoord.y);
					if (fade.fades.Length > self.currentCameraPosition) //we're not throwing, even if it'll fail to render the fade
					{ self.paletteTexture.SetPixel(i, j - 8, Color.Lerp(origColor, newColor, fade.fades[self.currentCameraPosition])); }
				}
			}
		}
	}

	private static void RoomCamera_ApplyEffectColorsToAllPaletteTextures(On.RoomCamera.orig_ApplyEffectColorsToAllPaletteTextures orig, RoomCamera self, int color1, int color2)
	{
		if (self.MoreFadeTextures().Keys.Count > 0)
		{
			foreach (FadePalette fade in self.MoreFadeTextures().Keys.ToList())
			{
				if (fade != null && fade.palette != -1)
				{
					Texture2D tex = self.MoreFadeTextures()[fade];
					self.ApplyEffectColorsToPaletteTexture(ref tex, color1, color2);
					self.MoreFadeTextures()[fade] = tex;
				}
			}
		}
		orig(self, color1, color2);
	}

	private static void RoomCamera_ChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
	{
		self.currentCameraPosition = cameraPosition; //THIS IS REALLY IMPORTANT idk why the base game doesn't do this, shouldn't break anything

		self.ClearMoreFadeTextures();
		if (newRoom == null) { orig(self, newRoom, cameraPosition); return; }
		foreach (FadePalette fade in newRoom.roomSettings.GetAllFades())
		{
			Texture2D moreTex = null!;
			self.LoadPalette(fade.palette, ref moreTex);
			self.MoreFadeTextures()[fade] = moreTex;
		}
		orig(self, newRoom, cameraPosition);
	}

	private static bool RoomSettings_Load(On.RoomSettings.orig_Load_Timeline orig, RoomSettings self, SlugcatStats.Timeline playerChar)
	{
		if (!orig(self, playerChar)) return false;

		var list = File.ReadAllLines(self.filePath).Select(x => Regex.Split(x, ": ")).Where(x => x.Length == 2).ToList();

		Dictionary<int, FadePalette> moreFadeIndex = new();
		int greatest = -1;
		foreach (string[] line in list)
		{
			if (line[0].StartsWith(MFPstr) && line[0].Length > MFPstr.Length && int.TryParse(line[0].Substring(MFPstr.Length), out int index))
			{
				moreFadeIndex[index] = FadeFromString(Regex.Split(ValidateSpacedDelimiter(line[1], ","), ", "));
				if (index > greatest) greatest = index;
			}
		}

		self.AllFadePalettes().Clear();
		for (int i = 0; i <= greatest; i++)
		{
			if (moreFadeIndex.ContainsKey(i))
			{ self.AllFadePalettes().Add(moreFadeIndex[i]); }
			else
			{ self.AllFadePalettes().Add(null!); }
		}

		return true;
	}

	private static FadePalette FadeFromString(string[] s)
	{
		FadePalette fadePalette = new(int.Parse(s[0], NumberStyles.Any, CultureInfo.InvariantCulture), s.Length - 1);
		for (int i = 0; i < s.Length - 1; i++)
		{
			fadePalette.fades[i] = float.Parse(s[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
		}
		return fadePalette;
	}

	private static List<string> _CommonHooks_RoomSettingsSave(RoomSettings self, bool saveAsTemplate)
	{
		List<string> result = new();
		if (saveAsTemplate) return result;

		FadePalette[] array = self.GetAllFades();
		for (int i = 0; i < array.Length; i++)
		{
			result.Add($"{MFPstr}{i}: {array[i].palette}, {string.Join(", ", array[i].fades)}");
		}
		return result;
	}

	private static void RoomSettingsPage_ctor(On.DevInterface.RoomSettingsPage.orig_ctor orig, RoomSettingsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
	{
		orig(self, owner, IDstring, parentNode, name);
		self.subNodes.Add(new MoreFadeDevPanel(owner, self, new Vector2(450f, 360f), self.RoomSettings.GetAllFades().Count() - 1));
		return;
		for (int i = 0; i < self.RoomSettings.GetAllFades().Length; i++)
		{
			self.subNodes.Add(new MoreFadeDevPanel(owner, self, new Vector2(250f + 210f * i, 190f), i));
		}
	}
	#endregion

	private const string MFPstr = "MorePalette";

	internal const int MFPControl = -1;

	#region devUI
	public class MoreFadeDevPanel : Panel, IDevUISignals
	{
		public int index;
		public int camCount => owner.room.cameraPositions.Length;
		public FadePalette GetFade => RoomSettings.GetMoreFade(index);
		public MoreFadeDevPanel(DevUI owner, DevUINode parentNode, Vector2 pos, int index) : base(owner, $"MoreFade_{index}", parentNode, pos, new Vector2(210f, 25f + 20f * owner.room.cameraPositions.Length), $"Fade Palette: {(index == -1 ? "None" : +2)}")
		{
			this.index = index;

			RefreshSubnodes();
		}

		public void RefreshSubnodes()
		{
			Title = $"Fade Palette: {index + 2} / {RoomSettings.GetAllFades().Count() + 1}";
			if (index == -1) Title = "Fade Palette: None";

			for (int i = subNodes.Count - 1; i >= 0; i--)
			{ subNodes[i].ClearSprites(); }
			subNodes.Clear();

			float height = 25f;
			height += 40f;
			if (index != -1)
			{ height += 20f + camCount * 20f; }

			Resize(new Vector2(210f, height));

			if (pos.y + height > 700f)
			{
				Move(new Vector2(pos.x, 700f - height));
			}

			height -= 20f;

			subNodes.Add(new Button(owner, "Add_New", this, new Vector2(5f, height), 90f, "Add New"));

			height -= 20f;
			if (RoomSettings.GetAllFades().Count() > (index == -1 ? 0 : 0))
			{
				subNodes.Add(new Button(owner, "Move_Prev", this, new Vector2(5f, height), 90f, "Prev"));
				subNodes.Add(new Button(owner, "Move_Next", this, new Vector2(105f, height), 90f, "Next"));
			}

			if (index == -1) return;

			subNodes.Add(new Button(owner, "Delete", this, new Vector2(105f, height + 20f), 90f, "Delete"));

			height -= 30f;

			subNodes.Add(new MorePaletteFadeController(owner, IDstring + "_FadePalette", this, new Vector2(5f, height), "Fade Palette: "));
			for (int i = 0; i < camCount; i++)
			{
				height -= 20f;
				subNodes.Add(new MorePaletteFadeSlider(owner, IDstring + "_PaletteFadeSlider_" + i.ToString(), this, new Vector2(5f, height), "Screen " + i.ToString() + ": ", i));
			}
		}

		public void SwitchPages(int index)
		{
			if (index >= RoomSettings.GetAllFades().Count() || index < 0) return;
			this.index = index;
			RefreshSubnodes();
		}

		public void Resize(Vector2 newSize)
		{
			if (newSize != size)
			{
				size = newSize;
				base.Refresh();
			}
		}

		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			LogMessage("signalis");
			switch (sender.IDstring)
			{
			case "Add_New":
				RoomSettings.SetMoreFade(RoomSettings.GetAllFades().Count(), new FadePalette(0, camCount));
				index = RoomSettings.GetAllFades().Count() - 1;
				RefreshSubnodes();
				break;
			case "Delete":
				RoomSettings.DeleteMoreFade(index);
				index = Mathf.Max(0, index - 1);
				if (RoomSettings.GetAllFades().Count() <= 0)
				{
					index = -1;
					RefreshSubnodes();
				}
				else { SwitchPages(index); }
				owner.room.game.cameras[0].ChangeMoreFade(RoomSettings.GetAllFades());
				break;
			case "Move_Prev":
				SwitchPages(index - 1);
				break;
			case "Move_Next":
				SwitchPages(index + 1);
				break;
			}
		}
	}

	public class MorePaletteFadeController : PaletteController
	{
		public MorePaletteFadeController(DevUI owner, string IDstring, MoreFadeDevPanel parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title, MFPControl)
		{
		}

		MoreFadeDevPanel FadePanel => (parentNode as MoreFadeDevPanel)!;

		public override void Refresh()
		{
			if (FadePanel.GetFade == null)
			{ NumberLabelText = "NONE"; }
			else
			{ NumberLabelText = RoomSettings.GetMoreFade(FadePanel.index).palette.ToString(); }

			base.Refresh();
		}

		public override void Increment(int change)
		{
			if (FadePanel.GetFade == null)
			{
				RoomSettings.SetMoreFade(FadePanel.index, new FadePalette(0, owner.room.cameraPositions.Length));
			}
			else
			{
				FadePanel.GetFade.palette += change;
				if (FadePanel.GetFade.palette < 0)
				{
					RoomSettings.SetMoreFade(FadePanel.index, null!);
					owner.room.game.cameras[0].ChangeMoreFade(RoomSettings.GetAllFades());
				}
				else
				{
					owner.room.game.cameras[0].ChangeMoreFade(RoomSettings.GetAllFades());
				}
			}
			parentNode.Refresh();
			base.Increment(change);
		}
	}

	public class MorePaletteFadeSlider : Slider
	{
		// Token: 0x06003A0D RID: 14861 RVA: 0x000246E7 File Offset: 0x000228E7
		public MorePaletteFadeSlider(DevUI owner, string IDstring, MoreFadeDevPanel parentNode, Vector2 pos, string title, int index) : base(owner, IDstring, parentNode, pos, title, false, 65f)
		{
			this.index = index;
		}

		MoreFadeDevPanel FadePanel => (parentNode as MoreFadeDevPanel)!;

		public override void Refresh()
		{
			base.Refresh();
			if (FadePanel.GetFade == null)
			{
				NumberText = " - ";
				RefreshNubPos(0f);
				return;
			}
			NumberText = ((int)(FadePanel.GetFade.fades[index] * 100f)).ToString() + "%";
			RefreshNubPos(FadePanel.GetFade.fades[index]);
		}

		public override void NubDragged(float nubPos)
		{
			if (FadePanel.GetFade == null)
			{
				return;
			}
			FadePanel.GetFade.fades[index] = nubPos;
			if (owner.room.game.cameras[0].currentCameraPosition == index)
			{
				owner.room.game.cameras[0].ChangeMoreFade(RoomSettings.GetAllFades());
			}
			Refresh();
		}

		public int index;
	}
	#endregion
}
