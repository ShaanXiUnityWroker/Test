using System.IO;
using UnityEngine;


namespace Utils
{
    [System.Serializable]
    public class AssetHeader
    {
        #region Data Members
        /// <summary>
        /// Const data Members
        /// </summary>
        public const string SUFFIX = ".header";


        /// <summary>
        /// Public data Members
        /// </summary>
        public long     size;       // In bytes
        public float    version;
        public string   url;
        public string   path;
        public string[] dependencies;


        /// <summary>
        /// Private data Members
        /// </summary>

        #endregion


        #region Function Members
        /// <summary>
        /// Standard Constructor
        /// </summary>
        public AssetHeader(string url, float version)
        {
            this.url     = url;
            this.version = version;
            Init();
        }


        /// <summary>
        /// Init this header
        /// </summary>
        public void Init()
        {
            path = AssetManager.GetFullPath(url);
        }

        public void DeleteAsset()
        {
            File.Delete(path);
        }


        public string GetText()
        {
            string result = null;
            if (File.Exists(path)) {
                result = File.ReadAllText(path);
            } else {
                Debug.LogError("File : " + path + " doesn't exist.");
            }

            return result;
        }
        #endregion
    }
}
