using UnityEngine;
using System.Collections.Generic;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Registry that maps StatusEffectType enum values to StatusEffectData ScriptableObjects.
    /// Configure this in the Unity Inspector to link enum types to their data.
    /// Place an instance in Resources folder named "StatusEffectRegistry"
    /// </summary>
    [CreateAssetMenu(fileName = "StatusEffectRegistry", menuName = "Riftbourne/Status Effect Registry")]
    public class StatusEffectRegistry : ScriptableObject
    {
        [Tooltip("List of status effect data. Each StatusEffectType should have one entry here.")]
        [SerializeField] private List<StatusEffectData> statusEffectDataList = new List<StatusEffectData>();

        private Dictionary<string, StatusEffectData> dataLookup;

        /// <summary>
        /// Build the lookup dictionary from the list (by name for easy lookup).
        /// Call this after loading or when data changes.
        /// </summary>
        public void BuildLookup()
        {
            dataLookup = new Dictionary<string, StatusEffectData>();
            foreach (var data in statusEffectDataList)
            {
                if (data != null && !string.IsNullOrEmpty(data.EffectName))
                {
                    dataLookup[data.EffectName] = data;
                }
            }
        }

        /// <summary>
        /// Get the StatusEffectData by name.
        /// </summary>
        public StatusEffectData GetDataByName(string effectName)
        {
            if (dataLookup == null)
            {
                BuildLookup();
            }

            if (dataLookup.TryGetValue(effectName, out StatusEffectData data))
            {
                return data;
            }

            Debug.LogWarning($"StatusEffectData named '{effectName}' not found in registry!");
            return null;
        }

        /// <summary>
        /// Check if a status effect is registered by name.
        /// </summary>
        public bool IsRegistered(string effectName)
        {
            if (dataLookup == null)
            {
                BuildLookup();
            }
            return dataLookup.ContainsKey(effectName);
        }

        private void OnEnable()
        {
            BuildLookup();
        }

        // Singleton instance (loaded from Resources)
        private static StatusEffectRegistry _instance;
        public static StatusEffectRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<StatusEffectRegistry>("StatusEffectRegistry");
                    if (_instance == null)
                    {
                        Debug.LogError("StatusEffectRegistry instance not found! Create one via Assets > Create > Riftbourne > Status Effect Registry and place it in a Resources folder.");
                    }
                    else
                    {
                        _instance.BuildLookup();
                    }
                }
                return _instance;
            }
            set
            {
                _instance = value;
                if (_instance != null)
                {
                    _instance.BuildLookup();
                }
            }
        }
    }
}

