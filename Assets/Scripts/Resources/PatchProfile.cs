using UnityEngine;

namespace Assets.Scripts.Resources
{
    [System.Serializable]
    public class PatchProfile
    {
        public bool     updateBinary;
        public float    version;
        public string   previousPatchProfileUrl;
        public string[] updatedSceneProfileUrls;
    }
}
