using System.IO;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Misc;

internal static class SlugcatRoomTemplates
{
	public static void Apply()
	{
		On.Region.ctor_string_int_int_RainWorldGame_Timeline += Region_ctor;
		try
		{
			IL.Region.ReloadRoomSettingsTemplate += Region_ReloadRoomSettingsTemplate;
			IL.RoomSettings.ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame += RoomSettings_ctor;
		}
		catch (Exception e) { LogError($"[SlugcatRoomTemplates] IL hooks failed!\n{e}"); }
	}
	public static void Undo()
	{
		On.Region.ctor_string_int_int_RainWorldGame_Timeline -= Region_ctor;
		IL.Region.ReloadRoomSettingsTemplate -= Region_ReloadRoomSettingsTemplate;
		IL.RoomSettings.ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame -= RoomSettings_ctor;
	}

	private static void RoomSettings_ctor(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(x => x.MatchLdstr("world"))
			&& c.TryGotoNext(MoveType.After, x => x.MatchStfld<RoomSettings>(nameof(RoomSettings.filePath)))
		)
		{
			c.Emit(OpCodes.Ldarg, 0);
			c.Emit(OpCodes.Ldarg, 3);
			c.Emit(OpCodes.Ldarg, 6);
			c.EmitDelegate((RoomSettings self, Region region, SlugcatStats.Timeline playerChar) =>
			{
				if (playerChar == null) return;
				string path = AssetManager.ResolveFilePath(Path.Combine("World", region.name, $"{self.name}-{playerChar.value}.txt"));
				LogTrace($"path is [{path}, exists? {File.Exists(path)}]");
				if (File.Exists(path)) self.filePath = path;
			});
		}
		else
		{
			LogError("SlugcatRoomTemplates.RoomSettings_ctor failed to apply!");
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
		else
		{
			LogError("SlugcatRoomTemplatets.Region_ReloadRoomSettingsTemplate failed to apply!");
		}
	}

	private static void Region_ctor(On.Region.orig_ctor_string_int_int_RainWorldGame_Timeline orig, Region self, string name, int firstRoomIndex, int regionNumber, RainWorldGame game, SlugcatStats.Timeline storyIndex)
	{
		staticName = storyIndex;
		orig(self, name, firstRoomIndex, regionNumber, game, storyIndex);
		staticName = null!;
	}
	private static SlugcatStats.Timeline staticName = null!;
}
