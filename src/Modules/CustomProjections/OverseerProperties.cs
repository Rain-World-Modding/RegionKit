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
		IL.Region.ctor_string_int_int_RainWorldGame_Timeline += Region_ctor;
	}
	public static void Undo()
	{
		IL.Region.ctor_string_int_int_RainWorldGame_Timeline -= Region_ctor;
	}

	private static void Region_ctor(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.AfterLabel,
			x => x.MatchCall("<PrivateImplementationDetails>", "ComputeStringHash")
			))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, 7);
			c.EmitDelegate((Region self, string[] line) => GetOverseerProperties(self).SetProperties(line));
		}
		else { LogMessage("failed to il hook Region.ctor"); }
	}

	public void SetProperties(string[] line)
	{
		switch (line[0])
		{
		case "guideDestinationRoom":
			CustomDestinationRoom = line[1];
			break;

		case "guideProgressionSymbol":
			ProgressionSymbol = line[1];
			break;

		case "guideShelterWeight":
			float.TryParse(line[1], out ShelterShowWeight);
			break;

		case "guideBatWeight":
			float.TryParse(line[1], out BatShowWeight);
			break;

		case "guideProgressionWeight":
			float.TryParse(line[1], out ProgressionShowWeight);
			break;

		case "guideDangerousCreatureWeight":
			float.TryParse(line[1], out DangerousCreatureWeight);
			break;

		case "guideDeliciousFoodWeight":
			float.TryParse(line[1], out DeliciousFoodWeight);
			break;

		case "guideColor":
			if (TryParseOverseerColor(line[1], out var result))
			{ guideColor = result; }
			break;

		case "inspectorColor":
			if (TryParseOverseerColor(line[1], out var result2))
			{ inspectorColor = result2; }
			break;

		default:
			if (line[0].StartsWith("overseersColorOverride"))
			{
				int start = line[0].IndexOf('(') + 1, end = line[0].IndexOf(')');
				if (start != -1 && start < end && TryParseOverseerColor(line[0].Substring(start, end - start), out var result3) && float.TryParse(line[1], out var num))
				{ overseerColorChances[result3] = num; }
			}
			break;
		}
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

		if (float.TryParse(array4[0], out var r) && float.TryParse(array4[1], out var g) && float.TryParse(array4[2], out var b))
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

	public int guideID  => guideColor is Color color? GetOverseerID(color) : -1;

	private Color? guideColor = null;
	public int inspectorID => inspectorColor is Color color ? GetOverseerID(color) : -1;
	private Color? inspectorColor = null;

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
