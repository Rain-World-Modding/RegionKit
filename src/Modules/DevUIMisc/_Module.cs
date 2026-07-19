using DevInterface;
using RegionKit.Modules.BackgroundBuilder;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace RegionKit.Modules.DevUIMisc;


[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "DevUI")]
public static class _Module
{
	internal static void Enable()
	{
		PaletteTextInput.Apply();
		ListFixes.Apply();
		FadePaletteTemplate.Apply();
		InsectPicker.Apply();
		PagedFadePalettes.Apply();

		//currently used for settings saving options stuffs, but will probably later be used for much more
		On.DevInterface.Page.ctor += Page_ctor;
		On.DevInterface.Page.Refresh += Page_Refresh;

		On.DevInterface.ObjectsPage.Signal += SubPage_Signal<On.DevInterface.ObjectsPage.orig_Signal, ObjectsPage>;
		On.DevInterface.RoomSettingsPage.Signal += SubPage_Signal<On.DevInterface.RoomSettingsPage.orig_Signal, RoomSettingsPage>;
		On.DevInterface.SoundPage.Signal += SubPage_Signal<On.DevInterface.SoundPage.orig_Signal, SoundPage>;
		On.DevInterface.TriggersPage.Signal += SubPage_Signal<On.DevInterface.TriggersPage.orig_Signal, TriggersPage>;

		// bugfix
		IL.DevInterface.DevUINode.Update += DevUINode_Update;
	}

	internal static void Disable()
	{
		PaletteTextInput.Undo();
		ListFixes.Undo();
		FadePaletteTemplate.Undo();
		InsectPicker.Undo();
		PagedFadePalettes.Undo();

		On.DevInterface.Page.ctor -= Page_ctor;
		On.DevInterface.Page.Refresh -= Page_Refresh;

		On.DevInterface.ObjectsPage.Signal -= SubPage_Signal<On.DevInterface.ObjectsPage.orig_Signal, ObjectsPage>;
		On.DevInterface.RoomSettingsPage.Signal -= SubPage_Signal<On.DevInterface.RoomSettingsPage.orig_Signal, RoomSettingsPage>;
		On.DevInterface.SoundPage.Signal -= SubPage_Signal<On.DevInterface.SoundPage.orig_Signal, SoundPage>;
		On.DevInterface.TriggersPage.Signal -= SubPage_Signal<On.DevInterface.TriggersPage.orig_Signal, TriggersPage>;

		IL.DevInterface.DevUINode.Update -= DevUINode_Update;
	}


	private static void Page_Refresh(On.DevInterface.Page.orig_Refresh orig, Page self)
	{
		//modSelectPanel = null;
		orig(self);
	}

	private static void SubPage_Signal<T, U>(T orig, U self, DevUISignalType type, DevUINode sender, string message)
		where T : Delegate
	{
		orig.DynamicInvoke(self, type, sender, message);
		if (self is Page page)
		{ 
			SettingsSaveOptions.SaveSignal(page, type, sender, message); 
		}
	}

	private static void Page_ctor(On.DevInterface.Page.orig_ctor orig, Page self, DevUI owner, string IDstring, DevUINode parentNode, string name)
	{
		orig(self, owner, IDstring, parentNode, name);

		//move pages over to avoid collision with save buttons
		//for BackgroundBuilder, currently unused
		foreach (DevUINode node in self.subNodes)
		{
			if (node is SwitchPageButton switchPageButton)
			{
				switchPageButton.pos.x -= 20f;
				switchPageButton.Refresh();
			}
		}
		if (self is MapPage or BackgroundPage)
		{ 
			SettingsSaveOptions.settingsSaveOptionsMenu = null; 
			return;
		}

		SettingsSaveOptions.settingsSaveOptionsMenu = new SettingsSaveOptions.SettingsSaveOptionsMenu(owner, "SettingsSaveOptions", self);
		self.subNodes.Add(SettingsSaveOptions.settingsSaveOptionsMenu);
	}

	private static void DevUINode_Update(ILContext il)
	{
		// Prevent index out of bounds exception if a subnode decides to kill its whole family (changes the length of the subnodes of its parent) mid-update
		var c = new ILCursor(il);

		c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<DevUINode>(nameof(DevUINode.Update)));
		Instruction brTo = c.Next;

		c.Goto(0);
		c.GotoNext(MoveType.After, x => x.MatchBr(out _));
		c.MoveAfterLabels();

		c.Emit(OpCodes.Ldarg_0);
		c.Emit(OpCodes.Ldloc_0); // I'm expecting this local variable index not to change in future updates
		c.EmitDelegate((DevUINode self, int i) => i >= self.subNodes.Count);
		c.Emit(OpCodes.Brtrue, brTo);
	}
}
