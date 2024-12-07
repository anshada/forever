using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using Forever.Core;
using Forever.Characters;

namespace Forever.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        public GameObject mainMenuPanel;
        public GameObject hudPanel;
        public GameObject pausePanel;
        public GameObject inventoryPanel;
        public GameObject questPanel;
        public GameObject dialoguePanel;
        public GameObject characterPanel;
        public GameObject settingsPanel;
        public GameObject mapPanel;
        
        [Header("HUD Elements")]
        public HealthBar[] characterHealthBars;
        public Image magicEnergyBar;
        public TextMeshProUGUI currentObjectiveText;
        public GameObject interactionPrompt;
        public GameObject notificationPanel;
        public Image weatherIndicator;
        public Image compassIndicator;
        
        [Header("Dialogue UI")]
        public TextMeshProUGUI dialogueText;
        public TextMeshProUGUI speakerNameText;
        public GameObject choicesPanel;
        public DialogueChoiceButton choiceButtonPrefab;
        public Image speakerPortrait;
        public Image listenerPortrait;
        
        [Header("Quest UI")]
        public QuestLogEntry questEntryPrefab;
        public Transform questLogContent;
        public GameObject questNotificationPrefab;
        public Transform questNotificationAnchor;
        
        [Header("Animation")]
        public float fadeSpeed = 1f;
        public float notificationDuration = 3f;
        public AnimationCurve notificationCurve;
        
        private GameManager gameManager;
        private QuestSystem questSystem;
        private DialogueSystem dialogueSystem;
        private InventorySystem inventorySystem;
        private AudioManager audioManager;
        
        private CanvasGroup mainCanvasGroup;
        private Coroutine typingCoroutine;
        private bool isTyping;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeUI();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeUI()
        {
            mainCanvasGroup = GetComponent<CanvasGroup>();
            
            // Get system references
            gameManager = FindObjectOfType<GameManager>();
            questSystem = FindObjectOfType<QuestSystem>();
            dialogueSystem = FindObjectOfType<DialogueSystem>();
            inventorySystem = FindObjectOfType<InventorySystem>();
            audioManager = FindObjectOfType<AudioManager>();
            
            // Initialize all panels
            InitializePanels();
            
            // Subscribe to events
            SubscribeToEvents();
        }
        
        private void InitializePanels()
        {
            // Set initial panel states
            ShowPanel(mainMenuPanel, true);
            ShowPanel(hudPanel, false);
            ShowPanel(pausePanel, false);
            ShowPanel(inventoryPanel, false);
            ShowPanel(questPanel, false);
            ShowPanel(dialoguePanel, false);
            ShowPanel(characterPanel, false);
            ShowPanel(settingsPanel, false);
            ShowPanel(mapPanel, false);
            
            // Initialize HUD elements
            UpdateHealthBars();
            UpdateMagicEnergy(1f);
            SetCurrentObjective("");
            ShowInteractionPrompt(false);
        }
        
        private void SubscribeToEvents()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += HandleGameStateChanged;
            }
            
            if (questSystem != null)
            {
                questSystem.OnQuestStarted += HandleQuestStarted;
                questSystem.OnQuestCompleted += HandleQuestCompleted;
                questSystem.OnQuestProgress += HandleQuestProgress;
            }
            
            if (dialogueSystem != null)
            {
                dialogueSystem.OnDialogueNodeStart += HandleDialogueNodeStart;
                dialogueSystem.OnDialogueNodeEnd += HandleDialogueNodeEnd;
                dialogueSystem.OnDialogueChoice += HandleDialogueChoice;
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
                    ShowGameUI();
                    break;
                case GameState.Paused:
                    ShowPauseMenu();
                    break;
                case GameState.Cutscene:
                    ShowCutsceneUI();
                    break;
            }
        }
        
        public void ShowMainMenu()
        {
            ShowPanel(mainMenuPanel, true);
            ShowPanel(hudPanel, false);
            ShowPanel(pausePanel, false);
        }
        
        public void ShowGameUI()
        {
            ShowPanel(mainMenuPanel, false);
            ShowPanel(hudPanel, true);
            ShowPanel(pausePanel, false);
        }
        
        public void ShowPauseMenu()
        {
            ShowPanel(pausePanel, true);
        }
        
        public void ShowCutsceneUI()
        {
            ShowPanel(hudPanel, false);
            // Show any cutscene-specific UI elements
        }
        
        private void ShowPanel(GameObject panel, bool show)
        {
            if (panel == null) return;
            
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                StartCoroutine(FadePanel(canvasGroup, show ? 1f : 0f));
            }
            else
            {
                panel.SetActive(show);
            }
        }
        
        private IEnumerator FadePanel(CanvasGroup canvasGroup, float targetAlpha)
        {
            canvasGroup.interactable = targetAlpha > 0;
            canvasGroup.blocksRaycasts = targetAlpha > 0;
            
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            
            while (elapsed < fadeSpeed)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeSpeed);
                yield return null;
            }
            
            canvasGroup.alpha = targetAlpha;
        }
        
        #region HUD Updates
        
        public void UpdateHealthBars()
        {
            if (characterHealthBars == null) return;
            
            foreach (var healthBar in characterHealthBars)
            {
                if (healthBar.character != null)
                {
                    healthBar.UpdateHealth(healthBar.character.currentHealth / healthBar.character.maxHealth);
                }
            }
        }
        
        public void UpdateMagicEnergy(float normalizedValue)
        {
            if (magicEnergyBar != null)
            {
                magicEnergyBar.fillAmount = normalizedValue;
            }
        }
        
        public void SetCurrentObjective(string objective)
        {
            if (currentObjectiveText != null)
            {
                currentObjectiveText.text = objective;
            }
        }
        
        public void ShowInteractionPrompt(bool show, string promptText = "")
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(show);
                if (show && !string.IsNullOrEmpty(promptText))
                {
                    interactionPrompt.GetComponentInChildren<TextMeshProUGUI>().text = promptText;
                }
            }
        }
        
        #endregion
        
        #region Dialogue UI
        
        public void ShowDialogueUI(bool show)
        {
            ShowPanel(dialoguePanel, show);
        }
        
        public void ShowDialogueText(string text, string speakerName)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            
            if (dialogueText != null)
            {
                typingCoroutine = StartCoroutine(TypeText(text));
            }
            
            if (speakerNameText != null)
            {
                speakerNameText.text = speakerName;
            }
        }
        
        private IEnumerator TypeText(string text)
        {
            isTyping = true;
            dialogueText.text = "";
            
            foreach (char c in text)
            {
                dialogueText.text += c;
                if (c != ' ' && dialogueSystem != null)
                {
                    dialogueSystem.PlayDialogueEffects(DialogueEffectType.Typing);
                }
                yield return new WaitForSeconds(dialogueSystem.typingSpeed);
            }
            
            isTyping = false;
        }
        
        public void ShowDialogueChoices(DialogueChoice[] choices)
        {
            if (choicesPanel == null || choiceButtonPrefab == null) return;
            
            // Clear existing choices
            foreach (Transform child in choicesPanel.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Create new choice buttons
            for (int i = 0; i < choices.Length; i++)
            {
                DialogueChoiceButton button = Instantiate(choiceButtonPrefab, choicesPanel.transform);
                button.SetChoice(choices[i], i);
            }
            
            choicesPanel.SetActive(true);
        }
        
        #endregion
        
        #region Quest UI
        
        public void ShowQuestStarted(Quest quest)
        {
            ShowNotification($"New Quest: {quest.questName}", NotificationType.QuestStart);
            UpdateQuestLog();
        }
        
        public void ShowQuestCompleted(Quest quest)
        {
            ShowNotification($"Quest Completed: {quest.questName}", NotificationType.QuestComplete);
            UpdateQuestLog();
        }
        
        private void UpdateQuestLog()
        {
            if (questLogContent == null || questEntryPrefab == null) return;
            
            // Clear existing entries
            foreach (Transform child in questLogContent)
            {
                Destroy(child.gameObject);
            }
            
            // Add active quests
            Quest[] activeQuests = questSystem.GetAvailableQuests();
            foreach (var quest in activeQuests)
            {
                QuestLogEntry entry = Instantiate(questEntryPrefab, questLogContent);
                entry.Initialize(quest);
            }
        }
        
        #endregion
        
        #region Notifications
        
        public void ShowNotification(string message, NotificationType type)
        {
            if (notificationPanel == null) return;
            
            StartCoroutine(ShowNotificationCoroutine(message, type));
        }
        
        private IEnumerator ShowNotificationCoroutine(string message, NotificationType type)
        {
            GameObject notification = Instantiate(notificationPanel, notificationPanel.transform.parent);
            notification.SetActive(true);
            
            TextMeshProUGUI notificationText = notification.GetComponentInChildren<TextMeshProUGUI>();
            if (notificationText != null)
            {
                notificationText.text = message;
            }
            
            // Play notification sound
            if (audioManager != null)
            {
                audioManager.PlayUISound(type);
            }
            
            // Animate notification
            CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < notificationDuration)
                {
                    elapsed += Time.deltaTime;
                    float normalizedTime = elapsed / notificationDuration;
                    canvasGroup.alpha = notificationCurve.Evaluate(normalizedTime);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(notificationDuration);
            }
            
            Destroy(notification);
        }
        
        #endregion
    }
    
    [System.Serializable]
    public class HealthBar
    {
        public Character character;
        public Image fillImage;
        public TextMeshProUGUI valueText;
        
        public void UpdateHealth(float normalizedValue)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = normalizedValue;
            }
            
            if (valueText != null)
            {
                valueText.text = $"{Mathf.RoundToInt(normalizedValue * 100)}%";
            }
        }
    }
    
    public enum NotificationType
    {
        QuestStart,
        QuestComplete,
        ItemReceived,
        Achievement,
        Warning
    }
} 