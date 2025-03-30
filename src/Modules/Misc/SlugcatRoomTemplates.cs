using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Misc;

internal static class SlugcatRoomTemplates
{
	public static void Apply()
	{
		On.Region.ctor += Region_ctor;
		try
		{
			IL.Region.ReloadRoomSettingsTemplate += Region_ReloadRoomSettingsTemplate;
			IL.RoomSettings.ctor += RoomSettings_ctor;
		}
		catch (Exception e) { LogError($"[SlugcatRoomTemplates] IL hooks failed!\n{e}"); }
	}
	public static void Undo()
	{
		On.Region.ctor -= Region_ctor;
		IL.Region.ReloadRoomSettingsTemplate -= Region_ReloadRoomSettingsTemplate;
		IL.RoomSettings.ctor -= RoomSettings_ctor;
	}

	private static void RoomSettings_ctor(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After,
			/*x => x.MatchLdarg(2),
			x => x.MatchLdfld<RoomSettings>(nameof(RoomSettings.name)),
			x => x.MatchStelemRef(),
			x => x.MatchDup(),
			x => x.MatchLdcI4(3),
			x => x.MatchLdsfld(nameof(Path), nameof(Path.DirectorySeparatorChar)),
			x => x.MatchStloc(0),
			x => x.MatchLdloca(0),*/ //I don't think Ldloca_S likes me... I'm sure the rest is fine
			x => x.MatchCall<char>(nameof(char.ToString)),
			x => x.MatchStelemRef(),
			x => x.MatchDup(),
			x => x.MatchLdcI4(4),
			x => x.MatchLdarg(1),
			x => x.MatchStelemRef(),
			x => x.MatchDup(),
			x => x.MatchLdcI4(5),
			x => x.MatchLdstr(".txt"),
			x => x.MatchStelemRef(),
			x => x.MatchCall<string>(nameof(string.Concat)),
			x => x.MatchCall<AssetManager>(nameof(AssetManager.ResolveFilePath)),
			x => x.MatchStfld<RoomSettings>(nameof(RoomSettings.filePath))
			))
		{
			c.Emit(OpCodes.Ldarg, 0);
			c.Emit(OpCodes.Ldarg, 2);
			c.Emit(OpCodes.Ldarg, 5);
			c.EmitDelegate((RoomSettings self, Region region, SlugcatStats.Name playerChar) =>
			{
				if (playerChar == null) return;
				string path = AssetManager.ResolveFilePath(
			$"World{Path.DirectorySeparatorChar}{region.name}{Path.DirectorySeparatorChar}{self.name}-{playerChar.value}.txt"
			);
				LogTrace($"path is [{path}, exists? {File.Exists(path)}]");
				if (File.Exists(path)) self.filePath = path;
			});
		}
	}

	private static void Region_ReloadRoomSettingsTemplate(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.Before,
			x => x.MatchLdnull(),
			x => x.MatchNewobj<RoomSettings>()
		))
		{
			c.EmitDelegate(() => staticName);
			c.Remove();
		}
	}

	private static void Region_ctor(On.Region.orig_ctor orig, Region self, string name, int firstRoomIndex, int regionNumber, SlugcatStats.Name storyIndex)
	{
		staticName = storyIndex;
		orig(self, name, firstRoomIndex, regionNumber, storyIndex);
		staticName = null!;
	}
	private static SlugcatStats.Name staticName = null!;
}
