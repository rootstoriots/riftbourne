using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Riftbourne.Core;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.UI
{
    /// <summary>
    /// UI component for displaying save list and handling save/load interactions.
    /// </summary>
    public class SaveLoadUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject savePanel;
        [SerializeField] private GameObject loadPanel;

        [Header("Save Panel UI")]
        [SerializeField] private TMP_Dropdown saveTypeDropdown;
        [SerializeField] private TMP_InputField saveNameInput;
        [SerializeField] private Button saveConfirmButton;
        [SerializeField] private Button saveCancelButton;
        [SerializeField] private TMP_Text saveStatusText;

        [Header("Load Panel UI")]
        [SerializeField] private Transform saveListContainer;
        [SerializeField] private GameObject saveItemPrefab;
        [SerializeField] private Button loadCancelButton;
        [SerializeField] private TMP_Text loadStatusText;

        [Header("Save Item UI Elements")]
        [SerializeField] private Image screenshotImage;
        [SerializeField] private TMP_Text saveNameText;
        [SerializeField] private TMP_Text timestampText;
        [SerializeField] private TMP_Text chapterText;
        [SerializeField] private TMP_Text saveTypeText;

        private List<GameObject> saveItemObjects = new List<GameObject>();
        private SaveType currentSaveType = SaveType.Manual;

        private void Awake()
        {
            if (savePanel != null)
            {
                savePanel.SetActive(false);
            }

            if (loadPanel != null)
            {
                loadPanel.SetActive(false);
            }
        }

        private void Start()
        {
            // Setup save panel
            if (saveTypeDropdown != null)
            {
                saveTypeDropdown.ClearOptions();
                saveTypeDropdown.AddOptions(new List<string> { "Manual Save", "Quicksave", "Autosave" });
                saveTypeDropdown.onValueChanged.AddListener(OnSaveTypeChanged);
                currentSaveType = SaveType.Manual; // Default to manual
            }

            // Setup save name input validation
            if (saveNameInput != null)
            {
                saveNameInput.onValueChanged.AddListener(OnSaveNameChanged);
            }

            if (saveConfirmButton != null)
            {
                saveConfirmButton.onClick.AddListener(OnSaveConfirm);
            }

            if (saveCancelButton != null)
            {
                saveCancelButton.onClick.AddListener(Hide);
            }

            // Setup load panel
            if (loadCancelButton != null)
            {
                loadCancelButton.onClick.AddListener(Hide);
            }
        }

        /// <summary>
        /// Show the save panel.
        /// </summary>
        public void ShowSavePanel()
        {
            if (savePanel != null)
            {
                savePanel.SetActive(true);
            }

            if (loadPanel != null)
            {
                loadPanel.SetActive(false);
            }

            // Reset UI
            if (saveNameInput != null)
            {
                saveNameInput.text = "";
            }

            if (saveStatusText != null)
            {
                saveStatusText.text = "";
            }

            // Show/hide save name input based on save type
            UpdateSaveUI();
        }

        /// <summary>
        /// Show the load panel.
        /// </summary>
        public void ShowLoadPanel()
        {
            if (loadPanel != null)
            {
                loadPanel.SetActive(true);
            }

            if (savePanel != null)
            {
                savePanel.SetActive(false);
            }

            if (loadStatusText != null)
            {
                loadStatusText.text = "";
            }

            RefreshSaveList();
        }

        /// <summary>
        /// Hide both panels.
        /// </summary>
        public void Hide()
        {
            if (savePanel != null)
            {
                savePanel.SetActive(false);
            }

            if (loadPanel != null)
            {
                loadPanel.SetActive(false);
            }
        }

        private void OnSaveTypeChanged(int index)
        {
            // Map dropdown index to SaveType enum
            // 0 = Manual Save, 1 = Quicksave, 2 = Autosave
            switch (index)
            {
                case 0:
                    currentSaveType = SaveType.Manual;
                    break;
                case 1:
                    currentSaveType = SaveType.Quicksave;
                    break;
                case 2:
                    currentSaveType = SaveType.Autosave;
                    break;
                default:
                    currentSaveType = SaveType.Manual;
                    break;
            }
            UpdateSaveUI();
        }

        private void OnSaveNameChanged(string value)
        {
            // Enable save button only if name is entered for manual saves
            if (saveConfirmButton != null && currentSaveType == SaveType.Manual)
            {
                saveConfirmButton.interactable = !string.IsNullOrEmpty(value);
            }
        }

        private void UpdateSaveUI()
        {
            // Show save name input only for manual saves
            if (saveNameInput != null)
            {
                saveNameInput.gameObject.SetActive(currentSaveType == SaveType.Manual);
            }

            // Update button text or enable state
            if (saveConfirmButton != null)
            {
                if (currentSaveType == SaveType.Manual)
                {
                    saveConfirmButton.interactable = false; // Enable when name is entered
                }
                else
                {
                    saveConfirmButton.interactable = true;
                }
            }
        }

        private void OnSaveConfirm()
        {
            if (SaveManager.Instance == null)
            {
                ShowSaveStatus("Error: SaveManager not found!", true);
                return;
            }

            string saveName = null;
            if (currentSaveType == SaveType.Manual)
            {
                if (saveNameInput != null)
                {
                    saveName = saveNameInput.text;
                }

                if (string.IsNullOrEmpty(saveName))
                {
                    ShowSaveStatus("Please enter a save name", true);
                    return;
                }
            }

            bool success = false;
            switch (currentSaveType)
            {
                case SaveType.Autosave:
                    SaveManager.Instance.AutoSave();
                    success = true;
                    break;
                case SaveType.Quicksave:
                    SaveManager.Instance.QuickSave();
                    success = true;
                    break;
                case SaveType.Manual:
                    success = SaveManager.Instance.ManualSave(saveName);
                    break;
            }

            if (success)
            {
                ShowSaveStatus("Game saved successfully!", false);
                // Hide panel after a delay
                Invoke(nameof(Hide), 1f);
            }
            else
            {
                ShowSaveStatus("Failed to save game", true);
            }
        }

        private void ShowSaveStatus(string message, bool isError)
        {
            if (saveStatusText != null)
            {
                saveStatusText.text = message;
                saveStatusText.color = isError ? Color.red : Color.green;
            }
        }

        private void RefreshSaveList()
        {
            // Clear existing items
            foreach (var item in saveItemObjects)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            saveItemObjects.Clear();

            if (saveListContainer == null)
            {
                Debug.LogWarning("SaveLoadUI: saveListContainer is null!");
                return;
            }

            if (SaveManager.Instance == null)
            {
                Debug.LogWarning("SaveLoadUI: SaveManager.Instance is null!");
                return;
            }

            // Get all save files (combine all types)
            List<SaveFileInfo> allSaves = new List<SaveFileInfo>();
            allSaves.AddRange(SaveManager.Instance.GetSaveFiles(SaveType.Manual));
            allSaves.AddRange(SaveManager.Instance.GetSaveFiles(SaveType.Quicksave));
            allSaves.AddRange(SaveManager.Instance.GetSaveFiles(SaveType.Autosave));

            // Sort by timestamp (newest first)
            allSaves = allSaves.OrderByDescending(s => 
            {
                System.DateTime time;
                if (System.DateTime.TryParse(s.saveData.timestamp, out time))
                {
                    return time;
                }
                return System.DateTime.MinValue;
            }).ToList();

            // Create UI items for each save
            foreach (var saveInfo in allSaves)
            {
                CreateSaveListItem(saveInfo);
            }

            if (allSaves.Count == 0)
            {
                if (loadStatusText != null)
                {
                    loadStatusText.text = "No save files found";
                }
            }
        }

        private void CreateSaveListItem(SaveFileInfo saveInfo)
        {
            GameObject itemObj = null;

            if (saveItemPrefab != null)
            {
                itemObj = Instantiate(saveItemPrefab, saveListContainer);
            }
            else
            {
                // Create basic save item UI
                itemObj = CreateBasicSaveItem(saveInfo);
            }

            if (itemObj != null)
            {
                SetupSaveItem(itemObj, saveInfo);
                saveItemObjects.Add(itemObj);
            }
        }

        private GameObject CreateBasicSaveItem(SaveFileInfo saveInfo)
        {
            GameObject itemObj = new GameObject("SaveItem");
            itemObj.transform.SetParent(saveListContainer, false);

            // Add RectTransform
            RectTransform rect = itemObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 100);

            // Add Image for background
            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Add Button
            Button button = itemObj.AddComponent<Button>();
            button.onClick.AddListener(() => OnSaveItemClicked(saveInfo));

            // Add Horizontal Layout Group
            HorizontalLayoutGroup layout = itemObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // Screenshot image
            GameObject screenshotObj = new GameObject("Screenshot");
            screenshotObj.transform.SetParent(itemObj.transform, false);
            RectTransform screenshotRect = screenshotObj.AddComponent<RectTransform>();
            screenshotRect.sizeDelta = new Vector2(80, 80);
            Image screenshotImg = screenshotObj.AddComponent<Image>();

            // Info container
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(itemObj.transform, false);
            RectTransform infoRect = infoObj.AddComponent<RectTransform>();
            VerticalLayoutGroup infoLayout = infoObj.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 5;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = false;

            // Save name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(infoObj.transform, false);
            TMP_Text nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = saveInfo.saveData.saveName;
            nameText.fontSize = 18;
            nameText.fontStyle = FontStyles.Bold;

            // Timestamp
            GameObject timeObj = new GameObject("Timestamp");
            timeObj.transform.SetParent(infoObj.transform, false);
            TMP_Text timeText = timeObj.AddComponent<TextMeshProUGUI>();
            timeText.text = saveInfo.saveData.timestamp;
            timeText.fontSize = 14;

            // Chapter
            GameObject chapterObj = new GameObject("Chapter");
            chapterObj.transform.SetParent(infoObj.transform, false);
            TMP_Text chapterText = chapterObj.AddComponent<TextMeshProUGUI>();
            chapterText.text = $"Chapter: {saveInfo.saveData.currentChapterName}";
            chapterText.fontSize = 14;

            // Save type
            GameObject typeObj = new GameObject("Type");
            typeObj.transform.SetParent(infoObj.transform, false);
            TMP_Text typeText = typeObj.AddComponent<TextMeshProUGUI>();
            typeText.text = saveInfo.saveData.saveType.ToString();
            typeText.fontSize = 12;
            typeText.color = GetSaveTypeColor(saveInfo.saveData.saveType);

            return itemObj;
        }

        private void SetupSaveItem(GameObject itemObj, SaveFileInfo saveInfo)
        {
            // First, try to use SaveItemUI component if it exists (preferred method)
            SaveItemUI saveItemUI = itemObj.GetComponent<SaveItemUI>();
            if (saveItemUI == null)
            {
                // Try to find in children
                saveItemUI = itemObj.GetComponentInChildren<SaveItemUI>();
            }

            if (saveItemUI != null)
            {
                // Use the helper component - it handles everything
                saveItemUI.Populate(saveInfo, OnSaveItemClicked);
                return;
            }

            // Fallback: Try to find components manually (for backwards compatibility)
            Debug.LogWarning("SaveLoadUI: SaveItemPrefab doesn't have SaveItemUI component. Please add SaveItemUI component to your prefab and assign all UI references for better reliability.");

            // Find UI elements
            Image screenshotImg = itemObj.GetComponentInChildren<Image>();
            TMP_Text[] allTexts = itemObj.GetComponentsInChildren<TMP_Text>();

            // Load screenshot - find the screenshot image specifically (not the background)
            // Look for an Image in a child named "Screenshot" or the first Image that's not the root
            Image screenshotImage = null;
            if (screenshotImg != null)
            {
                // Check if this is likely the screenshot (not background)
                Transform screenshotTransform = itemObj.transform.Find("Screenshot");
                if (screenshotTransform != null)
                {
                    screenshotImage = screenshotTransform.GetComponent<Image>();
                }
                
                // If not found by name, use the first Image that's not on the root
                if (screenshotImage == null)
                {
                    Image[] allImages = itemObj.GetComponentsInChildren<Image>();
                    foreach (Image img in allImages)
                    {
                        if (img.gameObject != itemObj && img.gameObject.name.ToLower().Contains("screenshot"))
                        {
                            screenshotImage = img;
                            break;
                        }
                    }
                    // If still not found, use the first child Image
                    if (screenshotImage == null && allImages.Length > 1)
                    {
                        screenshotImage = allImages[1]; // Skip index 0 (root background)
                    }
                }
            }

            if (screenshotImage != null && !string.IsNullOrEmpty(saveInfo.saveData.screenshotPath))
            {
                string screenshotPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(saveInfo.filePath),
                    saveInfo.saveData.screenshotPath);
                
                if (System.IO.File.Exists(screenshotPath))
                {
                    Texture2D screenshot = Riftbourne.Utils.ScreenshotCapture.LoadScreenshot(screenshotPath);
                    if (screenshot != null)
                    {
                        Sprite sprite = Sprite.Create(screenshot, new Rect(0, 0, screenshot.width, screenshot.height), new Vector2(0.5f, 0.5f));
                        screenshotImage.sprite = sprite;
                    }
                }
            }

            // Update text fields - try to find by GameObject name
            foreach (TMP_Text text in allTexts)
            {
                if (text == null) continue;
                
                string objName = text.gameObject.name.ToLower();
                
                // Try to match by GameObject name
                if (objName.Contains("name") || objName.Contains("savename"))
                {
                    text.text = saveInfo.saveData.saveName;
                    text.fontStyle = FontStyles.Bold;
                }
                else if (objName.Contains("time") || objName.Contains("timestamp") || objName.Contains("date"))
                {
                    text.text = saveInfo.saveData.timestamp;
                }
                else if (objName.Contains("chapter"))
                {
                    text.text = $"Chapter: {saveInfo.saveData.currentChapterName}";
                }
                else if (objName.Contains("type") || objName.Contains("savetype"))
                {
                    text.text = saveInfo.saveData.saveType.ToString();
                    text.color = GetSaveTypeColor(saveInfo.saveData.saveType);
                }
            }

            // Setup button
            Button button = itemObj.GetComponent<Button>();
            if (button == null)
            {
                button = itemObj.GetComponentInChildren<Button>();
            }
            
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnSaveItemClicked(saveInfo));
            }
            else
            {
                Debug.LogWarning("SaveLoadUI: No Button found on save item. Save item won't be clickable.");
            }
        }

        private void OnSaveItemClicked(SaveFileInfo saveInfo)
        {
            if (SaveManager.Instance == null)
            {
                if (loadStatusText != null)
                {
                    loadStatusText.text = "Error: SaveManager not found!";
                }
                return;
            }

            bool success = SaveManager.Instance.LoadGame(saveInfo.filePath);
            if (success)
            {
                // Close load panel and system menu
                Hide();
                var systemMenu = FindFirstObjectByType<SystemMenuUI>();
                if (systemMenu != null)
                {
                    systemMenu.CloseMenu();
                }
            }
            else
            {
                if (loadStatusText != null)
                {
                    loadStatusText.text = "Failed to load game";
                    loadStatusText.color = Color.red;
                }
            }
        }

        private Color GetSaveTypeColor(SaveType saveType)
        {
            switch (saveType)
            {
                case SaveType.Autosave:
                    return Color.cyan;
                case SaveType.Quicksave:
                    return Color.yellow;
                case SaveType.Manual:
                    return Color.white;
                default:
                    return Color.gray;
            }
        }
    }
}
