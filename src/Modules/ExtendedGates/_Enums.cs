//extended gates by Henpemaz

using Req = RegionGate.GateRequirement;

namespace RegionKit.Modules.ExtendedGates;

public static class _Enums
{
	public static Req Construction = new(nameof(Construction), true);
	public static Req uwu = new(nameof(uwu), true);
	public static Req Open = new(nameof(Open), true);
	public static Req Forbidden = new(nameof(Forbidden), true);
	public static Req Glow = new(nameof(Glow), true);
	public static Req CommsMark = new(nameof(CommsMark), true);
	public static Req SixKarma = new("6", true);
	public static Req SevenKarma = new("7", true);
	public static Req EightKarma = new("8", true);
	public static Req NineKarma = new("9", true);
	public static Req TenKarma = new("10", true);
	public static Req Ripple1_0 = new("Ripple1.0", true);
	public static Req Ripple1_5 = new("Ripple1.5", true);
	public static Req Ripple2_0 = new("Ripple2.0", true);
	public static Req Ripple2_5 = new("Ripple2.5", true);
	public static Req Ripple3_0 = new("Ripple3.0", true);
	public static Req Ripple3_5 = new("Ripple3.5", true);
	public static Req Ripple4_0 = new("Ripple4.0", true);
	public static Req Ripple4_5 = new("Ripple4.5", true);
	public static Req Ripple5_0 = new("Ripple5.0", true);
	public static Req[] reinforced = new Req[6];
	public static Req[] alt = new Req[21];
	public static Req[] txt = new Req[10];

	public static void Register()
	{
		reinforced = [Req.OneKarma, Req.TwoKarma, Req.ThreeKarma, Req.FourKarma, Req.FiveKarma, TenKarma];
		for (int i = 0; i < reinforced.Length; i++)
		{
			reinforced[i] = new(reinforced[i].value + ExtendedGates.REINFORCED_POSTFIX, true);
		}

		const int numericals = 10;
		var specials = new Req[6] { Construction, uwu, Open, Forbidden, Glow, CommsMark };

		alt = new Req[numericals + specials.Length + reinforced.Length];

		foreach (int i in Range(10))
		{
			txt[i] = new(i + 1 + ExtendedGates.TXT_POSTFIX, true);
		}

		foreach (int i in Range(numericals))
		{
			alt[i] = new(i + 1 + ExtendedGates.ALT_POSTFIX, true);
		}


		for (int i = 0; i < specials.Length; i++)
		{
			alt[i + numericals] = new(specials[i].value + ExtendedGates.ALT_POSTFIX, true);
		}

		for (int i = 0; i < reinforced.Length; i++)
		{
			alt[i + numericals + specials.Length] = new(reinforced[i].value + ExtendedGates.ALT_POSTFIX, true);
		}
	}
}
