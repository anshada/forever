using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using Forever.Core;
using Forever.Audio;

namespace Forever.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Notification Settings")]
        public GameObject notificationPanel;
        public TextMeshProUGUI notificationText;
        public float notificationDuration = 3f;
        public bool notificationAutoHide = true;
        public AnimationCurve notificationCurve;

        [Header("Dialogue UI")]
        public GameObject dialoguePanel;
        public Transform dialogueChoicesContainer;
        public DialogueChoiceButton dialogueChoicePrefab;

        [Header("Quest UI")]
        public Transform questLogContent;
        public GameObject questEntryPrefab;

        private DialogueSystem dialogueSystem;
        private QuestSystem questSystem;
        private AudioManager audioManager;

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
            dialogueSystem = DialogueSystem.Instance;
            questSystem = QuestSystem.Instance;
            audioManager = AudioManager.Instance;

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
                dialogueSystem.OnDialogueChoice += HandleDialogueChoiceSelected;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (questSystem != null)
            {
                questSystem.OnQuestStarted -= HandleQuestStarted;
                questSystem.OnQuestCompleted -= HandleQuestCompleted;
                questSystem.OnQuestProgress -= HandleQuestProgress;
            }

            if (dialogueSystem != null)
            {
                dialogueSystem.OnDialogueNodeStart -= HandleDialogueNodeStart;
                dialogueSystem.OnDialogueNodeEnd -= HandleDialogueNodeEnd;
                dialogueSystem.OnDialogueChoice -= HandleDialogueChoiceSelected;
            }
        }

        public void ShowNotification(string message, NotificationType type)
        {
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(true);
                notificationText.text = message;
                SetNotificationStyle(type);

                if (notificationAutoHide)
                {
                    CancelInvoke(nameof(HideNotification));
                    Invoke(nameof(HideNotification), notificationDuration);
                }

                PlayNotificationSound(type);
            }
        }

        private void HandleQuestStarted(Quest quest)
        {
            ShowNotification($"New Quest: {quest.title}", NotificationType.Info);
            UpdateQuestLog();
        }

        private void HandleQuestCompleted(Quest quest)
        {
            ShowNotification($"Quest Completed: {quest.title}", NotificationType.Success);
            UpdateQuestLog();
        }

        private void HandleQuestProgress(Quest quest, float progress)
        {
            ShowNotification($"Quest Progress: {progress:P0}", NotificationType.Info);
            UpdateQuestLog();
        }

        private void UpdateQuestLog()
        {
            if (questLogContent != null && questEntryPrefab != null)
            {
                // Clear existing entries
                foreach (Transform child in questLogContent)
                {
                    Destroy(child.gameObject);
                }

                // Add new entries
                var quests = questSystem.GetActiveQuests();
                foreach (var quest in quests)
                {
                    var entry = Instantiate(questEntryPrefab, questLogContent).GetComponent<QuestLogEntry>();
                    if (entry != null)
                    {
                        entry.SetQuestInfo(quest.title, quest.description, quest.currentProgress);
                    }
                }
            }
        }

        private void HandleDialogueNodeStart(DialogueNode node)
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }
        }

        private void HandleDialogueNodeEnd(DialogueNode node)
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
                // Hide all choice buttons
                foreach (Transform child in dialogueChoicesContainer)
                {
                    var button = child.GetComponent<DialogueChoiceButton>();
                    if (button != null)
                    {
                        button.Hide();
                    }
                }
            }
        }

        private void HandleDialogueChoiceSelected(string choice)
        {
            audioManager?.PlaySound(UISoundType.DialogueChoice.ToString());
        }

        public void ShowDialogueChoices(DialogueChoice[] choices)
        {
            if (dialogueChoicesContainer != null)
            {
                // Clear existing choices
                foreach (Transform child in dialogueChoicesContainer)
                {
                    Destroy(child.gameObject);
                }

                // Add new choices
                foreach (var choice in choices)
                {
                    ShowDialogueChoice(choice);
                }
            }
        }

        private void ShowDialogueChoice(DialogueChoice choice)
        {
            if (choice != null && dialogueChoicePrefab != null && dialogueChoicesContainer != null)
            {
                var button = Instantiate(dialogueChoicePrefab, dialogueChoicesContainer);
                button.SetChoice(choice.text, () => dialogueSystem.HandleChoice(choice.text));
                button.SetEnabled(dialogueSystem.CheckCondition(choice.condition));
            }
        }

        private void SetNotificationStyle(NotificationType type)
        {
            Color color = type switch
            {
                NotificationType.Success => Color.green,
                NotificationType.Warning => Color.yellow,
                NotificationType.Error => Color.red,
                _ => Color.white
            };

            if (notificationText != null)
            {
                notificationText.color = color;
            }
        }

        private void PlayNotificationSound(NotificationType type)
        {
            UISoundType soundType = type switch
            {
                NotificationType.Success => UISoundType.Success,
                NotificationType.Warning => UISoundType.Warning,
                NotificationType.Error => UISoundType.Error,
                _ => UISoundType.NotificationShow
            };

            audioManager?.PlaySound(soundType.ToString());
        }

        private void HideNotification()
        {
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
        }

        public void ShowEventNotification(string eventId)
        {
            ShowNotification($"Event Started: {eventId}", NotificationType.Info);
        }

        public void ShowEventCompletion(string eventId)
        {
            ShowNotification($"Event Completed: {eventId}", NotificationType.Success);
        }

        public void ShowEventFailure(string eventId)
        {
            ShowNotification($"Event Failed: {eventId}", NotificationType.Error);
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
} 