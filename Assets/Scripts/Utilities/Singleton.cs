using Unity.VisualScripting;
using UnityEngine;

namespace Kudoshi.Utilities
{
    /// <summary>
    /// Be aware this will not prevent a non singleton constructor
    ///   such as `T myT = new T();`
    /// To prevent that, add `protected T () {}` to your singleton class.
    /// 
    /// Changes:
    /// - 27/10/2025 : Removed auto creation of singleton and destroy checking. To prevent null when 
    /// </summary>
    /// 
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;

        private static object _lock = new object();

        public static T Instance
        {
            get
            {
                //if (applicationIsQuitting)
                //{
                //    Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                //        "' already destroyed on application quit." +
                //        " Won't create again - returning null.");
                //    return null;
                //}

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        Object[] instances = FindObjectsByType(typeof(T), FindObjectsSortMode.None);
                        
                        if (instances.Length > 0)
                        {
                            _instance = (T) instances[0];
                        }

                        if (instances.Length > 1)
                        {
                            Debug.LogError("[Singleton] Something went really wrong " +
                                " - there should never be more than 1 singleton!" +
                                " Reopening the scene might fix it.");

                            for (int i = 0; i < instances.Length; i++)
                            {
                                if (instances[i] != _instance)
                                {
                                    Destroy(instances[i]);
                                }
                            }
                        }

                        //if (_instance == null)
                        //{
                        //    GameObject singleton = new GameObject();
                        //    _instance = singleton.AddComponent<T>();
                        //    singleton.name = "(singleton) " + typeof(T).ToString();

                        //    Debug.Log("[Singleton] An instance of " + typeof(T) +
                        //        " is needed in the scene, so '" + singleton +
                        //        "' was created.");
                        //}
                        //else
                        //{
                        //    //Debug.Log("[Singleton] Using instance already created: " + _instance.gameObject.name);
                        //}
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Some magic fix due to a weird bug where ondisabled() scripts checks for null for instance but ends up creating a new instance
        /// </summary>
        //public static bool InstanceAlive()
        //{
        //    return _instance != null;
        //}

        //private static bool IsDontDestroyOnLoad()
        //{
        //    if (_instance == null)
        //    {
        //        return false;
        //    }

        //    // Object exists independent of Scene lifecycle, assume that means it has DontDestroyOnLoad set
        //    // - Kudo: I dont think below actually fixed it. Have yet to test in original game to see
        //    //if ((_instance.gameObject.hideFlags & HideFlags.DontSave) == HideFlags.DontSave)
        //    //{
        //    //    return true;
        //    //}

        //    if (_instance.gameObject.scene.name == "DontDestroyOnLoad")
        //        return true;
        //    else
        //        return false;
        //}

        //private static bool applicationIsQuitting = false;
        ///// <summary>
        ///// When Unity quits, it destroys objects in a random order.
        ///// In principle, a Singleton is only destroyed when application quits.
        ///// If any script calls Instance after it have been destroyed, 
        /////   it will create a buggy ghost object that will stay on the Editor scene
        /////   even after stopping playing the Application. Really bad!
        ///// So, this was made to be sure we're not creating that buggy ghost object.
        ///// </summary>
        //public void OnDestroy()
        //{
        //    if (IsDontDestroyOnLoad())
        //    {
        //        applicationIsQuitting = true;
        //        _instance = null;
        //    }
        //}
    }
}

