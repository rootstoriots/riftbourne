using UnityEngine;
using UnityEngine.InputSystem;
using Riftbourne.Utils;
using Riftbourne.Exploration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Riftbourne.Core
{
    /// <summary>
    /// Singleton manager for save/load operations.
    /// Handles autosave, quicksave, manual saves, and loading.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Save Settings")]
        [SerializeField] private int maxAutosaveSlots = 5;
        [SerializeField] private int maxQuicksaveSlots = 5;

        [Header("Input")]
        [SerializeField] private PlayerInputActions inputActions;

        // Events
        public System.Action<SaveData> OnGameSaved;
        public System.Action<SaveData> OnGameLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize input actions
            if (inputActions == null)
            {
                inputActions = new PlayerInputActions();
            }
        }

        private void OnEnable()
        {
            inputActions?.Gameplay.Enable();
            
            // Subscribe to quicksave input (F5)
            // Note: We'll need to add this to PlayerInputActions
            // For now, we'll handle it via Update
        }

        private void OnDisable()
        {
            inputActions?.Gameplay.Disable();
        }

        private void Update()
        {
            // Handle F5 quicksave
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                QuickSave();
            }
        }

        /// <summary>
        /// Save game with specified type and optional custom name.
        /// </summary>
        public bool SaveGame(SaveType saveType, string customName = null)
        {
            try
            {
                // Create save data from current state
                string saveName = customName ?? GenerateSaveName(saveType);
                SaveData data = SaveData.CreateFromCurrentState(saveName, saveType);

                // Determine file path
                string filePath;
                int slot = 0;

                if (saveType == SaveType.Manual)
                {
                    filePath = SaveFileHandler.GetManualSaveFilePath(saveName);
                }
                else
                {
                    // For autosave/quicksave, rotate slots
                    slot = RotateSaveSlots(saveType);
                    filePath = SaveFileHandler.GetSaveFilePath(saveType, slot);
                }

                // Capture screenshot
                string screenshotPath = SaveFileHandler.GetScreenshotFilePath(saveType, slot, saveName);
                StartCoroutine(CaptureScreenshotAndSave(data, filePath, screenshotPath, saveType, slot));

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SaveManager: Failed to save game: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Coroutine to capture screenshot and save game.
        /// </summary>
        private IEnumerator CaptureScreenshotAndSave(SaveData data, string filePath, string screenshotPath, SaveType saveType, int slot)
        {
            // Wait for end of frame to capture screenshot (ensures everything is rendered)
            yield return new WaitForEndOfFrame();
            
            // Wait one more frame to ensure UI is fully rendered (we'll capture without UI anyway)
            yield return null;

            // Capture screenshot (without UI - uses camera RenderTexture)
            bool screenshotSuccess = ScreenshotCapture.CaptureAndSave(screenshotPath);
            if (screenshotSuccess)
            {
                // Store relative screenshot path in save data
                data.screenshotPath = SaveFileHandler.GetRelativeScreenshotPath(saveType, slot, data.saveName);
                Debug.Log($"SaveManager: Screenshot captured and saved to {screenshotPath}");
            }
            else
            {
                Debug.LogWarning("SaveManager: Failed to capture screenshot, continuing with save anyway");
            }

            // Save game data
            bool saveSuccess = SaveFileHandler.SaveToFile(data, filePath);
            if (saveSuccess)
            {
                Debug.Log($"SaveManager: Successfully saved {saveType} game: {data.saveName}");
                OnGameSaved?.Invoke(data);
            }
            else
            {
                Debug.LogError($"SaveManager: Failed to save game to {filePath}");
            }
        }

        /// <summary>
        /// Rotate save slots for autosave/quicksave.
        /// Returns the slot number to use (0 = most recent).
        /// </summary>
        private int RotateSaveSlots(SaveType saveType)
        {
            int maxSlots = saveType == SaveType.Autosave ? maxAutosaveSlots : maxQuicksaveSlots;
            string directory = SaveFileHandler.GetSaveTypeDirectory(saveType);

            // Get existing saves
            List<SaveFileInfo> existingSaves = SaveFileHandler.GetSaveFiles(saveType);

            // If we have max slots, delete the oldest
            if (existingSaves.Count >= maxSlots)
            {
                // Delete the oldest save (last in sorted list)
                SaveFileInfo oldestSave = existingSaves[existingSaves.Count - 1];
                SaveFileHandler.DeleteSave(oldestSave.filePath);
            }

            // Shift existing saves
            for (int i = existingSaves.Count - 1; i >= 0; i--)
            {
                int oldSlot = ExtractSlotFromPath(existingSaves[i].filePath);
                int newSlot = oldSlot + 1;

                if (newSlot < maxSlots)
                {
                    string oldPath = existingSaves[i].filePath;
                    string newPath = SaveFileHandler.GetSaveFilePath(saveType, newSlot);
                    
                    // Move file
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Move(oldPath, newPath);
                    }

                    // Move screenshot
                    string oldScreenshotPath = oldPath.Replace(".json", ".png").Replace(
                        System.IO.Path.GetDirectoryName(oldPath),
                        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(oldPath), "Screenshots"));
                    string newScreenshotPath = newPath.Replace(".json", ".png").Replace(
                        System.IO.Path.GetDirectoryName(newPath),
                        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(newPath), "Screenshots"));
                    
                    if (System.IO.File.Exists(oldScreenshotPath))
                    {
                        System.IO.File.Move(oldScreenshotPath, newScreenshotPath);
                    }
                }
            }

            // Return slot 0 for the new save
            return 0;
        }

        /// <summary>
        /// Extract slot number from save file path.
        /// </summary>
        private int ExtractSlotFromPath(string filePath)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            if (fileName.StartsWith("save_"))
            {
                string slotStr = fileName.Substring(5);
                int slot;
                if (int.TryParse(slotStr, out slot))
                {
                    return slot;
                }
            }
            return -1;
        }

        /// <summary>
        /// Generate a save name based on type.
        /// </summary>
        private string GenerateSaveName(SaveType saveType)
        {
            switch (saveType)
            {
                case SaveType.Autosave:
                    return "Autosave";
                case SaveType.Quicksave:
                    return "Quicksave";
                case SaveType.Manual:
                    return $"Save_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                default:
                    return "Save";
            }
        }

        /// <summary>
        /// Autosave (called after battle ends).
        /// </summary>
        public void AutoSave()
        {
            SaveGame(SaveType.Autosave);
        }

        /// <summary>
        /// Quicksave (called by F5 key).
        /// </summary>
        public void QuickSave()
        {
            SaveGame(SaveType.Quicksave);
        }

        /// <summary>
        /// Manual save with custom name.
        /// </summary>
        public bool ManualSave(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
            {
                Debug.LogWarning("SaveManager: Cannot save with empty name");
                return false;
            }
            return SaveGame(SaveType.Manual, saveName);
        }

        /// <summary>
        /// Load game from file path.
        /// </summary>
        public bool LoadGame(string filePath)
        {
            try
            {
                SaveData data = SaveFileHandler.LoadFromFile(filePath);
                if (data == null)
                {
                    Debug.LogError("SaveManager: Failed to load save data");
                    return false;
                }

                // Restore game state
                StartCoroutine(RestoreGameState(data));
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SaveManager: Failed to load game: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Coroutine to restore game state from SaveData.
        /// </summary>
        private IEnumerator RestoreGameState(SaveData data)
        {
            Debug.Log($"SaveManager: Starting to load game from scene: {data.currentSceneName}");
            
            // Load scene asynchronously and wait for it to complete
            UnityEngine.AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(data.currentSceneName);
            
            // Wait until scene is fully loaded
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            Debug.Log("SaveManager: Scene loaded, waiting for objects to initialize...");
            
            // Wait a frame for all objects to initialize
            yield return null;
            yield return new WaitForSeconds(0.1f);

            // Restore party
            if (PartyManager.Instance != null)
            {
                PartyManager.Instance.ClearParty();
                foreach (var characterState in data.partyMembers)
                {
                    // Restore runtime references
                    characterState.RestoreRuntimeReferences();
                    PartyManager.Instance.AddPartyMember(characterState);
                }

                // Restore POV character
                if (!string.IsNullOrEmpty(data.povCharacterID))
                {
                    var povCharacter = data.partyMembers.FirstOrDefault(c => c.CharacterID == data.povCharacterID);
                    if (povCharacter != null)
                    {
                        PartyManager.Instance.SetPOVCharacter(povCharacter);
                    }
                }
            }

            // Restore chapter state
            if (ChapterManager.Instance != null && !string.IsNullOrEmpty(data.currentChapterID))
            {
                ChapterManager.Instance.LoadChapter(data.currentChapterID);
                
                // Restore chapter progression state
                Dictionary<string, bool> progressionState = new Dictionary<string, bool>();
                foreach (var entry in data.chapterProgressionState)
                {
                    progressionState[entry.chapterID] = entry.isCompleted;
                }
                ChapterManager.Instance.SetChapterProgressionState(progressionState);
            }

            // Restore journal entries
            if (JournalSystem.Instance != null)
            {
                JournalSystem.Instance.ClearEntries();
                foreach (var serializableEntry in data.journalEntries)
                {
                    var entry = serializableEntry.ToJournalEntry();
                    // JournalSystem doesn't have a method to add entries with all properties
                    // We'll need to add one or use reflection
                    JournalSystem.Instance.AddEntry(
                        entry.EntryText,
                        entry.ConfidenceLevel,
                        entry.IsUnresolved,
                        entry.RelatedSymbols,
                        entry.CanBeRecontextualized,
                        entry.IsKnownIncorrect
                    );
                }
            }

            // Restore player position
            // Wait for scene to fully load and ExplorationController to be available
            yield return new WaitForSeconds(0.2f);
            
            // Try multiple times to find ExplorationController (in case scene is still loading)
            ExplorationController explorationController = null;
            int attempts = 0;
            while (explorationController == null && attempts < 10)
            {
                explorationController = UnityEngine.Object.FindFirstObjectByType<ExplorationController>();
                if (explorationController == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    attempts++;
                }
            }
            
            if (explorationController != null)
            {
                // CharacterController requires special handling for position setting
                var characterController = explorationController.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    // Disable CharacterController, set position, then re-enable
                    characterController.enabled = false;
                    explorationController.transform.position = data.playerPosition;
                    characterController.enabled = true;
                    Debug.Log($"SaveManager: Restored player position to {data.playerPosition} (X:{data.playerPosition.x:F2}, Y:{data.playerPosition.y:F2}, Z:{data.playerPosition.z:F2})");
                }
                else
                {
                    // Fallback: just set transform position
                    explorationController.transform.position = data.playerPosition;
                    Debug.Log($"SaveManager: Restored player position to {data.playerPosition} (no CharacterController)");
                }
            }
            else
            {
                Debug.LogWarning("SaveManager: Could not find ExplorationController to restore position!");
            }

            Debug.Log($"SaveManager: Successfully loaded game: {data.saveName}");
            OnGameLoaded?.Invoke(data);
            
            // Resume time scale in case it was paused
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Get all save files of a specific type.
        /// </summary>
        public List<SaveFileInfo> GetSaveFiles(SaveType saveType)
        {
            return SaveFileHandler.GetSaveFiles(saveType);
        }

        /// <summary>
        /// Delete a save file.
        /// </summary>
        public bool DeleteSave(string filePath)
        {
            return SaveFileHandler.DeleteSave(filePath);
        }
    }
}
