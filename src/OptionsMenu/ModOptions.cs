using Menu.Remix.MixedUI;
using RegionKit.OptionsMenu;

namespace RegionKit;

public sealed class ModOptions : OptionsTemplate
{
    public static ModOptions Instance { get; } = new();

    public static void RegisterOI(string remixModId)
    {
		if (MachineConnector.GetRegisteredOI(remixModId) != Instance)
		{
            MachineConnector.SetRegisteredOI(remixModId, Instance);
		}
    }

    public static Color WarnRed { get; } = new(0.85f, 0.35f, 0.4f);

	

	// CONFIGURABLES

    public static Configurable<bool> EnableIggy { get; } = Instance.config.Bind(nameof(EnableIggy), true, new ConfigurableInfo(
        "When checked, Iggy will appear in the devtools menu to describe various elements of the UI.", null, "",
        "Enable Iggy?"));

	public static Configurable<bool> EnableRant { get; } = Instance.config.Bind(nameof(EnableRant), false, new ConfigurableInfo(
		"When checked, enables the rant that has a chance to be logged to the console on startup.", null, "",
		"Disable Rant?"));

	public static Configurable<bool> AltGateArt { get; } = Instance.config.Bind(nameof(AltGateArt), false, new ConfigurableInfo(
		"When checked, uses an alternative set of art for region gate glyphs.", null, "",
		"Alt Gate Art?"));



	// MENU

	public const int TAB_COUNT = 3;
	private const int TB_INDEX = 2;

    public override void Initialize()
    {
        base.Initialize();

        Tabs = new OpTab[TAB_COUNT];
        int tabIndex = -1;

        InitGeneral(ref tabIndex);
		InitCredits(ref tabIndex);
		Tabs[TB_INDEX] = new TurboBakerTab(this);
		(Tabs[TB_INDEX] as TurboBakerTab)!.Initialize();
	}

	public override void Update()
	{
		base.Update();
		(Tabs[TB_INDEX] as TurboBakerTab)!.Update();
	}


	private void InitGeneral(ref int tabIndex)
    {
        AddTab(ref tabIndex, "General");

        AddCheckBox(EnableIggy);
		AddCheckBox(AltGateArt);
		DrawCheckBoxes(ref Tabs[tabIndex]);

		AddCheckBox(EnableRant);
		DrawCheckBoxes(ref Tabs[tabIndex]);


		AddNewLine(15);

		DrawBox(ref Tabs[tabIndex]);
	}

	private static readonly List<(string name, Color color)> Credits =
	[
		("DryCryCrystal", hexToColor("e07ec8")),
		("Thalber", hexToColor("ffffff")),
		("DeltaTime", hexToColor("ad1457")),
		("M4rbleL1ne", hexToColor("afff00")),
		("Bro", hexToColor("ad1457")),
		("Henpemaz", hexToColor("0e7575")),
		("Thrithralas", hexToColor("f1c40f")),
		("Slime_Cubed", hexToColor("25c059")),
		("LeeMoriya", hexToColor("ffc900")),
		("Bebe", hexToColor("84c86b")),
		("Doggo", hexToColor("c73633")),
		("Kaeporo", hexToColor("8de7f3")),
		("Dracentis", hexToColor("ad1457")),
		("Isbjorn52", hexToColor("d97d3d")),
		("Xan", hexToColor("b3443b")),
		("ASlightlyOvergrownCactus", hexToColor("339124")),
		("Vigaro", hexToColor("eaba2a")),
		("forthfora", hexToColor("8b41ff")),
		("Alduris", hexToColor("fc770a")),
		("MagicaJaphet", hexToColor("c00a20")),
	];

	private void InitCredits(ref int tabIndex)
	{
		AddTab(ref tabIndex, "Credits");


		AddTextLabel("REGIONKIT TEAM", bigText: true);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddAndDrawLargeDivider(ref Tabs[tabIndex]);

		AddNewLine(1);

		const int COLS = 3;

		for (int i = 0; i < Credits.Count; i++)
		{
			if (i != 0 && i % COLS == 0)
			{
				DrawTextLabels(ref Tabs[tabIndex]);
				AddNewLine(1);
			}
			AddTextLabel(Credits[i].name, translate: false, color: Credits[i].color);
		}

		DrawTextLabels(ref Tabs[tabIndex]);
		AddNewLine(1);

		DrawBox(ref Tabs[tabIndex]);
	}
}
