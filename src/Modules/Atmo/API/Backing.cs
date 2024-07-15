namespace RegionKit.Modules.Atmo.API;

internal static class Backing 
{
	#region fields
	internal static readonly Dictionary<string, Create_NamedHappenBuilder> __namedActions = new();
	internal static readonly Dictionary<string, Create_NamedTriggerFactory> __namedTriggers = new();
	internal static readonly Dictionary<string, Create_NamedMetaFunction> __namedMetafuncs = new();
	#endregion
}
