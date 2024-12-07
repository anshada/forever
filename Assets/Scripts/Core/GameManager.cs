using UnityEngine;
using System;
using System.Collections.Generic;
using Forever.Characters;

namespace Forever.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Character Management")]
        public List<Character> availableCharacters;
        public Character currentCharacter;

        [Header("Game State")]
        public GameState currentGameState;
        public event Action<GameState> OnGameStateChanged;

        [Header("Game Settings")]
        public float globalVolume = 1f;
        public bool isFullscreen = true;
        public int targetFrameRate = 60;

        [Header("Player Progress")]
        public int playerLevel = 1;
        public float playerExperience = 0f;
        public int currency = 0;
        private Dictionary<string, bool> gameFlags = new Dictionary<string, bool>();
        private Dictionary<string, float> relationships = new Dictionary<string, float>();

        private GameState previousGameState;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            Application.targetFrameRate = targetFrameRate;
            Screen.fullScreen = isFullscreen;
        }

        private void Update()
        {
            // Check for game state changes
            if (currentGameState != previousGameState)
            {
                OnGameStateChanged?.Invoke(currentGameState);
                previousGameState = currentGameState;
            }

            // Handle pause input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        private void InitializeGame()
        {
            currentGameState = GameState.MainMenu;
            previousGameState = GameState.MainMenu;
            
            // Initialize character list if empty
            if (availableCharacters == null)
            {
                availableCharacters = new List<Character>();
            }
        }

        public void SwitchCharacter(Character newCharacter)
        {
            if (currentGameState != GameState.Playing) return;
            
            if (currentCharacter != null)
            {
                currentCharacter.Deactivate();
            }

            currentCharacter = newCharacter;
            currentCharacter.Activate();
        }

        public void SwitchCharacter(int characterIndex)
        {
            if (characterIndex >= 0 && characterIndex < availableCharacters.Count)
            {
                SwitchCharacter(availableCharacters[characterIndex]);
            }
        }

        public void TogglePause()
        {
            if (currentGameState == GameState.Playing)
            {
                SetGameState(GameState.Paused);
                Time.timeScale = 0f;
            }
            else if (currentGameState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
                Time.timeScale = 1f;
            }
        }

        public void SetGameState(GameState newState)
        {
            currentGameState = newState;
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public void UpdateSettings(float volume, bool fullscreen, int fps)
        {
            globalVolume = volume;
            isFullscreen = fullscreen;
            targetFrameRate = fps;

            // Apply settings
            Screen.fullScreen = isFullscreen;
            Application.targetFrameRate = targetFrameRate;
            // TODO: Implement volume control system
        }

        public void SetGameFlag(string flagName)
        {
            gameFlags[flagName] = true;
        }

        public bool GetGameFlag(string flagName)
        {
            return gameFlags.TryGetValue(flagName, out bool value) && value;
        }

        public void UpdateRelationship(string characterId, float change = 0.1f)
        {
            if (!relationships.ContainsKey(characterId))
            {
                relationships[characterId] = 0f;
            }
            relationships[characterId] = Mathf.Clamp01(relationships[characterId] + change);
        }

        public void AddInventoryItem(string itemId)
        {
            // Delegate to inventory system
            InventorySystem.Instance?.AddItem(itemId);
        }

        public void GainExperience(float amount)
        {
            playerExperience += amount;
            CheckLevelUp();
        }

        public void GainCurrency(int amount)
        {
            currency += amount;
        }

        private void CheckLevelUp()
        {
            float experienceForNextLevel = playerLevel * 1000f; // Simple level scaling
            if (playerExperience >= experienceForNextLevel)
            {
                playerLevel++;
                playerExperience -= experienceForNextLevel;
                // Trigger level up effects
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Cutscene
    }
} 