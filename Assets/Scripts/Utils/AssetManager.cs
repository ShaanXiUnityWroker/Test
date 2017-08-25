using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;


namespace Utils
{
    public delegate void LoadCompleteCallback(Asset asset);

    public class AssetManager : MonoSingleton<AssetManager>
    {
        #region Data Members
        /// <summary>
        /// Const data Members
        /// </summary>
        const string ASSET_MANIFEST_FILE = "Asset Manifest.json";


        /// <summary>
        /// Private data Members
        /// </summary>
        AssetManifest mAssetManifest;                   // Asset manifest
        Dictionary<string, Asset>mLoadedAssets;         // Assets which has been loaded
        Dictionary<string, AssetHeader> mLocalAssets;   // Asset lookup

        public static string assetPath { get; private set; }
        #endregion



        #region Function Members
        /// <summary>
        /// Use this to initialize
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // If Android manifest has the WRITE_EXTERNAL_STORAGE permission
            // then the PersistentDataPath will point to a location
            // on the SD card instead of internal storage
            // If Delete file on iOS : 
            // System.IO.File.Delete("/private" + Application.persistentDataPath + "/" + filename);
            assetPath =
#if UNITY_EDITOR
                Application.dataPath + "/../Downloaded/";
#else
                Application.persistentDataPath + "/";
#endif
            mLoadedAssets = new Dictionary<string, Asset>();
            mLocalAssets = new Dictionary<string, AssetHeader>();
            LoadAssetManifest();
        }


        public IEnumerator LoadAsset(string url, LoadCompleteCallback callback)
        {
            if (mLoadedAssets.ContainsKey(url)) {
                callback(mLoadedAssets[url]);
                yield break;
            }

            // Load dependencies if exists
            AssetBundleCreateRequest asyncLoad = null;
            AssetHeader header = mLocalAssets[url];
            if (header.dependencies != null && header.dependencies.Length > 0) {
                foreach (var depUrl in header.dependencies) {
                    if (!mLoadedAssets.ContainsKey(depUrl)) {
                        asyncLoad = AssetBundle.LoadFromFileAsync(GetFullPath(depUrl));
                        yield return asyncLoad;
                        mLoadedAssets.Add(depUrl, new Asset(mLocalAssets[depUrl], asyncLoad.assetBundle));
                    }
                }
            }

            // Load main asset bundle
            asyncLoad = AssetBundle.LoadFromFileAsync(GetFullPath(url));
            Asset asset = new Asset(mLocalAssets[url], asyncLoad.assetBundle);
            mLoadedAssets.Add(url, asset);
            yield return asyncLoad;

            if (callback != null) {
                callback(asset);
            }
        }

        public IEnumerator DownloadAsset(string url, LoadCompleteCallback callback)
        {
            if (mLoadedAssets.ContainsKey(url)) {
                callback(mLoadedAssets[url]);
                yield break;
            }
            
            // Download header
            Asset asset = new Asset();
            yield return StartCoroutine(DownloadAssetHeader(url, asset));
            if (asset.header == null) {
                callback(null);
                yield break;
            }

            // Download all dependency
            Asset dependentAsset = new Asset();
            if (asset.header.dependencies != null && asset.header.dependencies.Length > 0) {
                foreach (var dependency in asset.header.dependencies) {
                    yield return StartCoroutine(DownloadAssetHeader(dependency, dependentAsset));
                    if (dependentAsset.header == null) {
                        callback(null);
                        yield break;
                    }

                    yield return StartCoroutine(DownloadAssetBundle(dependency, dependentAsset));
                    if (dependentAsset.bundle == null) {
                        callback(null);
                        yield break;
                    }
                }
            }

            // Download main asset
            yield return StartCoroutine(DownloadAssetBundle(url, asset));
            if (asset.bundle == null) {
                callback(null);
                yield break;
            }

            SaveAssetManifest();
            yield return null;

            if (callback != null)
                callback(asset);
        }


        /// <summary>
        /// Contains specified asset or not
        /// </summary>
        /// <param name="url">URL of the asset</param>
        /// <returns></returns>
        public bool ContainsAsset(string url, float version)
        {
            bool result = false;
            if (mLocalAssets.ContainsKey(url)) {
                result = mLocalAssets[url].version == version;
            }

            return result;
        }


        /// <summary>
        /// Remove asset from local storage
        /// </summary>
        /// <param name="url">Url of the asset</param>
        public void RemoveAsset(string url)
        {
            if (mLocalAssets.ContainsKey(url)) {
                // Delete file from local storage
                AssetHeader assetToRemove = mLocalAssets[url];
                assetToRemove.DeleteAsset();

                // Update lookup and manifest
                mLocalAssets.Remove(url);
                mAssetManifest.assetHeaders.Remove(assetToRemove);
                SaveAssetManifest();
            }
        }


        /// <summary>
        /// Clean up loaded assets
        /// </summary>
        public void Cleanup()
        {
            foreach(var keyValue in mLoadedAssets) {
                keyValue.Value.Unload(false);
            }
            mLoadedAssets.Clear();

            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }


