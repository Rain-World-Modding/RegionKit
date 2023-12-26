using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;

namespace RegionKit.Modules.CustomProjections;

internal class OverseerProperties
{
	private static ConditionalWeakTable<Region.RegionParams, OverseerProperties> _AttachedProperties = new();

	public static OverseerProperties GetOverseerProperties(Region p) => p != null ? _AttachedProperties.GetValue(p.regionParams, _ => new()) : new();

	public static void Apply()
	{
		IL.Region.ctor += Region_ctor;
	}
	public static void Undo()
	{
		IL.Region.ctor -= Region_ctor;
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
		}
	}

	public string CustomDestinationRoom = "";

	public string ProgressionSymbol = "";

	public float ShelterShowWeight = 1f;

	public float BatShowWeight = 1f;

	public float ProgressionShowWeight = 1f;

	public float DangerousCreatureWeight = 1f;

	public float DeliciousFoodWeight = 1f;
}
