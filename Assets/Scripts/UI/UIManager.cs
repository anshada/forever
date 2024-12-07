using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using Forever.Core;
using Forever.Characters;
using Forever.Audio;
using System.Collections.Generic;
using System.Linq;

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
        public GameObject choicesContainer;
        public DialogueChoiceButton dialogueChoiceButtonPrefab;
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
        
        public void ShowDialoguePanel(bool show)
        {
            ShowPanel(dialoguePanel, show);
        }
        
        public void UpdateDialogueText(string text)
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
            }
        }
        
        public void ShowDialogueChoices(DialogueChoice[] choices)
        {
            if (choicesContainer == null || dialogueChoiceButtonPrefab == null) return;
            
            // Clear existing choices
            foreach (Transform child in choicesContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Create new choice buttons
            for (int i = 0; i < choices.Length; i++)
            {
                DialogueChoiceButton button = Instantiate(dialogueChoiceButtonPrefab, choicesContainer.transform);
                button.SetChoice(choices[i], i);
            }
            
            choicesContainer.SetActive(true);
        }
        
        #endregion
        
        #region Quest UI
        
        public void ShowQuestStarted(Quest quest)
        {
            ShowNotification($"New Quest: {quest.questName}", NotificationType.Quest);
            UpdateQuestLog();
        }
        
        public void ShowQuestCompleted(Quest quest)
        {
            ShowNotification($"Quest Completed: {quest.questName}", NotificationType.Achievement);
            UpdateQuestLog();
        }
        
        private void UpdateQuestLog()
        {
            if (questLogContent == null || questEntryPrefab == null || questSystem == null) return;
            
            // Clear existing entries
            foreach (Transform child in questLogContent)
            {
                Destroy(child.gameObject);
            }
            
            // Add active quests
            var activeQuests = questSystem.GetActiveQuests();
            foreach (var quest in activeQuests)
            {
                var entry = Instantiate(questEntryPrefab, questLogContent);
                float progress = questSystem.GetQuestState(quest.questId)?.objectives.Values.Average() ?? 0f;
                entry.SetQuestInfo(quest.questName, quest.description, progress);
            }
        }
        
        #endregion
        
        #region Notifications
        
        public void ShowNotification(string message, NotificationType type)
        {
            // Convert NotificationType to UISoundType
            UISoundType soundType = type switch
            {
                NotificationType.Quest => UISoundType.QuestAccept,
                NotificationType.Achievement => UISoundType.Success,
                NotificationType.Warning => UISoundType.Warning,
                NotificationType.Error => UISoundType.Error,
                _ => UISoundType.NotificationShow
            };

            // Play sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUISound(soundType);
            }

            // Show notification UI
            if (notificationPanel != null)
            {
                StartCoroutine(ShowNotificationCoroutine(message, type));
            }
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
        
        private void ShowQuestNotification(string message)
        {
            ShowNotification(message, NotificationType.Quest);
        }
        
        #endregion
        
        private void HandleQuestStarted(Quest quest)
        {
            if (quest != null)
            {
                ShowQuestNotification($"New Quest: {quest.questName}");
                UpdateQuestLog();
            }
        }

        private void HandleQuestCompleted(Quest quest)
        {
            if (quest != null)
            {
                ShowQuestNotification($"Quest Completed: {quest.questName}");
                UpdateQuestLog();
            }
        }

        private void HandleQuestProgress(Quest quest, float progress)
        {
            UpdateQuestLog();
            if (progress >= 1f)
            {
                AudioManager.Instance?.PlayUISound(UISoundType.ObjectiveComplete);
            }
        }

        private void HandleDialogueNodeStart(DialogueNode node)
        {
            if (node != null)
            {
                ShowDialoguePanel(true);
                UpdateDialogueText(node.text);
                AudioManager.Instance?.PlayUISound(UISoundType.DialogueStart);
            }
        }

        private void HandleDialogueNodeEnd(DialogueNode node)
        {
            ShowDialoguePanel(false);
            AudioManager.Instance?.PlayUISound(UISoundType.DialogueEnd);
        }

        private void HandleDialogueChoice(DialogueChoice choice)
        {
            if (choice != null)
            {
                dialogueSystem.SelectChoice(choice);
                PlayUISound(UISoundType.DialogueChoice);
            }
        }

        public void ShowDialogueUI(bool show)
        {
            ShowPanel(dialoguePanel, show);
        }

        public void ShowDialogueText(string text, string speakerName)
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
            }
            if (speakerNameText != null)
            {
                speakerNameText.text = speakerName;
            }
        }

        public void ShowEventNotification(string eventId)
        {
            ShowNotification($"Event Started: {eventId}", NotificationType.Info);
        }

        public void ShowEventCompletion(string eventId)
        {
            ShowNotification($"Event Completed: {eventId}", NotificationType.Achievement);
        }

        public void ShowEventFailure(string eventId)
        {
            ShowNotification($"Event Failed: {eventId}", NotificationType.Error);
        }
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
        Info,
        Quest,
        Achievement,
        Warning,
        Error
    }
} 