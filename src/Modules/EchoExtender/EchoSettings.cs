using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RegionKit.Extras;
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
	public string EchoRoom;
	/// <summary>
	/// Echo size multiplier 
	/// </summary>
	public float EchoSizeMultiplier;
	/// <summary>
	/// Map radius of echo presence
	/// </summary>
	public float EffectRadius;
	/// <summary>
	/// Whether to require priming
	/// </summary>
	public PrimingKind RequirePriming;
	/// <summary>
	/// Minimum karma required to see
	/// </summary>
	public int MinimumKarma;
	/// <summary>
	/// Minimum karma cap required to see
	/// </summary>
	public int MinimumKarmaCap;
	/// <summary>
	/// Default rotation
	/// </summary>
	public float DefaultFlip;
	/// <summary>
	/// Which characters have the echo
	/// </summary>
	public bool SpawnOnDifficulty;
	/// <summary>
	/// Song name
	/// </summary>
	public string EchoSong;

	internal static EchoSettings GetDefault(SlugcatStats.Name name) => new()
	{
		EchoRoom = "",
		EchoSizeMultiplier = 1f,
		EffectRadius = 4f,
		MinimumKarma = -1,
		MinimumKarmaCap = 0,
		RequirePriming = (name == SlugcatStats.Name.Red)? PrimingKind.No : (name == MscNames.Saint)? PrimingKind.Saint : PrimingKind.Yes,
		EchoSong = "NA_32 - Else1",
		SpawnOnDifficulty = true,
		DefaultFlip = 0f
	};

	/// <summary>
	/// Tries to parse echo settings from a file
	/// </summary>
	public static EchoSettings FromFile(string path, SlugcatStats.Name name)
	{
		EchoSettings settings = GetDefault(name);

		if (!File.Exists(path))
		{
			LogError("[Echo Extender] No settings file found! Using default");
			return settings;
		}

		LogMessage("[Echo Extender] Found settings file: " + path);
		string[] rows = File.ReadAllLines(path);

		settings.SpawnOnDifficulty = !rows.Any(x => x.ToLower().Contains("spawnondifficulty")); //kinda hacky, but necessary

		foreach (string row in ProcessTimelineConditions(rows, SlugcatStats.SlugcatToTimeline(name)))
		{

			if (row.StartsWith("#") || row.StartsWith("//")) continue;
			try
			{
				string[] split = row.Split(':');
				string pass = split[0].Trim();
				string trimmed = split.Length >= 2? split[1].Trim() : "";
				bool
					sfloat = float.TryParse(trimmed, out float floatval),
					sint = int.TryParse(trimmed, out int intval);
				switch (pass.Trim().ToLower())
				{
				case "room":
					settings.EchoRoom = trimmed;
					break;
				case "size":
					settings.EchoSizeMultiplier = floatval;
					break;
				case "radius":
					settings.EffectRadius = floatval;
					break;
				case "priming":
					if (sint) settings.RequirePriming = (PrimingKind)intval;
					else
					{
						settings.RequirePriming = trimmed.ToLower() switch
						{
							"true" => PrimingKind.Yes,
							"false" => PrimingKind.No,
							"saint" => PrimingKind.Saint,
							_ => settings.RequirePriming, //don't do anything
						};
					}
					break;
				case "minkarma":
					settings.MinimumKarma = intval;
					break;
				case "minkarmacap":
					settings.MinimumKarmaCap = intval;
					break;
				case "difficulties":
					LogWarning($"[Echo Extender] 'difficulties' is obsolete! New format is [({trimmed})SpawnOnDifficulty]");
					if(RainWorldTools.StringMatchesTimeline(trimmed,SlugcatStats.SlugcatToTimeline(name))) settings.SpawnOnDifficulty = true;
					//settings.SpawnOnDifficulty = split[1].Split(',').Select(s => new SlugcatStats.Name(s.Trim(), false)).ToArray();
					break;
				case "spawnondifficulty":
					settings.SpawnOnDifficulty = true;
					break;
				case "echosong":
					string result = EchoParser.__echoSongs.TryGetValue(trimmed, out string song) ? song : trimmed;
					settings.EchoSong = result;
					break;
				case "defaultflip":
					settings.DefaultFlip = floatval;
					break;
				default:
					LogWarning($"[Echo Extender] Setting '{pass.Trim().ToLower()}' not found! Skipping : " + row);
					break;
				}
			}
			catch (Exception ex)
			{
				LogWarning($"[Echo Extender] Failed to parse line \"{row}\" : {ex}");
			}
		}

		return settings;
	}
	/// <summary>
	/// Whether selected karma and karma cap fulfill the echo's conditions
	/// </summary>
	public bool KarmaCondition(int karma, int karmaCap)
	{
		var mymin = MinimumKarma;
		if (MinimumKarma == -1)
		{
			LogMessage($"[Echo Extender] checking dynamic karma: {mymin}, {karma}, {karmaCap}");
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
		return karma >= MinimumKarma;
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
