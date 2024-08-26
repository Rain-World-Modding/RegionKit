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


	private void InitCredits(ref int tabIndex)
	{
		AddTab(ref tabIndex, "Credits");


		AddTextLabel("REGIONKIT TEAM", bigText: true);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddAndDrawLargeDivider(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("DryCryCrystal", translate: false, color: hexToColor("e07ec8"));
		AddTextLabel("Thalber", translate: false, color: hexToColor("ffffff"));
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("DeltaTime", translate: false, color: hexToColor("ad1457"));
		AddTextLabel("M4rbleL1ne", translate: false, color: hexToColor("afff00"));
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("Bro", translate: false, color: hexToColor("ad1457"));
		AddTextLabel("Henpemaz", translate: false, color: hexToColor("0e7575"));
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("Thrithralas", translate: false, color: hexToColor("f1c40f"));
		AddTextLabel("Slime_Cubed", translate: false, color: hexToColor("25c059"));
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("LeeMoriya", translate: false, color: hexToColor("ffc900"));
		AddTextLabel("Bebe", translate: false, color: hexToColor("84c86b"));
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("Doggo", translate: false, color: hexToColor("c73633"));
		AddTextLabel("Kaeporo", translate: false, color: hexToColor("8de7f3"));
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);
		
		AddTextLabel("Dracentis", translate: false, color: hexToColor("ad1457"));
		AddTextLabel("Isbjorn52", translate: false, color: hexToColor("d97d3d"));
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("Xan", translate: false, color: hexToColor("b3443b"));
		AddTextLabel("ASlightlyOvergrownCactus", translate: false, color: hexToColor("339124"));
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("Vigaro", translate: false, color: hexToColor("eaba2a"));
		AddTextLabel("forthbridge", translate: false, color: hexToColor("8b41ff"));
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("Alduris", translate: false, color: hexToColor("fc770a"));
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		DrawBox(ref Tabs[tabIndex]);
	}
}
