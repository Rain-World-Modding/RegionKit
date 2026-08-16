using System.IO;
using UnityEditor;

public class ScriptThing
{
    [MenuItem("Assets/Build RK AssetBundles")]
    static void BuildAllAssetBundles()
    {
        const string BasePath = "../mod/assets/regionkit/";

        // Build the shaders
        _ = BuildPipeline.BuildAssetBundles(BasePath, BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows);

        // Remove unnecessary files
        string allItemsPath = Path.Combine(BasePath, "regionkit");
        if (File.Exists(allItemsPath))
            File.Delete(allItemsPath);
        foreach (var item in Directory.EnumerateFiles(BasePath))
        {
            if (item.EndsWith(".manifest"))
            {
                File.Delete(item);
            }
        }
    }
}
