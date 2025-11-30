using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace RegionKit.Modules.CustomProjections;

internal class OverseerProperties
{
	private static ConditionalWeakTable<Region.RegionParams, OverseerProperties> _AttachedProperties = new();

	public static OverseerProperties GetOverseerProperties(Region? p) => p != null ? _AttachedProperties.GetValue(p.regionParams, _ => new()) : new();

	public static void Apply()
	{
		//IL.Region.ctor_string_int_int_RainWorldGame_Timeline += Region_ctor;
		//On.Region.ctor_string_int_int_RainWorldGame_Timeline += Region_ctor_string_int_int_RainWorldGame_Timeline;
	}

	public static void Undo()
	{
		//IL.Region.ctor_string_int_int_RainWorldGame_Timeline -= Region_ctor;
	}

	public static bool TryParseOverseerColor(string s, out Color result)
	{
		if (int.TryParse(s, out var num) && 0 <= num && num < BaseGameColors.Count)
		{
			result = BaseGameColors[num];
			return true;
		}
		return TryParseColor(s, out result);
	}

	public static bool TryParseColor(string s, out Color result)
	{
		result = new();
		string[] array4 = Regex.Split(Custom.ValidateSpacedDelimiter(s, ","), ", ");

		if (array4.Length >= 3 && float.TryParse(array4[0], out var r) && float.TryParse(array4[1], out var g) && float.TryParse(array4[2], out var b))
		{ result = new Color(r, g, b); return true; }

		return false;
	}

	public string CustomDestinationRoom = "";

	public string ProgressionSymbol = "";

	public float ShelterShowWeight = 1f;

	public float BatShowWeight = 1f;

	public float ProgressionShowWeight = 1f;

	public float DangerousCreatureWeight = 1f;

	public float DeliciousFoodWeight = 1f;

	public int guideID  => GuideColor is Color color? GetOverseerID(color) : -1;

	public Color? GuideColor = null;
	public int inspectorID => InspectorColor is Color color ? GetOverseerID(color) : -1;
	public Color? InspectorColor = null;

	public Dictionary<Color, float> overseerColorChances = new();

	public Dictionary<int, Color> overseerColorLookup = new();

	const int BaseNumber = -10000;

	public int GetOverseerID(Color color)
	{
		if (BaseGameColors.Contains(color) && BaseIndex(BaseGameColors.IndexOf(color)))
		{ return BaseGameColors.IndexOf(color); }

		if (!overseerColorLookup.ContainsValue(color))
		{ overseerColorLookup[BaseNumber + overseerColorLookup.Count] = color; }

		return overseerColorLookup.FirstOrDefault(x => x.Value == color).Key;
	}

	public Color GetOverseerColor(int id) => overseerColorLookup[id];

	public static bool BaseIndex(int num) => num <= (ModManager.MSC ? 5 : 2) && num >= 0;

	public static List<Color> BaseGameColors => new()
	{
		new Color(0.44705883f, 0.9019608f, 0.76862746f),
		new Color(1f, 0.8f, 0.3f),
		new Color(0f, 1f, 0f),
		new Color(1f, 0.2f, 0f),
		new Color(0.9f, 0.95f, 1f),
		new Color(0.56f, 0.27f, 0.68f),
	};
}
