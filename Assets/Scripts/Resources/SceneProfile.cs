using UnityEngine;
using System.IO;
using Utils;


namespace Assets.Scripts.Resources
{
    [System.Serializable]
    public class SceneProfile
    {
        public const string PATH    = "";
        public const string SUFFIX  = "sceneprofile";
        public const string PATTERN = "*." + SUFFIX;

        public float         version;
        public string        url;
        public AssetHeader[] assetHeaders;


        public static SceneProfile[] LoadAll()
        {
            SceneProfile[] profiles = null;

            try {
                // Found all SceneProfile json files
                string filePath = AssetManager.assetPath + PATH;
                string[] fileNames = Directory.GetFiles(filePath, PATTERN);

                if (fileNames != null && fileNames.Length > 0) {
                    // Deserialize json files
                    profiles = new SceneProfile[fileNames.Length];
                    for (int i = 0; i < profiles.Length; i++) {
                        string json = File.ReadAllText(fileNames[i]);
                        profiles[i] = JsonUtility.FromJson<SceneProfile>(json);
                    }
                }
            } catch (System.Exception e) {
                profiles = null;
                Debug.LogError("Load scene profiles failed : " + e.Message);
            }

            return profiles;
        }

        public static SceneProfile Load(string url)
        {
            return JsonUtility.FromJson<SceneProfile>(AssetManager.Load(url));
        }

        public static void Delete(string url)
        {
            string path = AssetManager.GetFullPath(url);
            try {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            } catch (System.Exception e) {
                Debug.LogError("Delete scene profile failed : " + e.Message + "\nFile : " + path);
            }
        }
    }
}
