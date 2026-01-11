using UnityEngine;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Factory for creating CharacterState instances from CharacterDefinitions.
    /// </summary>
    public static class CharacterStateFactory
    {
        /// <summary>
        /// Creates a CharacterState from a CharacterDefinition.
        /// Initializes with base values and applies starting equipment.
        /// </summary>
        public static CharacterState Create(CharacterDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("CharacterStateFactory: Cannot create state from null definition!");
                return null;
            }

            CharacterState state = new CharacterState(definition);
            Debug.Log($"CharacterStateFactory: Created CharacterState for {definition.CharacterName} (ID: {definition.CharacterID})");
            return state;
        }
    }
}
