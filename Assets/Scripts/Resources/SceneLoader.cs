using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;



namespace Assets.Scripts.Resources
{
    public delegate void OnSceneLoaded(string error);


    public class SceneLoader : MonoSingleton<SceneLoader>
    {
        public enum State
        {
            Initiating,

            CheckingContentVersion,
            DownloadingPatchProfiles,
            DownloadingSceneProfiles,

            LoadingSceneProfiles,
            Ready,

            LoadingScene,
            DownloadingAssets,
            LoadingLocalAssets,
            BuildingHierarchy,

            DownloadError,
        }

        #region Data Members
        /// <summary>
        /// Const data Members
        /// </summary>
        public const string ASSET_SERVER        = "http://192.168.1.10/";
        public const string URL_LATEST_PATCH    = ASSET_SERVER + "latest.patch.json";
        public const string URL_CONTENT_VERSION = ASSET_SERVER + "content_version.txt";

        const string KEY_ASSET_VERSION = "ASSET_VERSION";


        /// <summary>
        /// Public data Members
        /// </summary>
        public State state { get; private set; }
        public float localContentVersion { get; private set; }
        public float latestContentVersion { get; private set; }


        /// <summary>
        /// Private data Members
        /// </summary>
        PatchProfile mDownloadedPatchProfile;
        Dictionary<string, Asset>        mSceneAssets;
        Dictionary<string, SceneProfile> mLocalProfiles;
        #endregion


        #region Function Members
        /// <summary>
        /// Use this for initialization
        /// </summary>
        IEnumerator Start()
        {
            state = State.Initiating;
            mSceneAssets = new Dictionary<string, Asset>();
            mLocalProfiles = new Dictionary<string, SceneProfile>();

            // Check latest version and download new resources
            yield return StartCoroutine(GetLatestVersion());
            if (latestContentVersion != localContentVersion) {
                yield return StartCoroutine(DownloadProfiles());
            }

            LoadLocalSceneProfiles();

            state = State.Ready;
        }


        /// <summary>
        /// Load scene of specified url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="onSceneLoaded"></param>
        public void LoadSceneAsync(string url, OnSceneLoaded onSceneLoaded)
        {
            if (state == State.Ready) {
                StartCoroutine(LoadScene(url, onSceneLoaded));
            } else {
                if (onSceneLoaded != null)
                    onSceneLoaded("Error : Scene state is " + state.ToString());
            }
        }


        IEnumerator GetLatestVersion()
        {
            state = State.CheckingContentVersion;
            localContentVersion = PlayerPrefs.GetFloat(KEY_ASSET_VERSION);
            using (WWW www = new WWW(URL_CONTENT_VERSION)) {
                yield return www;
                if (string.IsNullOrEmpty(www.error)) {
                    try {
                        latestContentVersion = float.Parse(www.text);
                    } catch (System.Exception e) {
                        latestContentVersion = 0;
                        Debug.LogError("Convert version number failed : " + e.Message);
                    }
                } else {
                    latestContentVersion = 0;
                    Debug.LogError("Get version number failed : " + www.error);
                }
            }
        }

        IEnumerator DownloadProfiles()
        {
            // Download patch profiles and record all scene profiles to be updated
            state = State.DownloadingPatchProfiles;
            List<string> sceneProfilesToBeUpdated = new List<string>();
            string patchUrl = URL_LATEST_PATCH;
            do {
                yield return StartCoroutine(DownloadPatchProfile(patchUrl));
                if (mDownloadedPatchProfile != null) {
                    // Add all updated scene profile url
                    foreach (var url in mDownloadedPatchProfile.updatedSceneProfileUrls) {
                        if (!sceneProfilesToBeUpdated.Contains(url)) {
                            sceneProfilesToBeUpdated.Add(url);
                        }
                    }
                } else {
                    state = State.DownloadError;
                    yield break;
                }
            } while (mDownloadedPatchProfile.version != localContentVersion);

            // Download scene profiles to be updated
            state = State.DownloadingSceneProfiles;
            foreach (var url in sceneProfilesToBeUpdated) {
                yield return StartCoroutine(DownloadSceneProfile(url));
            }
        }

        IEnumerator DownloadPatchProfile(string url)
        {
            using (WWW www = new WWW(url)) {
                yield return www;
                if (string.IsNullOrEmpty(www.error)) {
                    mDownloadedPatchProfile = JsonUtility.FromJson<PatchProfile>(www.text);
                } else {
                    mDownloadedPatchProfile = null;
                    Debug.LogError("Download patch profile failed : " + www.error);
                }
            }
        }

        IEnumerator DownloadSceneProfile(string url)
        {
            using (WWW www = new WWW(url)) {
                yield return www;
                if (string.IsNullOrEmpty(www.error)) {
                    AssetManager.Save(url, www.bytes);
                } else {
                    Debug.LogError("Download scene profile failed : " + www.error);
                }
            }
        }

        void LoadLocalSceneProfiles()
        {
            state = State.LoadingSceneProfiles;
            var profiles = SceneProfile.LoadAll();
            if (profiles != null) {
                // Add all profiles into lookup
                foreach (var profile in profiles) {
                    mLocalProfiles.Add(profile.url, profile);
                }
            }
        }


        IEnumerator LoadScene(string url, OnSceneLoaded onSceneLoaded)
        {
            state = State.LoadingScene;

            // Get scene profile
            SceneProfile sceneProfile = null;
            if (mLocalProfiles.ContainsKey(url)) {
                sceneProfile = mLocalProfiles[url];
            } else {
                yield return StartCoroutine(DownloadSceneProfile(url));
                sceneProfile = SceneProfile.Load(url);
            }

            // Get download list
            state = State.DownloadingAssets;
            List<string> downloadList = new List<string>();
            foreach (var header in sceneProfile.assetHeaders) {
                if (!AssetManager.instance.ContainsAsset(header.url, header.version)) {
                    if (!downloadList.Contains(header.url))
                        downloadList.Add(header.url);
                }
            }
            // Download all assets
            foreach (var assetUrl in downloadList) {
                yield return StartCoroutine(AssetManager.instance.DownloadAsset(assetUrl, DownloadCallback));
            }

            // Load all local assets
            state = State.LoadingLocalAssets;
            foreach(var header in sceneProfile.assetHeaders) {
                if (!downloadList.Contains(header.url) && !mSceneAssets.ContainsKey(header.url)) {
                    yield return StartCoroutine(AssetManager.instance.LoadAsset(header.url, LoadCallback));
                }
            }

            // Build hierarchy
            state = State.BuildingHierarchy;
            foreach (var keyValue in mSceneAssets) {
                Instantiate(keyValue.Value.bundle.GetMainAsset());
                yield return null;
            }

            AssetManager.instance.Cleanup();

            if (onSceneLoaded != null) onSceneLoaded(null);
        }


        void DownloadCallback(Asset asset)
        {
            if (!mSceneAssets.ContainsKey(asset.header.url)) {
                mSceneAssets.Add(asset.header.url, asset);
            }
        }

        void LoadCallback(Asset asset)
        {
            if (!mSceneAssets.ContainsKey(asset.header.url)) {
                mSceneAssets.Add(asset.header.url, asset);
            }
        }
        #endregion
    }
}
