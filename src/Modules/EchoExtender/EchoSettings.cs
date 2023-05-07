using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using MscNames = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;


namespace RegionKit.Modules.EchoExtender;

/// <summary>
/// Settings for a registered echo. All settings are keyed by difficulty.
/// </summary>
public struct EchoSettings
{
	internal bool _initialized;

	/// <summary>
	/// Which room should the echo appear in
	/// </summary>
	public Dictionary<SlugcatStats.Name, string> EchoRoom;
	/// <summary>
	/// Echo size multiplier 
	/// </summary>
	public Dictionary<SlugcatStats.Name, float> EchoSizeMultiplier;
	/// <summary>
	/// Map radius of echo presence
	/// </summary>
	public Dictionary<SlugcatStats.Name, float> EffectRadius;
	/// <summary>
	/// Whether to require priming
	/// </summary>
	public Dictionary<SlugcatStats.Name, PrimingKind> RequirePriming;
	/// <summary>
	/// Minimum karma required to see
	/// </summary>
	public Dictionary<SlugcatStats.Name, int> MinimumKarma;
	/// <summary>
	/// Minimum karma cap required to see
	/// </summary>
	public Dictionary<SlugcatStats.Name, int> MinimumKarmaCap;
	/// <summary>
	/// Default rotation
	/// </summary>
	public Dictionary<SlugcatStats.Name, float> DefaultFlip;
	/// <summary>
	/// Which characters have the echo
	/// </summary>
	public SlugcatStats.Name[] SpawnOnDifficulty;
	/// <summary>
	/// Song name
	/// </summary>
	public Dictionary<SlugcatStats.Name, string> EchoSong;

#pragma warning disable 1591
	public string GetEchoRoom(SlugcatStats.Name diff)
	{
		string res;
		if (EchoRoom.TryGetValue(diff, out res)) return res;
		if (Default.EchoRoom.TryGetValue(diff, out res)) return res;
		return "";
	}

	public float GetSizeMultiplier(SlugcatStats.Name diff)
	{
		float res;
		if (EchoSizeMultiplier.TryGetValue(diff, out res)) return res;
		if (Default.EchoSizeMultiplier.TryGetValue(diff, out res)) return res;
		return 1f;
	}

	public float GetRadius(SlugcatStats.Name diff)
	{
		float res;
		if (EffectRadius.TryGetValue(diff, out res)) return res;
		if (Default.EffectRadius.TryGetValue(diff, out res)) return res;
		return 4f;
	}

	public PrimingKind GetPriming(SlugcatStats.Name diff)
	{
		PrimingKind res;
		if (RequirePriming.TryGetValue(diff, out res)) return res;
		if (Default.RequirePriming.TryGetValue(diff, out res)) return res;
		return PrimingKind.Yes;
	}

	public int GetMinimumKarma(SlugcatStats.Name diff)
	{
		int res;
		if (MinimumKarma.TryGetValue(diff, out res)) return res - 1;
		if (Default.MinimumKarma.TryGetValue(diff, out res)) return res - 1;
		return -2;
	}

	public int GetMinimumKarmaCap(SlugcatStats.Name diff)
	{
		int res;
		if (MinimumKarmaCap.TryGetValue(diff, out res)) return res - 1;
		if (Default.MinimumKarmaCap.TryGetValue(diff, out res)) return res - 1;
		return -1;
	}

	public string GetEchoSong(SlugcatStats.Name diff)
	{
		string res;
		if (EchoSong.TryGetValue(diff, out res)) return res;
		if (Default.EchoSong.TryGetValue(diff, out res)) return res;
		return "NA_32 - Else1";
	}

	public bool SpawnOnThisDifficulty(SlugcatStats.Name diff)
	{
		//todo: check whether it works when msc is toggled
		if (SpawnOnDifficulty.Count() > 0) return SpawnOnDifficulty.Contains(new(diff.value));
		return Default.SpawnOnDifficulty.Contains(new(diff.value));
	}

