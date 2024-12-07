using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Forever.Core;
using Forever.Levels;
using Forever.Characters;

namespace Forever.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Menu Panels")]
        public GameObject mainMenuPanel;
        public GameObject pauseMenuPanel;
        public GameObject levelSelectPanel;
        public GameObject gameplayHUDPanel;
        public GameObject levelCompletePanel;

        [Header("Character UI")]
        public Image[] characterPortraits;
        public Image specialAbilityCooldown;
        public TextMeshProUGUI currentCharacterName;

        [Header("Level UI")]
        public Slider progressBar;
        public TextMeshProUGUI levelNameText;
        public TextMeshProUGUI objectiveText;

        [Header("Animation")]
        public float fadeSpeed = 0.5f;
        public CanvasGroup fadePanel;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Subscribe to events
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            
            // Initialize UI
            ShowMainMenu();
        }

        private void Update()
        {
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (GameManager.Instance.currentGameState != GameState.Playing)
                return;

            // Update character info
            Character currentCharacter = GameManager.Instance.currentCharacter;
            if (currentCharacter != null)
            {
                currentCharacterName.text = currentCharacter.characterName;
                specialAbilityCooldown.fillAmount = currentCharacter.currentCooldown / currentCharacter.specialAbilityCooldown;
            }

            // Update level progress
            if (LevelManager.Instance != null)
            {
                progressBar.value = LevelManager.Instance.levelProgress;
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    ShowMainMenu();
                    break;
                case GameState.Playing:
                    ShowGameplayHUD();
                    break;
                case GameState.Paused:
                    ShowPauseMenu();
                    break;
                case GameState.Cutscene:
                    HideAllPanels();
                    break;
            }
        }

        public void ShowMainMenu()
        {
            HideAllPanels();
            mainMenuPanel.SetActive(true);
        }

        public void ShowLevelSelect()
        {
            HideAllPanels();
            levelSelectPanel.SetActive(true);
            PopulateLevelSelect();
        }

        public void ShowGameplayHUD()
        {
            HideAllPanels();
            gameplayHUDPanel.SetActive(true);
            UpdateLevelInfo();
        }

        public void ShowPauseMenu()
        {
            pauseMenuPanel.SetActive(true);
        }

        public void ShowLevelComplete()
        {
            levelCompletePanel.SetActive(true);
        }

        private void HideAllPanels()
        {
            mainMenuPanel.SetActive(false);
            pauseMenuPanel.SetActive(false);
            levelSelectPanel.SetActive(false);
            gameplayHUDPanel.SetActive(false);
            levelCompletePanel.SetActive(false);
        }

        private void PopulateLevelSelect()
        {
            // TODO: Populate level select UI with available levels
        }

        private void UpdateLevelInfo()
        {
            var currentLevel = LevelManager.Instance.GetCurrentLevel();
            if (currentLevel != null)
            {
                levelNameText.text = currentLevel.levelName;
                // TODO: Update objective text based on current level goals
            }
        }

        public void OnPlayButtonClicked()
        {
            ShowLevelSelect();
        }

        public void OnLevelSelected(int levelIndex)
        {
            if (LevelManager.Instance.IsLevelUnlocked(levelIndex))
            {
                StartCoroutine(FadeAndLoadLevel(levelIndex));
            }
        }

        private System.Collections.IEnumerator FadeAndLoadLevel(int levelIndex)
        {
            // Fade out
            float alpha = 0f;
            while (alpha < 1f)
            {
                alpha += Time.deltaTime * fadeSpeed;
                fadePanel.alpha = alpha;
                yield return null;
            }

            // Load level
            LevelManager.Instance.LoadLevel(levelIndex);

            // Fade in
            while (alpha > 0f)
            {
                alpha -= Time.deltaTime * fadeSpeed;
                fadePanel.alpha = alpha;
                yield return null;
            }
        }

        public void OnPauseButtonClicked()
        {
            if (GameManager.Instance.currentGameState == GameState.Playing)
            {
                GameManager.Instance.currentGameState = GameState.Paused;
                Time.timeScale = 0f;
            }
            else if (GameManager.Instance.currentGameState == GameState.Paused)
            {
                GameManager.Instance.currentGameState = GameState.Playing;
                Time.timeScale = 1f;
            }
        }

        public void OnMainMenuButtonClicked()
        {
            Time.timeScale = 1f;
            GameManager.Instance.currentGameState = GameState.MainMenu;
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
    }
} 