using Menu.Remix.MixedUI;

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

	public static Configurable<bool> DisableRant { get; } = Instance.config.Bind(nameof(DisableRant), false, new ConfigurableInfo(
		"When checked, disables the rant that has a chance to be logged to the console on startup.", null, "",
		"Disable Rant?"));

	public static Configurable<bool> AltGateArt { get; } = Instance.config.Bind(nameof(AltGateArt), false, new ConfigurableInfo(
		"When checked, uses an alternative set of art for region gate glyphs.", null, "",
		"Alt Gate Art?"));

	public static Configurable<bool> LogLevels { get; } = Instance.config.Bind(nameof(LogLevels), false, new ConfigurableInfo(
		"When checked, ...", null, "",
		"Log Levels?"));


	// MENU

	public const int TAB_COUNT = 2;

    public override void Initialize()
    {
        base.Initialize();

        Tabs = new OpTab[TAB_COUNT];
        int tabIndex = -1;

        InitGeneral(ref tabIndex);
		InitCredits(ref tabIndex);
	}


	private void InitGeneral(ref int tabIndex)
    {
        AddTab(ref tabIndex, "General");

        AddCheckBox(EnableIggy);
		AddCheckBox(AltGateArt);
		DrawCheckBoxes(ref Tabs[tabIndex]);

		AddCheckBox(LogLevels);
		AddCheckBox(DisableRant);
		DrawCheckBoxes(ref Tabs[tabIndex]);


		AddNewLine(15);

		DrawBox(ref Tabs[tabIndex]);
	}


	private void InitCredits(ref int tabIndex)
	{
		AddTab(ref tabIndex, "Credits");


		AddTextLabel("CREDITS", bigText: true);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddAndDrawLargeDivider(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("DryCryCrystal", translate: false);
		AddTextLabel("Thalber", translate: false);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("DeltaTime", translate: false);
		AddTextLabel("M4rbleL1ne", translate: false);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("Bro", translate: false);
		AddTextLabel("Henpemaz", translate: false);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("Thrithralas", translate: false);
		AddTextLabel("Slime_Cubed", translate: false);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("LeeMoriya", translate: false);
		AddTextLabel("Bebe", translate: false);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("Doggo", translate: false);
		AddTextLabel("Kaeporo", translate: false);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);
		
		AddTextLabel("Dracentis", translate: false);
		AddTextLabel("Isbjorn52", translate: false);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("Xan", translate: false);
		AddTextLabel("ASlightlyOvergrownCactus", translate: false);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		AddTextLabel("forthbridge", translate: false);
		AddTextLabel("", translate: false);
		DrawTextLabels(ref Tabs[tabIndex]);

		AddNewLine(1);

		DrawBox(ref Tabs[tabIndex]);
	}
}
