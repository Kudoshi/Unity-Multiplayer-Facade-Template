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
    /// - 27/10/2025 : Removed auto creation of singleton and destroy checking. To prevent null 
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

                        
                    }

                    return _instance;
                }
            }
        }
        protected static void SetSingletonDontDestroyOnLoad(T singleton)
        {
            if (singleton == null) return;

            // Compare with the singleton instance
            if (_instance == null)
            {
                _instance = singleton;
                Util.SetDontDestroyOnLoad(_instance.gameObject);
            }
            else if (singleton != _instance)
            {
                Debug.Log($"{singleton.GetType()} singleton instance already exist! Destroying the new one");
                UnityEngine.Object.Destroy(singleton.gameObject);
            }
        }
    }
}

