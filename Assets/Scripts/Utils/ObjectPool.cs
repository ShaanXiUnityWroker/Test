using UnityEngine;
using System.Collections.Generic;


namespace Utils
{
    public class ObjectPool : MonoSingleton<ObjectPool>
    {
        #region Data Members
        // ---------------------------- //
        // Const data Members
        // ---------------------------- //


        // ---------------------------- //
        // Public data Members
        // ---------------------------- //


        // ---------------------------- //
        // Private data Members
        // ---------------------------- //
        Dictionary<Component, GameObject> mPrefabLookup = new Dictionary<Component,GameObject>();
        Dictionary<GameObject, List<Component>> mObjectLookup = new Dictionary<GameObject,List<Component>>();
        #endregion


        #region Function Members
        // ---------------------------- //
        // Use this for initialization
        // ---------------------------- //
        void Start()
        {

        }


        // ---------------------------- //
        // Update is called once per frame
        // ---------------------------- //
        void Update()
        {

        }


        // ---------------------------- //
        // Spawn object with specified type
        // ---------------------------- //
        public T Spawn<T>(GameObject prefab, Vector3 position, Vector3 eularAngles) where T : Component
        {
            // ---------------------------- //
            // Find game object in pool
            // ---------------------------- //
            T createdObject = null;
            createdObject = GetObjectInPool<T>(prefab);
            if (createdObject == null) {
                createdObject = CreateGameObject<T>(prefab);
            }

            createdObject.transform.position = position;
            createdObject.transform.eulerAngles = eularAngles;

            return createdObject;
        }

        public T Spawn<T>(GameObject prefab) where T : Component
        {
            return Spawn<T>(prefab, Vector3.zero, Vector3.zero);
        }

        public T Spawn<T>(GameObject prefab, Vector3 position) where T : Component
        {
            return Spawn<T>(prefab, position, Vector3.zero);
        }


        // ---------------------------- //
        // Recycle specified component
        // ---------------------------- //
        public void Recycle<T>(T obj) where T : Component
        {
            if (mPrefabLookup.ContainsKey(obj)) {
                obj.gameObject.SetActive(false);
                obj.transform.parent = transform;
                GameObject prefab = mPrefabLookup[obj];
                mObjectLookup[prefab].Add(obj);
            } else {
                Debug.LogWarning("This object is not spawned by ObjectPool, destroy immediately");
                Destroy(obj.gameObject);
            }
        }


        // ---------------------------- //
        // Clear all game objects
        // ---------------------------- //
        public void Clear()
        {
            foreach (KeyValuePair<GameObject, List<Component>> keyValue in mObjectLookup) {
                foreach (Component obj in keyValue.Value) {
                    Destroy(obj.gameObject);
                }
            }
            mPrefabLookup.Clear();
            mObjectLookup.Clear();
        }


        // ---------------------------- //
        // Get object in pool with specified type
        // ---------------------------- //
        T GetObjectInPool<T>(GameObject prefab) where T : Component
        {
            T obj = null;
            if (mObjectLookup.ContainsKey(prefab)) {
                List<Component> objs = mObjectLookup[prefab];
                if (objs.Count > 0) {
                    obj = objs[0] as T;
                    if (obj != null) {
                        obj.gameObject.SetActive(true);
                        obj.transform.parent = null;
                        objs.RemoveAt(0);
                    } else {
                        Debug.LogError("Object in pool type error, object in pool : " + objs[0]);
                    }
                }
            }

            return obj;
        }


        // ---------------------------- //
        // Create new GameObject
        // ---------------------------- //
        T CreateGameObject<T>(GameObject prefab) where T : Component
        {
            T obj = null; 
            GameObject go = Object.Instantiate(prefab) as GameObject;
            if (go != null) {
                obj = go.GetComponent<T>();
                if (obj != null) {
                    mPrefabLookup.Add(obj, prefab);
                    if (!mObjectLookup.ContainsKey(prefab)) {
                        mObjectLookup.Add(prefab, new List<Component>());
                    }
                } else {
                    Debug.LogError("Prefab has no " + typeof(T).Name + "!");
                }
            } else {
                Debug.LogError("Create GameObject failed, prefab : " + prefab.name);
            }

            return obj;
        }
        #endregion
    }
}
