using UnityEngine;
using Utils;
using System.Collections;
using Assets.Scripts.Resources;

namespace Assets.Scripts
{
    public class Test : MonoBehaviour
    {
        #region Data Members
        /// <summary>
        /// Const data Members
        /// </summary>


        /// <summary>
        /// Public data Members
        /// </summary>


        /// <summary>
        /// Private data Members
        /// </summary>

        #endregion


        #region Function Members
        /// <summary>
        /// Use this for initialization
        /// </summary>
        IEnumerator Start()
        {
            // Load cube with dependencies
            // yield return StartCoroutine(AssetManager.instance.DownloadAsset("http://127.0.0.1/Builds/Android/test/prefabs/cube", LoadAndInstantiate));

            while (SceneLoader.instance.state != SceneLoader.State.Ready) {
                yield return null;
            }
            print("Start to load scene");
            SceneLoader.instance.LoadSceneAsync("http://127.0.0.1/test.sceneprofile", LoadSceneComplete);
        }


        /// <summary>
        /// Update is called once per frame
        /// </summary>
        void Update()
        {

        }

        void LoadSceneComplete(string msg)
        {
            print("Load compelete");
            if (msg != null) {
                print(msg);
            }
        }

        void LoadAsset(Asset asset)
        {
            asset.bundle.GetMainAsset();
        }


        void LoadAndInstantiate(Asset asset)
        {
            Instantiate(asset.bundle.GetMainAsset());
        }

        IEnumerator TestNestedCoroutine()
        {
            yield return new WaitForSeconds(1);
            print("Test");
        }
        #endregion
    }
}
