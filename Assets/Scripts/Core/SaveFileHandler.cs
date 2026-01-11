using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Riftbourne.Core
{
    /// <summary>
    /// Utility class for handling save file I/O operations.
    /// Handles JSON serialization, file paths, and save metadata.
    /// </summary>
    public static class SaveFileHandler
    {
        private const string SAVES_DIRECTORY = "Saves";
        private const string AUTOSAVE_DIRECTORY = "Autosave";
        private const string QUICKSAVE_DIRECTORY = "Quicksave";
        private const string MANUAL_DIRECTORY = "Manual";
        private const string SCREENSHOT_DIRECTORY = "Screenshots";

        /// <summary>
        /// Get the base saves directory path.
        /// </summary>
        public static string GetSavesDirectory()
        {
            return Path.Combine(Application.persistentDataPath, SAVES_DIRECTORY);
        }

        /// <summary>
        /// Get the directory path for a specific save type.
        /// </summary>
        public static string GetSaveTypeDirectory(SaveType saveType)
        {
            string baseDir = GetSavesDirectory();
            switch (saveType)
            {
                case SaveType.Autosave:
                    return Path.Combine(baseDir, AUTOSAVE_DIRECTORY);
                case SaveType.Quicksave:
                    return Path.Combine(baseDir, QUICKSAVE_DIRECTORY);
                case SaveType.Manual:
                    return Path.Combine(baseDir, MANUAL_DIRECTORY);
                default:
                    return baseDir;
            }
        }

        /// <summary>
        /// Get the screenshot directory path for a save type.
        /// </summary>
        public static string GetScreenshotDirectory(SaveType saveType)
        {
            return Path.Combine(GetSaveTypeDirectory(saveType), SCREENSHOT_DIRECTORY);
        }

        /// <summary>
        /// Ensure save directories exist.
        /// </summary>
        public static void EnsureDirectoriesExist(SaveType saveType)
        {
            string saveDir = GetSaveTypeDirectory(saveType);
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }

            string screenshotDir = GetScreenshotDirectory(saveType);
            if (!Directory.Exists(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
            }
        }

        /// <summary>
        /// Generate a save file path for a slot (for autosave/quicksave).
        /// </summary>
        public static string GetSaveFilePath(SaveType saveType, int slot)
        {
            EnsureDirectoriesExist(saveType);
            string fileName = $"save_{slot:D3}.json";
            return Path.Combine(GetSaveTypeDirectory(saveType), fileName);
        }

        /// <summary>
        /// Generate a save file path for a manual save with custom name.
        /// </summary>
        public static string GetManualSaveFilePath(string saveName)
        {
            EnsureDirectoriesExist(SaveType.Manual);
            // Sanitize filename
            string sanitizedName = SanitizeFileName(saveName);
            string fileName = $"{sanitizedName}.json";
            return Path.Combine(GetSaveTypeDirectory(SaveType.Manual), fileName);
        }

        /// <summary>
        /// Generate a screenshot file path for a save.
        /// </summary>
        public static string GetScreenshotFilePath(SaveType saveType, int slot, string saveName = null)
        {
            EnsureDirectoriesExist(saveType);
            string fileName;
            if (saveType == SaveType.Manual && !string.IsNullOrEmpty(saveName))
            {
                string sanitizedName = SanitizeFileName(saveName);
                fileName = $"{sanitizedName}.png";
            }
            else
            {
                fileName = $"save_{slot:D3}.png";
            }
            return Path.Combine(GetScreenshotDirectory(saveType), fileName);
        }

        /// <summary>
        /// Get screenshot path relative to save directory (for storing in SaveData).
        /// </summary>
        public static string GetRelativeScreenshotPath(SaveType saveType, int slot, string saveName = null)
        {
            string fileName;
            if (saveType == SaveType.Manual && !string.IsNullOrEmpty(saveName))
            {
                string sanitizedName = SanitizeFileName(saveName);
                fileName = $"{sanitizedName}.png";
            }
            else
            {
                fileName = $"save_{slot:D3}.png";
            }
            return Path.Combine(SCREENSHOT_DIRECTORY, fileName);
        }

        /// <summary>
        /// Save SaveData to JSON file.
        /// </summary>
        public static bool SaveToFile(SaveData data, string filePath)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(filePath, json);
                Debug.Log($"SaveFileHandler: Saved game to {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveFileHandler: Failed to save game to {filePath}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load SaveData from JSON file.
        /// </summary>
        public static SaveData LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"SaveFileHandler: Save file not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"SaveFileHandler: Loaded game from {filePath}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveFileHandler: Failed to load game from {filePath}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get all save files for a specific save type.
        /// </summary>
        public static List<SaveFileInfo> GetSaveFiles(SaveType saveType)
        {
            List<SaveFileInfo> saves = new List<SaveFileInfo>();
            string directory = GetSaveTypeDirectory(saveType);

            if (!Directory.Exists(directory))
            {
                return saves;
            }

            string[] files = Directory.GetFiles(directory, "*.json");
            foreach (string filePath in files)
            {
                try
                {
                    SaveData data = LoadFromFile(filePath);
                    if (data != null)
                    {
                        saves.Add(new SaveFileInfo
                        {
                            filePath = filePath,
                            saveData = data,
                            fileName = Path.GetFileName(filePath)
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"SaveFileHandler: Failed to read save file {filePath}: {e.Message}");
                }
            }

            // Sort by timestamp (newest first)
            saves.Sort((a, b) =>
            {
                DateTime timeA, timeB;
                if (DateTime.TryParse(a.saveData.timestamp, out timeA) && DateTime.TryParse(b.saveData.timestamp, out timeB))
                {
                    return timeB.CompareTo(timeA); // Descending order
                }
                return 0;
            });

            return saves;
        }

        /// <summary>
        /// Delete a save file.
        /// </summary>
        public static bool DeleteSave(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"SaveFileHandler: Deleted save file: {filePath}");

                    // Also try to delete associated screenshot
                    string screenshotPath = filePath.Replace(".json", ".png").Replace(Path.GetDirectoryName(filePath), 
                        Path.Combine(Path.GetDirectoryName(filePath), SCREENSHOT_DIRECTORY));
                    if (File.Exists(screenshotPath))
                    {
                        File.Delete(screenshotPath);
                    }

                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveFileHandler: Failed to delete save file {filePath}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get save file metadata without loading full data.
        /// </summary>
        public static SaveFileInfo GetSaveFileInfo(string filePath)
        {
            SaveData data = LoadFromFile(filePath);
            if (data != null)
            {
                return new SaveFileInfo
                {
                    filePath = filePath,
                    saveData = data,
                    fileName = Path.GetFileName(filePath)
                };
            }
            return null;
        }

        /// <summary>
        /// Sanitize a filename to remove invalid characters.
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return "Save";
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = fileName;
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            // Limit length
            if (sanitized.Length > 100)
            {
                sanitized = sanitized.Substring(0, 100);
            }

            return sanitized;
        }
    }

    /// <summary>
    /// Information about a save file.
    /// </summary>
    public class SaveFileInfo
    {
        public string filePath;
        public SaveData saveData;
        public string fileName;
    }
}
