using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Forever.Levels;

namespace Forever.Core
{
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        private const string SAVE_FILE = "forever_save.json";
        private const string SETTINGS_FILE = "forever_settings.json";

        [Serializable]
        public class GameSaveData
        {
            public List<LevelSaveData> levels = new List<LevelSaveData>();
            public Dictionary<string, float> bestTimes = new Dictionary<string, float>();
            public int totalCollectibles;
            public float playtime;
            public DateTime lastSaveTime;
        }

        [Serializable]
        public class LevelSaveData
        {
            public string levelName;
            public bool isUnlocked;
            public bool isCompleted;
            public float bestTime;
            public int collectiblesFound;
            public List<string> unlockedSecrets;
        }

        [Serializable]
        public class GameSettings
        {
            public float masterVolume = 1f;
            public float musicVolume = 1f;
            public float sfxVolume = 1f;
            public bool isFullscreen = true;
            public int targetFrameRate = 60;
            public int qualityLevel = 2;
            public Dictionary<string, KeyCode> keyBindings = new Dictionary<string, KeyCode>();
        }

        private GameSaveData currentSave;
        private GameSettings currentSettings;
        private string savePath;
        private string settingsPath;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePaths();
                LoadSettings();
                LoadGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePaths()
        {
            savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            settingsPath = Path.Combine(Application.persistentDataPath, SETTINGS_FILE);
        }

        public void SaveGame()
        {
            try
            {
                currentSave = new GameSaveData
                {
                    lastSaveTime = DateTime.Now,
                    playtime = Time.time
                };

                // Save level data
                foreach (var level in LevelManager.Instance.levels)
                {
                    var levelData = new LevelSaveData
                    {
                        levelName = level.levelName,
                        isUnlocked = level.isUnlocked,
                        isCompleted = false, // TODO: Implement completion tracking
                        collectiblesFound = 0 // TODO: Implement collectibles
                    };
                    currentSave.levels.Add(levelData);
                }

                string json = JsonUtility.ToJson(currentSave, true);
                File.WriteAllText(savePath, json);
                Debug.Log("Game saved successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }

        public void LoadGame()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    string json = File.ReadAllText(savePath);
                    currentSave = JsonUtility.FromJson<GameSaveData>(json);

                    // Apply saved data to game state
                    foreach (var levelData in currentSave.levels)
                    {
                        var level = LevelManager.Instance.levels.Find(l => l.levelName == levelData.levelName);
                        if (level != null)
                        {
                            level.isUnlocked = levelData.isUnlocked;
                        }
                    }

                    Debug.Log("Game loaded successfully!");
                }
                else
                {
                    currentSave = new GameSaveData();
                    SaveGame(); // Create initial save file
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                currentSave = new GameSaveData();
            }
        }

        public void SaveSettings()
        {
            try
            {
                string json = JsonUtility.ToJson(currentSettings, true);
                File.WriteAllText(settingsPath, json);
                
                // Apply settings
                GameManager.Instance.UpdateSettings(
                    currentSettings.masterVolume,
                    currentSettings.isFullscreen,
                    currentSettings.targetFrameRate
                );

                Debug.Log("Settings saved successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save settings: {e.Message}");
            }
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    currentSettings = JsonUtility.FromJson<GameSettings>(json);
                }
                else
                {
                    currentSettings = new GameSettings();
                    InitializeDefaultKeyBindings();
                    SaveSettings();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load settings: {e.Message}");
                currentSettings = new GameSettings();
                InitializeDefaultKeyBindings();
            }
        }

        private void InitializeDefaultKeyBindings()
        {
            currentSettings.keyBindings.Clear();
            currentSettings.keyBindings.Add("Jump", KeyCode.Space);
            currentSettings.keyBindings.Add("Interact", KeyCode.F);
            currentSettings.keyBindings.Add("SpecialAbility", KeyCode.E);
            currentSettings.keyBindings.Add("Pause", KeyCode.Escape);
        }

        public void UpdateSetting<T>(string settingName, T value)
        {
            var settingsType = typeof(GameSettings);
            var property = settingsType.GetProperty(settingName);
            if (property != null)
            {
                property.SetValue(currentSettings, value);
                SaveSettings();
            }
        }

        public GameSettings GetSettings()
        {
            return currentSettings;
        }

        public void DeleteSaveData()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    currentSave = new GameSaveData();
                    Debug.Log("Save data deleted successfully!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save data: {e.Message}");
            }
        }

        private void OnApplicationQuit()
        {
            SaveGame();
            SaveSettings();
        }

        public T GetSavedData<T>(string key) where T : class
        {
            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json))
                return null;
            return JsonUtility.FromJson<T>(json);
        }

        public void SaveData<T>(string key, T data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public void SaveEventState(string eventId, bool completed)
        {
            PlayerPrefs.SetInt($"Event_{eventId}", completed ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
} 