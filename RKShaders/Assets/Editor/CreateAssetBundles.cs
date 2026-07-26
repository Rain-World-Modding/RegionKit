using UnityEditor;

public class ScriptThing
{
    [MenuItem("Assets/Build RK AssetBundles")]
    static void BuildAllAssetBundles()
    {
        const string BasePath = "../mod/assets/regionkit/";
        // Build the shaders
        _ = BuildPipeline.BuildAssetBundles(BasePath, BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows);
    }
}
