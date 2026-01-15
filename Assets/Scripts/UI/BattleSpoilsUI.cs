using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Riftbourne.Combat;
using Riftbourne.Characters;
using Riftbourne.Core;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Riftbourne.UI
{
    /// <summary>
    /// Displays comprehensive battle statistics and spoils after victory.
    /// Shows per-character breakdown and party-wide totals.
    /// </summary>
    public class BattleSpoilsUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject spoilsPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button continueButton;
        
        [Header("Party Totals Section")]
        [SerializeField] private GameObject partyTotalsSection;
        [SerializeField] private TMP_Text totalSPText;
        [SerializeField] private TMP_Text totalKillsText;
        [SerializeField] private TMP_Text totalCritsText;
        [SerializeField] private TMP_Text totalDamageText;
        [SerializeField] private TMP_Text totalSkillsUsedText;
        [SerializeField] private TMP_Text totalSkillsMasteredText;
        
        [Header("Per-Character Section")]
        [SerializeField] private Transform characterStatsContainer;
        [SerializeField] private GameObject characterStatPrefab; // Prefab for individual character stats
        
        [Header("Skills Mastered Section")]
        [SerializeField] private GameObject skillsMasteredSection;
        [SerializeField] private Transform skillsMasteredContainer;
        [SerializeField] private GameObject skillMasteredPrefab; // Prefab for skill mastered entry
        
        private BattleStatistics statistics;
        private Action onContinue;
        
        private void Awake()
        {
            // Hide panel initially
            if (spoilsPanel != null)
            {
                spoilsPanel.SetActive(false);
            }
            
            // Setup button
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }
        
        /// <summary>
        /// Show the spoils screen with battle statistics.
        /// </summary>
        public void ShowSpoils(BattleStatistics stats, Action onContinueCallback)
        {
            Debug.Log("BattleSpoilsUI: ShowSpoils called");
            
            if (stats == null)
            {
                Debug.LogWarning("BattleSpoilsUI: BattleStatistics is null!");
                onContinueCallback?.Invoke();
                return;
            }
            
            if (spoilsPanel == null)
            {
                Debug.LogError("BattleSpoilsUI: spoilsPanel is null! Cannot show spoils screen.");
                onContinueCallback?.Invoke();
                return;
            }
            
            statistics = stats;
            onContinue = onContinueCallback;
            
            // Finalize SP tracking before displaying
            List<Unit> partyMembers = GetPartyMembers();
            if (BattleStatisticsTracker.Instance != null)
            {
                BattleStatisticsTracker.Instance.FinalizeSPTracking(partyMembers);
            }
            
            // Update UI
            UpdatePartyTotals();
            UpdateCharacterStats();
            UpdateSkillsMastered();
            
            // Show panel
            spoilsPanel.SetActive(true);
            Debug.Log("BattleSpoilsUI: Panel activated");
            
            // Pause game time
            Time.timeScale = 0f;
        }
        
        /// <summary>
        /// Update party-wide totals display.
        /// </summary>
        private void UpdatePartyTotals()
        {
            if (statistics == null) return;
            
            if (totalSPText != null)
            {
                totalSPText.text = $"SP Earned: {statistics.TotalSPEarned}";
            }
            
            if (totalKillsText != null)
            {
                totalKillsText.text = $"Kills: {statistics.TotalKills}";
            }
            
            if (totalCritsText != null)
            {
                totalCritsText.text = $"Critical Hits: {statistics.TotalCriticalHits}";
            }
            
            if (totalDamageText != null)
            {
                totalDamageText.text = $"Damage Dealt: {statistics.TotalDamageDealt}";
            }
            
            if (totalSkillsUsedText != null)
            {
                totalSkillsUsedText.text = $"Skills Used: {statistics.TotalSkillsUsed}";
            }
            
            if (totalSkillsMasteredText != null)
            {
                totalSkillsMasteredText.text = $"Skills Mastered: {statistics.TotalSkillsMastered}";
            }
        }
        
        /// <summary>
        /// Update per-character statistics display.
        /// </summary>
        private void UpdateCharacterStats()
        {
            if (characterStatsContainer == null || statistics == null) return;
            
            // Clear existing entries
            foreach (Transform child in characterStatsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Get all character stats
            Dictionary<string, CharacterBattleStats> charStats = statistics.GetCharacterStats();
            
            foreach (var kvp in charStats)
            {
                CharacterBattleStats stats = kvp.Value;
                
                // Create character stat entry
                GameObject entryObj = null;
                if (characterStatPrefab != null)
                {
                    entryObj = Instantiate(characterStatPrefab, characterStatsContainer);
                }
                else
                {
                    // Fallback: create simple text entry
                    entryObj = new GameObject($"Stat_{stats.CharacterName}");
                    entryObj.transform.SetParent(characterStatsContainer, false);
                    RectTransform rect = entryObj.AddComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(400, 100);
                    
                    TMP_Text text = entryObj.AddComponent<TextMeshProUGUI>();
                    text.text = FormatCharacterStats(stats);
                }
                
                // If using prefab, update its text components
                if (characterStatPrefab != null)
                {
                    TMP_Text[] texts = entryObj.GetComponentsInChildren<TMP_Text>();
                    if (texts.Length > 0)
                    {
                        texts[0].text = FormatCharacterStats(stats);
                    }
                }
            }
        }
        
        /// <summary>
        /// Format character statistics as a string.
        /// </summary>
        private string FormatCharacterStats(CharacterBattleStats stats)
        {
            return $"{stats.CharacterName}\n" +
                   $"  SP: {stats.SPEarned} | Kills: {stats.Kills} | Crits: {stats.CriticalHits}\n" +
                   $"  Damage: {stats.DamageDealt} | Skills Used: {stats.SkillsUsed}";
        }
        
        /// <summary>
        /// Update skills mastered display.
        /// </summary>
        private void UpdateSkillsMastered()
        {
            if (skillsMasteredContainer == null || statistics == null) return;
            
            // Clear existing entries
            foreach (Transform child in skillsMasteredContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Collect all mastered skills
            Dictionary<string, List<string>> skillsByCharacter = new Dictionary<string, List<string>>();
            Dictionary<string, CharacterBattleStats> charStats = statistics.GetCharacterStats();
            
            foreach (var kvp in charStats)
            {
                CharacterBattleStats stats = kvp.Value;
                if (stats.SkillsMastered.Count > 0)
                {
                    skillsByCharacter[stats.CharacterName] = new List<string>(stats.SkillsMastered);
                }
            }
            
            // Show section only if there are mastered skills
            if (skillsMasteredSection != null)
            {
                skillsMasteredSection.SetActive(skillsByCharacter.Count > 0);
            }
            
            // Create entries for each mastered skill
            foreach (var kvp in skillsByCharacter)
            {
                string characterName = kvp.Key;
                foreach (string skillName in kvp.Value)
                {
                    GameObject entryObj = null;
                    if (skillMasteredPrefab != null)
                    {
                        entryObj = Instantiate(skillMasteredPrefab, skillsMasteredContainer);
                    }
                    else
                    {
                        // Fallback: create simple text entry
                        entryObj = new GameObject($"Skill_{skillName}");
                        entryObj.transform.SetParent(skillsMasteredContainer, false);
                        RectTransform rect = entryObj.AddComponent<RectTransform>();
                        rect.sizeDelta = new Vector2(300, 30);
                        
                        TMP_Text text = entryObj.AddComponent<TextMeshProUGUI>();
                        text.text = $"{characterName} mastered {skillName}!";
                    }
                    
                    // If using prefab, update its text components
                    if (skillMasteredPrefab != null)
                    {
                        TMP_Text[] texts = entryObj.GetComponentsInChildren<TMP_Text>();
                        if (texts.Length > 0)
                        {
                            texts[0].text = $"{characterName} mastered {skillName}!";
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Get party members list.
        /// </summary>
        private List<Unit> GetPartyMembers()
        {
            if (PartyManager.Instance != null)
            {
                return PartyManager.Instance.GetPartyMembersAsUnits();
            }
            return new List<Unit>();
        }
        
        /// <summary>
        /// Handle continue button click.
        /// </summary>
        private void OnContinueClicked()
        {
            // Hide panel
            if (spoilsPanel != null)
            {
                spoilsPanel.SetActive(false);
            }
            
            // Resume game time
            Time.timeScale = 1f;
            
            // Invoke callback
            onContinue?.Invoke();
            onContinue = null;
        }
        
        /// <summary>
        /// Check if spoils screen is currently showing.
        /// </summary>
        public bool IsShowing()
        {
            return spoilsPanel != null && spoilsPanel.activeSelf;
        }
    }
}
