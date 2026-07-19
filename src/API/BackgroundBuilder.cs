using RegionKit.Modules.BackgroundBuilder;

namespace RegionKit.API;

public static class BackgroundBuilder
{

	public static void RegisterElementType<T>(string name, params Type[] types) where T : BackgroundElementData.CustomBgElement, new()
	{
		Registry.RegisterElementType<T>(name, types);
	}
	public static void RegisterElementType<T>(string alias, string name, params Type[] types) where T : BackgroundElementData.CustomBgElement, new()
	{
		Registry.RegisterElementType<T>(alias, name, types);
	}

	public static void RegisterSceneType<T>(BackgroundTemplateType name, Registry.SceneMaker makeScene) where T : Data.BGSceneData, new()
	{
		Registry.RegisterSceneType<T>(name, makeScene);
	}
}
