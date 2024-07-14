namespace RegionKit.Modules.Atmo.API;

internal static class Backing {
	#region fields
	internal static readonly Dictionary<string, V0_Create_RawHappenBuilder> __namedActions = new();
	internal static readonly Dictionary<string, V0_Create_RawTriggerFactory> __namedTriggers = new();
	internal static readonly Dictionary<string, V0_Create_RawMetaFunction> __namedMetafuncs = new();
	#endregion
	#region events
	public static event V0_Create_RawHappenBuilder? __EV_MakeNewHappen;
	public static event V0_Create_RawTriggerFactory? __EV_MakeNewTrigger;
	public static event V0_Create_RawMetaFunction? __EV_ApplyMetafunctions;

	internal static IEnumerable<V0_Create_RawMetaFunction?>? __AM_invl
		=> __EV_ApplyMetafunctions?.GetInvocationList()?.Cast<V0_Create_RawMetaFunction?>();
	internal static IEnumerable<V0_Create_RawTriggerFactory?>? __MNT_invl
		=> __EV_MakeNewTrigger?.GetInvocationList()?.Cast<V0_Create_RawTriggerFactory?>();
	internal static IEnumerable<V0_Create_RawHappenBuilder?>? __MNH_invl
		=> __EV_MakeNewHappen?.GetInvocationList().Cast<V0_Create_RawHappenBuilder?>();
	#endregion
}