        /// <summary>
        /// Get relative path of input url
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Relative path of the url</returns>
        public static string GetRelativePath(string url)
        {
            return new System.Uri(url).AbsolutePath;
        }

        public static string GetFullPath(string url)
        {
            return assetPath + GetRelativePath(url);
        }


        /// <summary>
        /// Start coroutine to load specified asset
        /// </summary>
        /// <param name="url">URL of the asset</param>
        /// <param name="version">Asset version</param>
        /// <param name="callback">Load complete callback method</param>
        public void LoadAssetAsync(string url, float version, LoadCompleteCallback callback)
        {
            if (mLocalAssets.ContainsKey(url)) {
                if (mLocalAssets[url].version == version) {
                    if (mLoadedAssets.ContainsKey(url)) {
                        callback(mLoadedAssets[url]);
                    } else {
                        StartCoroutine(LoadAsset(url, callback));
                    }
                    return;
                }
            } else {
                callback(null);
            }

            StartCoroutine(DownloadAsset(url, callback));
        }


        public static bool Save(string url, byte[] content)
        {
            bool result = true;

            try {
                string relativePath  = GetRelativePath(url);
                string filePath = assetPath + Path.GetDirectoryName(relativePath);
                string filePathName = assetPath + relativePath;

                // Create directory if not exist
                if (!Directory.Exists(filePath)) {
                    Directory.CreateDirectory(filePath);
                }

                File.WriteAllBytes(filePathName, content);
            } catch (System.Exception e) {
                Debug.LogError("Save file failed : " + e.Message);
                result = false;
            }

            return result;
        }


        public static string Load(string url)
        {
            string content = null;
            string path = GetFullPath(url);
            try {
                if (File.Exists(path)) {
                    content = File.ReadAllText(path);
                }
            } catch (System.Exception e) {
                Debug.LogError("Load scene profile failed : " + e.Message + "\nFile : " + path);
            }

            return content;
        }


        void LoadAssetManifest()
        {
            string path = assetPath + ASSET_MANIFEST_FILE;
            if (File.Exists(path)) {
                string json = File.ReadAllText(assetPath + ASSET_MANIFEST_FILE);
                mAssetManifest = JsonUtility.FromJson<AssetManifest>(json);
                if (mAssetManifest != null && mAssetManifest.assetHeaders != null && mAssetManifest.assetHeaders.Count > 0) {
                    foreach (var header in mAssetManifest.assetHeaders) {
                        header.Init();
                        if (mLocalAssets.ContainsKey(header.url)) {
                            mLocalAssets[header.url] = header;
                        } else {
                            mLocalAssets.Add(header.url, header);
                        }
                    }
                }
            } else {
                mAssetManifest = new AssetManifest();
                Debug.LogWarning("Manifest file doesn't exist!");
            }
        }


        void SaveAssetManifest()
        {
            string path = assetPath + ASSET_MANIFEST_FILE;
            try {
                File.WriteAllText(path, JsonUtility.ToJson(mAssetManifest));
            } catch (System.Exception e) {
                Debug.LogError("Write file failed : " + e.Message);
            }
        }

        IEnumerator DownloadAssetHeader(string url, Asset asset)
        {
            if (mLocalAssets.ContainsKey(url)) {
                asset.header = mLocalAssets[url];
                yield break;
            }

            using (WWW www = new WWW(url + AssetHeader.SUFFIX)) {
                yield return www;
                if (string.IsNullOrEmpty(www.error)) {
                    asset.header = JsonUtility.FromJson<AssetHeader>(www.text);
                    
                    // Update lookup
                    if (mLocalAssets.ContainsKey(url)) {
                        mLocalAssets[url] = asset.header;
                    } else {
                        mLocalAssets.Add(url, asset.header);
                    }
                } else {
                    Debug.LogError("Header download failed : " + www.error);
                    yield break;
                }
            }
        }

        IEnumerator DownloadAssetBundle(string url, Asset asset)
        {
            if (mLoadedAssets.ContainsKey(url)) {
                asset.bundle = mLoadedAssets[url].bundle;
                yield break;
            }

            using (WWW www = new WWW(url)) {
                yield return www;
                if (string.IsNullOrEmpty(www.error)) {
                    Save(url, www.bytes);
                    asset.bundle = www.assetBundle;
                    
                    // Input asset is used as an output param
                    // So it's safer to create new instance and add it into lookup
                    mLoadedAssets.Add(url, new Asset(asset.header, asset.bundle));
                    mAssetManifest.assetHeaders.Add(asset.header);
                } else {
                    Debug.LogError("Asset download failed : " + www.error);
                    yield break;
                }
            }
        }
        #endregion



        #region Inner Class
        [System.Serializable]
        public class AssetManifest
        {
            public List<AssetHeader> assetHeaders;

            public AssetManifest()
            {
                assetHeaders = new List<AssetHeader>();
            }
        }
        #endregion
    }
}
