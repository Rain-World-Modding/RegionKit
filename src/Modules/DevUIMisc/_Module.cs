using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;

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
		SettingsSaveOptions.Apply();
		DecalPreview.Enable();
		DecalSelectSearch.Apply();
		SoundPageSearch.Apply();
		AntiPanelCollapse.Apply();
		SelectSongPanelOverhaul.Apply();

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
		SettingsSaveOptions.Undo();
		DecalPreview.Disable();
		DecalSelectSearch.Undo();
		SoundPageSearch.Undo();
		AntiPanelCollapse.Undo();
		SelectSongPanelOverhaul.Undo();

		IL.DevInterface.DevUINode.Update -= DevUINode_Update;
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
