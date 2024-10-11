//extended gates by Henpemaz

using Req = RegionGate.GateRequirement;

namespace RegionKit.Modules.Misc;

public static class _Enums
{
	public static Req Construction = new(nameof(Construction), true);
	public static Req uwu = new(nameof(uwu), true);
	public static Req Open = new(nameof(Open), true);
	public static Req Forbidden = new(nameof(Forbidden), true);
	public static Req Glow = new(nameof(Glow), true);
	public static Req CommsMark = new(nameof(CommsMark), true);
	public static Req TenReinforced = new(nameof(TenReinforced), true);
	public static Req SixKarma = new("6", true);
	public static Req SevenKarma = new("7", true);
	public static Req EightKarma = new("8", true);
	public static Req NineKarma = new("9", true);
	public static Req TenKarma = new("10", true);
	public static Req[] alt = new Req[17];
	public static Req[] txt = new Req[5];
	public static void Register()
	{
		const int numericals = 10;
		Req[] specials = new Req[7] { Construction, uwu, Open, Forbidden, Glow, CommsMark, TenReinforced };

		alt = new Req[numericals + specials.Length];

		foreach (int i in Range(5))
		{
			txt[i] = new((i + 1) + ExtendedGates.TXT_POSTFIX, true);
		}

		foreach (int i in Range(numericals))
		{
			alt[i] = new((i + 1) + ExtendedGates.ALT_POSTFIX, true);
		}


		for (int i = 0; i < specials.Length; i++)
		{
			LogInfo("ExtendedGates registering special " + specials[i].value + ExtendedGates.ALT_POSTFIX);
			alt[i + numericals] = new(specials[i].value + ExtendedGates.ALT_POSTFIX, true);
		}
	}
}
