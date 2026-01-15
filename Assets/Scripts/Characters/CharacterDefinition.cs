using UnityEngine;
using Riftbourne.Skills;
using Riftbourne.Items;
using Riftbourne.Inventory;
using System.Collections.Generic;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Static character data that never changes.
    /// Created by designers as ScriptableObject assets.
    /// Referenced by CharacterState for base values.
    /// </summary>
    [CreateAssetMenu(fileName = "New Character", menuName = "Riftbourne/Characters/Character Definition")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string characterID = "char_001";
        [SerializeField] private string characterName = "New Character";
        [Tooltip("Character's full name (e.g., 'Alexander the Brave')")]
        [SerializeField] private string fullName = "";
        [Tooltip("Character's title or epithet (e.g., 'The Riftwalker', 'Master Scholar')")]
        [SerializeField] private string title = "";
        [SerializeField] private Sprite portrait;
        [TextArea(3, 5)]
        [SerializeField] private string bio = "Character description";

        [Header("Base Stats")]
        [Tooltip("Starting values for core attributes")]
        [SerializeField] private int baseStrength = 5;
        [SerializeField] private int baseFinesse = 5;
        [SerializeField] private int baseFocus = 5;
        [SerializeField] private int baseSpeed = 5;
        [SerializeField] private int baseLuck = 5;

        [Header("Base Narrative Skills")]
        [Tooltip("Starting values for exploration narrative skills")]
        [SerializeField] private int basePerception = 5;
        [SerializeField] private int baseInterpretive = 3;
        [SerializeField] private int baseEmpathic = 4;

        [Header("Character Type")]
        [SerializeField] private MantleType mantle = MantleType.None;

        [Header("Skills and Equipment")]
        [Tooltip("Skills this character can learn")]
        [SerializeField] private List<Skill> availableSkills = new List<Skill>();
        [Tooltip("Optional starting equipment")]
        [SerializeField] private List<EquipmentItem> startingEquipment = new List<EquipmentItem>();

        [Header("Starting Inventory")]
        [Tooltip("Optional starting inventory items")]
        [SerializeField] private List<InventorySlot> startingInventory = new List<InventorySlot>();
        [Tooltip("Starting Aurum Shards (currency)")]
        [SerializeField] private int startingAurumShards = 0;

        [Header("Character Metadata")]
        [Tooltip("Can this character be the protagonist (POV character)?")]
        [SerializeField] private bool isPOVCharacter = false;
        [Tooltip("Chapter IDs where this character can appear")]
        [SerializeField] private List<string> availableChapters = new List<string>();
        [Tooltip("Which storylines include this character")]
        [SerializeField] private List<string> narrativeTracks = new List<string>();
        [Tooltip("Chapter IDs where this character must appear")]
        [SerializeField] private List<string> requiredChapters = new List<string>();
        [Tooltip("Chapter ID when character joins party (if applicable)")]
        [SerializeField] private string joinChapter = "";
        [Tooltip("Chapter ID when character leaves party (if applicable)")]
        [SerializeField] private string leaveChapter = "";

        // Public properties
        public string CharacterID => characterID;
        public string CharacterName => characterName;
        public string FullName => string.IsNullOrEmpty(fullName) ? characterName : fullName;
        public string Title => title;
        public Sprite Portrait => portrait;
        public string Bio => bio;

        // Base stats
        public int BaseStrength => baseStrength;
        public int BaseFinesse => baseFinesse;
        public int BaseFocus => baseFocus;
        public int BaseSpeed => baseSpeed;
        public int BaseLuck => baseLuck;

        // Base narrative skills
        public int BasePerception => basePerception;
        public int BaseInterpretive => baseInterpretive;
        public int BaseEmpathic => baseEmpathic;

        // Character type
        public MantleType Mantle => mantle;

        // Skills and equipment
        public List<Skill> AvailableSkills => new List<Skill>(availableSkills);
        public List<EquipmentItem> StartingEquipment => new List<EquipmentItem>(startingEquipment);

        // Starting inventory
        public List<InventorySlot> StartingInventory => new List<InventorySlot>(startingInventory);
        public int StartingAurumShards => startingAurumShards;

        // Metadata
        public bool IsPOVCharacter => isPOVCharacter;
        public List<string> AvailableChapters => new List<string>(availableChapters);
        public List<string> NarrativeTracks => new List<string>(narrativeTracks);
        public List<string> RequiredChapters => new List<string>(requiredChapters);
        public string JoinChapter => joinChapter;
        public string LeaveChapter => leaveChapter;
    }
}
