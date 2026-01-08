using UnityEngine;
using System.Collections.Generic;

namespace Riftbourne.Core
{
    /// <summary>
    /// Centralized registry for accessing managers that aren't singletons.
    /// Managers register themselves on Awake() and unregister on OnDestroy().
    /// </summary>
    public static class ManagerRegistry
    {
        private static Dictionary<System.Type, MonoBehaviour> registeredManagers = new Dictionary<System.Type, MonoBehaviour>();

        /// <summary>
        /// Register a manager instance. Called by managers in their Awake() method.
        /// </summary>
        public static void Register<T>(T manager) where T : MonoBehaviour
        {
            if (manager == null)
            {
                Debug.LogWarning($"ManagerRegistry: Attempted to register null manager of type {typeof(T).Name}");
                return;
            }

            System.Type type = typeof(T);
            if (registeredManagers.ContainsKey(type))
            {
                if (registeredManagers[type] != manager)
                {
                    Debug.LogWarning($"ManagerRegistry: Manager of type {type.Name} already registered. Replacing with new instance.");
                }
            }

            registeredManagers[type] = manager;
            Debug.Log($"ManagerRegistry: Registered {type.Name}");
        }

        /// <summary>
        /// Unregister a manager instance. Called by managers in their OnDestroy() method.
        /// </summary>
        public static void Unregister<T>(T manager) where T : MonoBehaviour
        {
            System.Type type = typeof(T);
            if (registeredManagers.ContainsKey(type) && registeredManagers[type] == manager)
            {
                registeredManagers.Remove(type);
                Debug.Log($"ManagerRegistry: Unregistered {type.Name}");
            }
        }

        /// <summary>
        /// Get a registered manager instance. Returns null if not registered.
        /// </summary>
        public static T Get<T>() where T : MonoBehaviour
        {
            System.Type type = typeof(T);
            if (registeredManagers.TryGetValue(type, out MonoBehaviour manager))
            {
                // Verify the manager still exists (hasn't been destroyed)
                if (manager != null)
                {
                    return manager as T;
                }
                else
                {
                    // Manager was destroyed but not unregistered - clean up
                    registeredManagers.Remove(type);
                }
            }

            return null;
        }

        /// <summary>
        /// Check if a manager is registered.
        /// </summary>
        public static bool IsRegistered<T>() where T : MonoBehaviour
        {
            System.Type type = typeof(T);
            if (registeredManagers.TryGetValue(type, out MonoBehaviour manager))
            {
                return manager != null;
            }
            return false;
        }

        /// <summary>
        /// Clear all registered managers (useful for testing or scene transitions).
        /// </summary>
        public static void Clear()
        {
            registeredManagers.Clear();
            Debug.Log("ManagerRegistry: Cleared all registered managers");
        }
    }
}

