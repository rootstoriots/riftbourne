using UnityEngine;
using System.Collections.Generic;
using Riftbourne.Items;

namespace Riftbourne.UI
{
    /// <summary>
    /// Manages item button styles for different item types.
    /// Singleton that provides style lookup functionality.
    /// </summary>
    public class ItemButtonStyleManager : MonoBehaviour
    {
        public static ItemButtonStyleManager Instance { get; private set; }

        [Header("Button Styles")]
        [Tooltip("Collection of button styles for each item type")]
        [SerializeField] private List<ItemButtonStyle> buttonStyles = new List<ItemButtonStyle>();

        [Header("Default Style")]
        [Tooltip("Default style to use if no specific style is found")]
        [SerializeField] private ItemButtonStyle defaultStyle;

        private Dictionary<ItemType, ItemButtonStyle> styleCache = new Dictionary<ItemType, ItemButtonStyle>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("ItemButtonStyleManager: Multiple instances detected! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildStyleCache();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Build the style cache for fast lookup.
        /// </summary>
        private void BuildStyleCache()
        {
            styleCache.Clear();

            foreach (var style in buttonStyles)
            {
                if (style != null)
                {
                    styleCache[style.ItemType] = style;
                }
            }
        }

        /// <summary>
        /// Get the button style for a specific item type.
        /// </summary>
        public ItemButtonStyle GetStyleForItemType(ItemType itemType)
        {
            if (styleCache.ContainsKey(itemType))
            {
                return styleCache[itemType];
            }

            // Return default style if no specific style found
            return defaultStyle;
        }

        /// <summary>
        /// Add a button style to the collection.
        /// </summary>
        public void AddStyle(ItemButtonStyle style)
        {
            if (style != null && !buttonStyles.Contains(style))
            {
                buttonStyles.Add(style);
                BuildStyleCache();
            }
        }

        /// <summary>
        /// Remove a button style from the collection.
        /// </summary>
        public void RemoveStyle(ItemButtonStyle style)
        {
            if (buttonStyles.Remove(style))
            {
                BuildStyleCache();
            }
        }

        /// <summary>
        /// Refresh the style cache (call after modifying styles in editor).
        /// </summary>
        public void RefreshCache()
        {
            BuildStyleCache();
        }
    }
}
