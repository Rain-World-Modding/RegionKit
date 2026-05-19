
using static RegionKit.Modules.BackgroundBuilder.BackgroundElementData;
using Watcher;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class Registry
{

	public static Dictionary<Type, string> BackgroundTypeNames = new();

	public delegate BackgroundElementData.CustomBgElement ElementMaker();
	public static Dictionary<string, ElementMaker> ElementMakerRegistry = new();

	public static Dictionary<Type, string> SceneElementString = new();

	public static void RegisterElementType<T>(string name, params Type[] types) where T : BackgroundElementData.CustomBgElement, new()
	{
		ElementMakerRegistry[name] = () => new T();
		BackgroundTypeNames[typeof(T)] = name;
		foreach (Type t in types)
		{
			SceneElementString[t] = name;
		}
	}
	public static void RegisterElementType<T>(string alias, string name, params Type[] types) where T : BackgroundElementData.CustomBgElement, new()
	{
		ElementMakerRegistry[alias + name] = () => new T();
		BackgroundTypeNames[typeof(T)] = name;
		foreach (Type t in types)
		{
			SceneElementString[t] = name;
		}
	}


	public delegate Data.BGSceneData MakeSceneData();
	public static Dictionary<BackgroundTemplateType, MakeSceneData> SceneDataMakerRegistry = new();
	public delegate BackgroundScene SceneMaker(Room room);
	public static Dictionary<BackgroundTemplateType, SceneMaker> SceneMakerRegistry = new();

	public static void RegisterSceneType<T>(BackgroundTemplateType name, SceneMaker makeScene) where T : Data.BGSceneData, new()
	{
		SceneDataMakerRegistry[name] = () => new T();
		SceneMakerRegistry[name] = makeScene;
	}



	internal static void InitElementFactoryRegistry()
	{
		RegisterElementType<BG_ElementGroup>("GROUP");
		RegisterElementType<BG_ElementGroup>("Group");
		RegisterElementType<BG_SimpleElement>("SimpleElement", typeof(CustomBackgroundElements.SimpleBackgroundElement));
		RegisterElementType<BG_Illustration>("SimpleIllustration", typeof(BackgroundScene.Simple2DBackgroundIllustration), typeof(BackgroundScene.AdditiveBackgroundIllustration), typeof(AncientUrbanView.Sky));
		RegisterElementType<ACV_DistantBuilding>("DistantBuilding", typeof(AboveCloudsView.DistantBuilding));
		RegisterElementType<ACV_DistantLightning>("DistantLightning", typeof(AboveCloudsView.DistantLightning));
		RegisterElementType<ACV_FlyingCloud>("FlyingCloud", typeof(AboveCloudsView.FlyingCloud));
		RegisterElementType<ACV_HorizonFog>("HorizonFog", typeof(AboveCloudsView.HorizonFog));
		RegisterElementType<RTV_DistantBuilding>("RF_", "DistantBuilding", typeof(RoofTopView.DistantBuilding));
		RegisterElementType<RTV_Building>("Building", typeof(RoofTopView.Building));
		RegisterElementType<RTV_Floor>("Floor", typeof(RoofTopView.Floor));
		RegisterElementType<RTV_DistantGhost>("DistantGhost", typeof(RoofTopView.DistantGhost));
		RegisterElementType<RTV_DustWave>("DustWave", typeof(RoofTopView.DustWave));
		RegisterElementType<RTV_Smoke>("Smoke", typeof(RoofTopView.Smoke));
		RegisterElementType<RTV_Smoke>("AU_", "Smoke", typeof(AncientUrbanView.Smoke));
		RegisterElementType<AUV_Building>("AU_", "Building", typeof(AncientUrbanView.Building));
		RegisterElementType<AUV_SmokeGradient>("SmokeGradient", typeof(AncientUrbanView.SmokeGradient));
		RegisterElementType<RWS_PebbsGrid>("PebbsGrid", typeof(RotWormScene.PebbsGrid));
		RegisterElementType<RWS_RotWorm>("RotWorm", typeof(RotWorm));
	}



	internal static void InitSceneMakerRegistry()
	{
		RegisterSceneType<Data.AboveCloudsView_SceneData>(BackgroundTemplateType.AboveCloudsView, (room) => new AboveCloudsView(room, new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.AboveCloudsView, 0f, false)));
		RegisterSceneType<Data.RoofTopView_SceneData>(BackgroundTemplateType.RoofTopView, (room) => new RoofTopView(room, new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.RoofTopView, 0f, false)));
		RegisterSceneType<Data.AncientUrbanView_SceneData>(BackgroundTemplateType.AncientUrbanView, (room) => new AncientUrbanView(room, new RoomSettings.RoomEffect(WatcherEnums.RoomEffectType.AncientUrbanView, 0f, false)));
		RegisterSceneType<Data.RotWormScene_SceneData>(BackgroundTemplateType.RotWormScene, (room) => new RotWormScene(room));
		RegisterSceneType<Data.BGSceneData>(BackgroundTemplateType.VoidSeaScene, (room) => new VoidSea.VoidSeaScene(room));
		RegisterSceneType<Data.BGSceneData>(BackgroundTemplateType.None, (room) => new BackgroundScene(room));
	}
}
