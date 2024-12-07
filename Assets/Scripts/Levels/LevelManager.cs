using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Forever.Core;

namespace Forever.Levels
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [System.Serializable]
        public class LevelData
        {
            public string sceneName;
            public string levelName;
            public string description;
            public Sprite levelIcon;
            public bool isUnlocked;
        }

        [Header("Level Configuration")]
        public List<LevelData> levels = new List<LevelData>();
        public int currentLevelIndex = -1;

        [Header("Level Progress")]
        public float levelProgress = 0f;
        public bool isLevelComplete = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLevels();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeLevels()
        {
            // Initialize the Whispering Woods level
            var whisperingWoods = new LevelData
            {
                sceneName = "WhisperingWoods",
                levelName = "The Whispering Woods of Xylia",
                description = "A lush, vibrant forest with towering trees, glowing flora, and hidden pathways.",
                isUnlocked = true
            };
            levels.Add(whisperingWoods);

            // Add other levels (locked initially)
            var crystalCaverns = new LevelData
            {
                sceneName = "CrystalCaverns",
                levelName = "The Crystal Caverns of Kryos",
                description = "A network of icy caves filled with shimmering crystals and frozen waterfalls.",
                isUnlocked = false
            };
            levels.Add(crystalCaverns);

            // Add more levels as needed...
        }

        public void LoadLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= levels.Count)
                return;

            if (!levels[levelIndex].isUnlocked)
                return;

            currentLevelIndex = levelIndex;
            levelProgress = 0f;
            isLevelComplete = false;

            // Update game state
            GameManager.Instance.currentGameState = GameState.Playing;

            // Load the level scene
            SceneManager.LoadScene(levels[levelIndex].sceneName);
        }

        public void UpdateProgress(float progress)
        {
            levelProgress = Mathf.Clamp01(progress);
            
            // Check if level is complete
            if (levelProgress >= 1f && !isLevelComplete)
            {
                CompleteLevel();
            }
        }

        private void CompleteLevel()
        {
            isLevelComplete = true;

            // Unlock next level if available
            if (currentLevelIndex + 1 < levels.Count)
            {
                levels[currentLevelIndex + 1].isUnlocked = true;
            }

            // TODO: Show level complete UI
            // TODO: Save progress
        }

        public LevelData GetCurrentLevel()
        {
            if (currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
            {
                return levels[currentLevelIndex];
            }
            return null;
        }

        public void RestartLevel()
        {
            if (currentLevelIndex >= 0)
            {
                LoadLevel(currentLevelIndex);
            }
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex >= 0 && levelIndex < levels.Count)
            {
                return levels[levelIndex].isUnlocked;
            }
            return false;
        }
    }
} 