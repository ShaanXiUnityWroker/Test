using System;
using UnityEngine;


namespace Utils
{
    public class Asset : IDisposable
    {
        #region Data Members
        /// <summary>
        /// Const data Members
        /// </summary>


        /// <summary>
        /// Public data Members
        /// </summary>
        public AssetHeader header { get; internal set; }
        public AssetBundle bundle { get; internal set; }


        /// <summary>
        /// Private data Members
        /// </summary>

        #endregion


        #region Function Members
        /// <summary>
        /// Standard constructor
        /// </summary>
        internal Asset()
        {

        }

        public Asset(AssetHeader header, AssetBundle bundle)
        {
            this.header = header;
            this.bundle = bundle;
        }


        /// <summary>
        /// Release all memory
        /// </summary>
        public void Unload(bool unloadAllLoadedObjects)
        {
            bundle.Unload(unloadAllLoadedObjects);
        }


        /// <summary>
        /// Release compressed data
        /// </summary>
        public void Dispose()
        {
            bundle.Unload(false);
        }
        #endregion
    }


    // Asset bundle extention
    public static class AssetBundleExtention
    {
        public static UnityEngine.Object GetMainAsset(this AssetBundle asset)
        {
            UnityEngine.Object result = null;
            if (asset.GetAllAssetNames().Length > 0) {
                result = asset.LoadAsset(asset.GetAllAssetNames()[0]);
            }

            return result;
        }
    }
}
