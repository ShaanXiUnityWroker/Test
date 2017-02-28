using UnityEngine;


namespace Utils
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {

        #region Data Members
        // ---------------------------- //
        // Public data Members
        // ---------------------------- //
        public static T instance
        {
            get
            {
                if (sInstance == null) {
                    sInstance = (T)GameObject.FindObjectOfType(typeof(T)) as T;
                    if (sInstance == null) {
                        sInstance = new GameObject(typeof(T).Name).AddComponent<T>();
                    }
                    if (sInstance == null) {
                        Debug.LogError("Failed to create instance of " + typeof(T).FullName + ".");
                    }
                }

                return sInstance;
            }
        }


        // ---------------------------- //
        // Protected data Members
        // ---------------------------- //
        protected static T sInstance;
        #endregion



        #region Function Members
        protected virtual void Awake()
        {
            sInstance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        #endregion
    }
}