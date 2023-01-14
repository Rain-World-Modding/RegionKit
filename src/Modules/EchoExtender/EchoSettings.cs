using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


namespace RegionKit.Modules.EchoExtender;

public struct EchoSettings
{
	public Dictionary<SlugcatStats.Name, string> EchoRoom;
	public Dictionary<SlugcatStats.Name, float> EchoSizeMultiplier;
	public Dictionary<SlugcatStats.Name, float> EffectRadius;
	public Dictionary<SlugcatStats.Name, bool> RequirePriming;
	public Dictionary<SlugcatStats.Name, int> MinimumKarma;
	public Dictionary<SlugcatStats.Name, int> MinimumKarmaCap;
	public Dictionary<SlugcatStats.Name, float> DefaultFlip;
	public SlugcatStats.Name[] SpawnOnDifficulty;
	public Dictionary<SlugcatStats.Name, string> EchoSong;

	public string GetEchoRoom(SlugcatStats.Name diff)
	{
		if (EchoRoom.ContainsKey(diff)) return EchoRoom[diff];
		return "";
	}

	public float GetSizeMultiplier(SlugcatStats.Name diff)
	{
		if (EchoSizeMultiplier.ContainsKey(diff)) return EchoSizeMultiplier[diff];
		return Default.EchoSizeMultiplier[diff];
	}

	public float GetRadius(SlugcatStats.Name diff)
	{
		if (EffectRadius.ContainsKey(diff)) return EffectRadius[diff];
		return Default.EffectRadius[diff];
	}

	public bool GetPriming(SlugcatStats.Name diff)
	{
		if (RequirePriming.ContainsKey(diff)) return RequirePriming[diff];
		return Default.RequirePriming[diff];
	}

	public int GetMinimumKarma(SlugcatStats.Name diff)
	{
		if (MinimumKarma.ContainsKey(diff)) return MinimumKarma[diff] - 1;
		return Default.MinimumKarma[diff] - 1;
	}

	public int GetMinimumKarmaCap(SlugcatStats.Name diff)
	{
		if (MinimumKarmaCap.ContainsKey(diff)) return MinimumKarmaCap[diff] - 1;
		return Default.MinimumKarmaCap[diff] - 1;
	}

	public string GetEchoSong(SlugcatStats.Name diff)
	{
		if (EchoSong.ContainsKey(diff)) return EchoSong[diff];
		return Default.EchoSong[diff];
	}

	public bool SpawnOnThisDifficulty(SlugcatStats.Name diff)
	{
		if (SpawnOnDifficulty.Length > 0) return SpawnOnDifficulty.Contains(diff);
		return Default.SpawnOnDifficulty.Contains(diff);
	}

	public float GetDefaultFlip(SlugcatStats.Name diff)
	{
		if (DefaultFlip.ContainsKey(diff)) return DefaultFlip[diff];
		return Default.DefaultFlip[diff];
	}

	public static EchoSettings Default;

	static EchoSettings()
	{
		Default = Empty;
		Default.EchoRoom.AddMultiple("", SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.RequirePriming.AddMultiple(true, SlugcatStats.Name.White, SlugcatStats.Name.Yellow);
		Default.RequirePriming.Add(SlugcatStats.Name.Red, false);
		Default.EffectRadius.AddMultiple(4, SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.MinimumKarma.AddMultiple(-1, SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.MinimumKarmaCap.AddMultiple(0, SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.SpawnOnDifficulty = new[] { SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red };
		Default.EchoSong.AddMultiple("NA_3SlugcatStats.Name.Red - ElseSlugcatStats.Name.Yellow", SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.EchoSizeMultiplier.AddMultiple(0, SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
		Default.DefaultFlip.AddMultiple(0, SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red);
	}

	// Hook-friendly
	public static List<SlugcatStats.Name> DefaultDifficulties() 
	=> new (){ SlugcatStats.Name.White, SlugcatStats.Name.Yellow, SlugcatStats.Name.Red };

	public static EchoSettings Empty => new EchoSettings()
	{
		EchoRoom = new (),
		EchoSizeMultiplier = new(),
		EffectRadius = new (),
		MinimumKarma = new (),
		MinimumKarmaCap = new (),
		RequirePriming = new (),
		EchoSong = new(),
		SpawnOnDifficulty = new SlugcatStats.Name[0],
		DefaultFlip = new()
	};

	public static EchoSettings FromFile(string path)
	{
		//TODO: change paths to fit new fs
		plog.LogMessage("[Echo Extender] Found settings file: " + path);
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
						if (!ExtEnumBase.TryParse(typeof(SlugcatStats.Name), rawNum, out object result))
						{
							plog.LogWarning($"[Echo Extender] Found an invalid character name '{rawNum}'! Skipping : " + row);
							continue;
						}

						difficulties.Add((SlugcatStats.Name)result);
					}

					pass = pass.Substring(pass.IndexOf(")", StringComparison.Ordinal) + 1);
				}
				else difficulties = DefaultDifficulties();

				switch (pass.Trim().ToLower())
				{
				case "room":
					settings.EchoRoom.AddMultiple(split[1].Trim(), difficulties);
					break;
				case "size":
					settings.EchoSizeMultiplier.AddMultiple(float.Parse(split[1]), difficulties);
					break;
				case "radius":
					settings.EffectRadius.AddMultiple(float.Parse(split[1]), difficulties);
					break;
				case "priming":
					settings.RequirePriming.AddMultiple(bool.Parse(split[1]), difficulties);
					break;
				case "minkarma":
					settings.MinimumKarma.AddMultiple(int.Parse(split[1]), difficulties);
					break;
				case "minkarmacap":
					settings.MinimumKarmaCap.AddMultiple(int.Parse(split[1]), difficulties);
					break;
				case "difficulties":
					settings.SpawnOnDifficulty = split[1].Split(',').Select(s => (SlugcatStats.Name)ExtEnumBase.Parse(typeof(SlugcatStats.Name), s.Trim(), false)).ToArray();
					break;
				case "echosong":
					string trimmed = split[1].Trim();
					string result = EchoParser.EchoSongs.TryGetValue(trimmed, out string song) ? song : trimmed;
					settings.EchoSong.AddMultiple(result, difficulties);
					break;
				case "defaultflip":
					settings.DefaultFlip.AddMultiple(float.Parse(split[1]), difficulties);
					break;
				default:
					plog.LogWarning($"[Echo Extender] Setting '{pass.Trim().ToLower()}' not found! Skipping : " + row);
					break;
				}
			}

			catch (Exception)
			{
				plog.LogWarning("[Echo Extender] Failed to parse line " + row);
			}
		}

		return settings;
	}

	public bool KarmaCondition(int karma, int karmaCap, SlugcatStats.Name diff)
	{
		if (GetMinimumKarma(diff) == -2)
		{
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
}