	public float GetDefaultFlip(SlugcatStats.Name diff)
	{
		if (DefaultFlip.ContainsKey(diff)) return DefaultFlip[diff];
		return Default.DefaultFlip[diff];
	}
#pragma warning restore 1591
	/// <summary>
	/// Default configuration
	/// </summary>
	public static EchoSettings Default;
	internal static void InitDefault()
	{
		Default = Empty;
		Default.EchoRoom.AddMultiple("", SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		//--Default.RequirePriming.AddMultiple(PrimingKind.Yes, nameof(SlugcatStats));
		Default.RequirePriming.Add(SlugcatStats.Name.Red, PrimingKind.No);
		Default.RequirePriming.Add(new(nameof(MscNames.Saint)), PrimingKind.Saint);
		Default.EffectRadius.AddMultiple(4, SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.MinimumKarma.AddMultiple(-1, SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.MinimumKarmaCap.AddMultiple(0, SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.SpawnOnDifficulty = DefaultDifficulties().ToArray();
		//Default.EchoSong.AddMultiple("NA_32 - Else1", SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.EchoSizeMultiplier.AddMultiple(0, SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.DefaultFlip.AddMultiple(0, SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
	}

	/// <summary>
	/// Returns all difficulties
	/// </summary>
	/// <returns></returns>
	public static List<SlugcatStats.Name> DefaultDifficulties()
	 	=> new() {
			SlugcatStats.Name.White,
			SlugcatStats.Name.Yellow,
			SlugcatStats.Name.Red,
			new(nameof(MscNames.Artificer)),
			new(nameof(MscNames.Gourmand)),
			new(nameof(MscNames.Spear)),
			new(nameof(MscNames.Rivulet)),
			new(nameof(MscNames.Saint))
			};

	/// <summary>
	/// Returns a completely empty instance
	/// </summary>
	public static EchoSettings Empty => new EchoSettings()
	{
		EchoRoom = new(),
		EchoSizeMultiplier = new(),
		EffectRadius = new(),
		MinimumKarma = new(),
		MinimumKarmaCap = new(),
		RequirePriming = new(),
		EchoSong = new(),
		SpawnOnDifficulty = new SlugcatStats.Name[0],
		DefaultFlip = new()
	};
	/// <summary>
	/// Tries to parse echo settings from a file
	/// </summary>
	public static EchoSettings FromFile(string path)
	{
		__logger.LogMessage("[Echo Extender] Found settings file: " + path);
		if (!File.Exists(path))
		{
			__logger.LogError("[Echo Extender] Error: File not found!");
			return Default;
		}
		string[] rows = File.ReadAllLines(path);
		EchoSettings settings = Empty;
		foreach (string row in rows)
		{

			if (row.StartsWith("#") || row.StartsWith("//")) continue;
			try
			{
				string[] split = row.Split(':');
				string pass = split[0].Trim();
				List<SlugcatStats.Name> difficulties = new();
				if (pass.StartsWith("("))
				{
					foreach (string rawNum in pass.Substring(1, pass.IndexOf(')') - 1).SplitAndRemoveEmpty(","))
					{
						SlugcatStats.Name tarname = new(rawNum, false);
						if (tarname.Index < 0)
						{
							__logger.LogWarning($"[Echo Extender] Found an invalid character name '{rawNum}'! Skipping : " + row);
							continue;
						}
						difficulties.Add(tarname);
					}
					pass = pass.Substring(pass.IndexOf(")", StringComparison.Ordinal) + 1);
				}
				else difficulties = DefaultDifficulties();

				//float floatval = float.Parse(split[1]);
				string trimmed = split[1].Trim();
				bool
					sfloat = float.TryParse(trimmed, out float floatval),
					sint = int.TryParse(trimmed, out int intval);
				switch (pass.Trim().ToLower())
				{
				case "room":
					settings.EchoRoom.AddMultiple(trimmed, difficulties);
					break;
				case "size":
					settings.EchoSizeMultiplier.AddMultiple(floatval, difficulties);
					break;
				case "radius":
					settings.EffectRadius.AddMultiple(floatval, difficulties);
					break;
				case "priming":
					settings.RequirePriming.AddMultiple((PrimingKind)intval, difficulties);
					break;
				case "minkarma":
					settings.MinimumKarma.AddMultiple(intval, difficulties);
					break;
				case "minkarmacap":
					settings.MinimumKarmaCap.AddMultiple(intval, difficulties);
					break;
				case "difficulties":
					settings.SpawnOnDifficulty = split[1].Split(',').Select(s => new SlugcatStats.Name(s.Trim(), false)).ToArray();
					break;
				case "echosong":
					string result = EchoParser.__echoSongs.TryGetValue(trimmed, out string song) ? song : trimmed;
					settings.EchoSong.AddMultiple(result, difficulties);
					break;
				case "defaultflip":
					settings.DefaultFlip.AddMultiple(floatval, difficulties);
					break;
				default:
					__logger.LogWarning($"[Echo Extender] Setting '{pass.Trim().ToLower()}' not found! Skipping : " + row);
					break;
				}
			}
			catch (Exception ex)
			{
				__logger.LogWarning($"[Echo Extender] Failed to parse line \"{row}\" : {ex}");
			}
		}

		return settings;
	}
	/// <summary>
	/// Whether selected karma and karma cap fulfill the echo's conditions
	/// </summary>
	public bool KarmaCondition(int karma, int karmaCap, SlugcatStats.Name diff)
	{
		MinimumKarma.TryGetValue(diff, out var mymin);
		Default.MinimumKarma.TryGetValue(diff, out var defmin);
		if (GetMinimumKarma(diff) == -2)
		{
			__logger.LogMessage($"[Echo Extender] checking dynamic karma: {mymin}, {defmin}, {karma}, {karmaCap}");
			switch (karmaCap)
			{
			case 4:
				return karma >= 4;
			case 6:
				return karma >= 5;
			default:
				return karma >= 6;
			}
		}
		return karma >= GetMinimumKarma(diff);
	}
	/// <summary>
	/// Types of echo priming
	/// </summary>
	public enum PrimingKind
	{
		/// <summary>
		/// Priming required
		/// </summary>
		Yes = 1,
		/// <summary>
		/// No priming required
		/// </summary>
		No = 0,
		/// <summary>
		/// No priming, causes saint's hunch
		/// </summary>
		Saint = 2
	};
}
