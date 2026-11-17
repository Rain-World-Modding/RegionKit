using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace RegionKit.Modules.CustomProjections;

internal class OverseerProperties
{
	private static ConditionalWeakTable<Region.RegionParams, OverseerProperties> _AttachedProperties = new();

	public static OverseerProperties? GetOverseerProperties(Region? r) => r != null && _AttachedProperties.TryGetValue(r.regionParams, out OverseerProperties p) ? p : null;

	public static void Apply()
	{
		_CommonHooks.GeneralUnrecognizedRegionParamProcessor += OverseerParamProcessor;
	}

	public static void Undo()
	{
		_CommonHooks.GeneralUnrecognizedRegionParamProcessor -= OverseerParamProcessor;
	}

	public static void OverseerParamProcessor(Region region, string key, string value)
	{
		if (_AttachedProperties.TryGetValue(region.regionParams, out OverseerProperties properties))
		{
			properties.SetProperties(key, value);
		}
		else
		{
			properties = new();
			if (properties.SetProperties(key, value))
			{
				_AttachedProperties.Add(region.regionParams, properties);
			}
		}
	}

	private bool SetProperties(string key, string value)
	{
		switch (key)
		{
			case "guideDestinationRoom":
				CustomDestinationRoom = value;
				return true;

			case "guideProgressionSymbol":
				ProgressionSymbol = value;
				return true;

			case "guideShelterWeight":
				float.TryParse(value, out ShelterShowWeight);
				return true;

			case "guideBatWeight":
				float.TryParse(value, out BatShowWeight);
				return true;

			case "guideProgressionWeight":
				float.TryParse(value, out ProgressionShowWeight);
				return true;

			case "guideDangerousCreatureWeight":
				float.TryParse(value, out DangerousCreatureWeight);
				return true;

			case "guideDeliciousFoodWeight":
				float.TryParse(value, out DeliciousFoodWeight);
				return true;

			case "guideColor":
				if (TryParseOverseerColor(value, out var result))
				{ 
					guideColor = result;
					return true;
				}
				break;

			case "inspectorColor":
				if (TryParseOverseerColor(value, out var result2))
				{
					inspectorColor = result2;
					return true;
				}
				break;

			default:
				if (key.StartsWith("overseersColorOverride"))
				{
					int start = key.IndexOf('(') + 1, end = key.IndexOf(')');
					if (start != -1 && start < end && TryParseOverseerColor(key.Substring(start, end - start), out var result3) && float.TryParse(value, out var num))
					{ 
						overseerColorChances[result3] = num;
						return true;
					}
				}
				break;
		}
		return false;
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
		{ 
			result = new Color(r, g, b); 
			return true; 
		}

		return false;
	}

	public string CustomDestinationRoom = "";

	public string ProgressionSymbol = "";

	public float ShelterShowWeight = 1f;

	public float BatShowWeight = 1f;

	public float ProgressionShowWeight = 1f;

	public float DangerousCreatureWeight = 1f;

	public float DeliciousFoodWeight = 1f;

	public int guideID => guideColor is Color color ? GetOverseerID(color) : -1;

	private Color? guideColor = null;
	public int inspectorID => inspectorColor is Color color ? GetOverseerID(color) : -1;
	private Color? inspectorColor = null;

	public Dictionary<Color, float> overseerColorChances = new();

	public Dictionary<int, Color> overseerColorLookup = new();

	const int BaseNumber = -10000;

	public int GetOverseerID(Color color)
	{
		if (BaseGameColors.Contains(color) && BaseIndex(BaseGameColors.IndexOf(color)))
		{ 
			return BaseGameColors.IndexOf(color); 
		}

		if (!overseerColorLookup.ContainsValue(color))
		{ 
			overseerColorLookup[BaseNumber + overseerColorLookup.Count] = color; 
		}

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
