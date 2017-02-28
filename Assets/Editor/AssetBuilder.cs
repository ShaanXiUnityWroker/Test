using System.IO;
using UnityEngine;
using UnityEditor;
using Utils;
using Assets.Scripts.Resources;


namespace Kamikami.EditorTools
{
    public class AssetBuilder
    {
        const string OUTPUT_PATH    = "Build/";
        const string PATH_4_IOS     = OUTPUT_PATH + "Android/";
        const string PATH_4_ANDROID = OUTPUT_PATH + "iOS/";

        [MenuItem("Assets/Build All Asset Bundles")]
        static void BuildAll()
        {
            if (!Directory.Exists(PATH_4_IOS)) {
                Directory.CreateDirectory(PATH_4_IOS);
            }            
            var manifest = BuildPipeline.BuildAssetBundles(PATH_4_IOS, BuildAssetBundleOptions.None, BuildTarget.iOS);
            CreateAssetHeaders(manifest, PATH_4_IOS);

            if (!Directory.Exists(PATH_4_ANDROID)) {
                Directory.CreateDirectory(PATH_4_ANDROID);
            }
            manifest = BuildPipeline.BuildAssetBundles(PATH_4_ANDROID, BuildAssetBundleOptions.None, BuildTarget.Android);
            CreateAssetHeaders(manifest, PATH_4_ANDROID);
        }

        static void CreateAssetHeaders(AssetBundleManifest manifest, string path)
        {
            AssetHeader header = null;
            string[] dependencies = null;
            string urlPath = SceneLoader.ASSET_SERVER + path;
            foreach (var assetBundleName in manifest.GetAllAssetBundles()) {
                // Setup header info
                header = new AssetHeader(urlPath + assetBundleName, 0);
                header.size = new FileInfo(path + assetBundleName).Length;
                dependencies = manifest.GetAllDependencies(assetBundleName);
                header.dependencies = new string[dependencies.Length];
                for (int i = 0; i < dependencies.Length; i++) {
                    header.dependencies[i] = urlPath + dependencies[i];
                }

                // Save into file
                string fileName = path + assetBundleName + AssetHeader.SUFFIX;
                File.WriteAllText(fileName, JsonUtility.ToJson(header));
            }
        }
    }
}
