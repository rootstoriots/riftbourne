using UnityEngine;

namespace Riftbourne.Items
{
    /// <summary>
    /// ScriptableObject that can be referenced by items to store examine/secrets data.
    /// Reveals hidden information when the item is examined.
    /// </summary>
    [CreateAssetMenu(fileName = "New Item Examine Data", menuName = "Riftbourne/Items/Item Examine Data")]
    public class ItemExamineData : ScriptableObject
    {
        [Header("Secrets")]
        [Tooltip("Hidden text revealed when examining this item")]
        [TextArea(3, 6)]
        [SerializeField] private string secretsText = "";

        [Tooltip("Additional secrets that can be discovered (for future expansion)")]
        [SerializeField] private string[] additionalSecrets = new string[0];

        // Properties
        public string SecretsText => secretsText;

        /// <summary>
        /// Get the secrets text for this item.
        /// </summary>
        public string GetSecretsText()
        {
            if (string.IsNullOrEmpty(secretsText))
                return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>Secrets Discovered:</b>");
            sb.AppendLine(secretsText);

            // Add additional secrets if any
            if (additionalSecrets != null && additionalSecrets.Length > 0)
            {
                foreach (var secret in additionalSecrets)
                {
                    if (!string.IsNullOrEmpty(secret))
                    {
                        sb.AppendLine(secret);
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Check if this item has any secrets.
        /// </summary>
        public bool HasSecrets()
        {
            return !string.IsNullOrEmpty(secretsText) || 
                   (additionalSecrets != null && additionalSecrets.Length > 0);
        }
    }
}
