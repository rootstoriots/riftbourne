using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Riftbourne.Core;

namespace Riftbourne.UI
{
    /// <summary>
    /// Helper component for save item UI elements.
    /// Attach this to the SaveItemPrefab and assign all UI element references.
    /// This makes it easy to populate save item data without relying on name matching.
    /// </summary>
    public class SaveItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image screenshotImage;
        [SerializeField] private TMP_Text saveNameText;
        [SerializeField] private TMP_Text timestampText;
        [SerializeField] private TMP_Text chapterText;
        [SerializeField] private TMP_Text saveTypeText;
        [SerializeField] private Button loadButton;

        /// <summary>
        /// Populate this save item with save data.
        /// </summary>
        public void Populate(SaveFileInfo saveInfo, System.Action<SaveFileInfo> onLoadClicked)
        {
            if (saveInfo == null || saveInfo.saveData == null)
            {
                Debug.LogError("SaveItemUI: Cannot populate with null saveInfo!");
                return;
            }

            // Update screenshot
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

            // Update text fields
            if (saveNameText != null)
            {
                saveNameText.text = saveInfo.saveData.saveName;
                saveNameText.fontStyle = FontStyles.Bold;
            }

            if (timestampText != null)
            {
                timestampText.text = saveInfo.saveData.timestamp;
            }

            if (chapterText != null)
            {
                chapterText.text = $"Chapter: {saveInfo.saveData.currentChapterName}";
            }

            if (saveTypeText != null)
            {
                saveTypeText.text = saveInfo.saveData.saveType.ToString();
                saveTypeText.color = GetSaveTypeColor(saveInfo.saveData.saveType);
            }

            // Setup button
            Button buttonToUse = loadButton;
            if (buttonToUse == null)
            {
                // Try to find button on this GameObject or children
                buttonToUse = GetComponent<Button>();
                if (buttonToUse == null)
                {
                    buttonToUse = GetComponentInChildren<Button>();
                }
            }

            if (buttonToUse != null && onLoadClicked != null)
            {
                buttonToUse.onClick.RemoveAllListeners();
                buttonToUse.onClick.AddListener(() => onLoadClicked(saveInfo));
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
