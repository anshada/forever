using UnityEngine;
using System;
using System.Collections.Generic;
using Forever.Audio;

namespace Forever.Core
{
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        private DialogueNode currentNode;
        private AudioManager audioManager;

        public event Action<DialogueNode> OnDialogueNodeStart;
        public event Action<DialogueNode> OnDialogueNodeEnd;
        public event Action<string> OnDialogueChoice;

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
            audioManager = AudioManager.Instance;
        }

        public void PlayDialogueAudio(AudioClip clip)
        {
            if (clip != null && audioManager != null)
            {
                audioManager.PlaySound(clip.name);
            }
        }

        public void HandleChoice(string choiceText)
        {
            if (currentNode != null && currentNode.choices != null)
            {
                var choice = currentNode.choices.Find(c => c.text == choiceText);
                if (choice != null)
                {
                    SelectChoice(choice);
                    OnDialogueChoice?.Invoke(choiceText);
                }
            }
        }

        public void SelectChoice(DialogueChoice choice)
        {
            if (choice != null)
            {
                // Execute consequences
                if (choice.consequences != null)
                {
                    foreach (var action in choice.consequences)
                    {
                        ExecuteDialogueAction(action);
                    }
                }

                // Move to next node
                if (choice.nextNode != null)
                {
                    currentNode = choice.nextNode;
                    DisplayCurrentNode();
                }
            }
        }

        private void ExecuteDialogueAction(DialogueAction action)
        {
            if (action == null) return;

            switch (action.type)
            {
                case DialogueActionType.SetFlag:
                    GameManager.Instance?.SetGameFlag(action.parameter);
                    break;
                case DialogueActionType.GiveItem:
                    GameManager.Instance?.AddInventoryItem(action.parameter);
                    break;
                case DialogueActionType.StartQuest:
                    if (QuestSystem.Instance != null)
                    {
                        QuestSystem.Instance.StartQuest(action.parameter);
                    }
                    break;
                case DialogueActionType.ChangeRelationship:
                    GameManager.Instance?.UpdateRelationship(action.parameter);
                    break;
                case DialogueActionType.TriggerEvent:
                    DynamicEventsManager.Instance?.TriggerEvent(action.parameter);
                    break;
                default:
                    Debug.LogWarning($"Unknown dialogue action type: {action.type}");
                    break;
            }
        }

        private void DisplayCurrentNode()
        {
            if (currentNode != null)
            {
                OnDialogueNodeStart?.Invoke(currentNode);
                // Display dialogue text
                // Update UI
                // Show choices
            }
        }

        public bool CheckCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return true;

            // Check game flags
            if (condition.StartsWith("flag:"))
            {
                string flagName = condition.Substring(5);
                return GameManager.Instance?.GetGameFlag(flagName) ?? false;
            }

            // Check quest completion
            if (condition.StartsWith("quest:"))
            {
                string questId = condition.Substring(6);
                var quest = QuestSystem.Instance?.GetQuestState(questId);
                return quest != null && quest.currentProgress >= 1f;
            }

            // Check player level
            if (condition.StartsWith("level:"))
            {
                if (int.TryParse(condition.Substring(6), out int requiredLevel))
                {
                    return GameManager.Instance?.playerLevel >= requiredLevel;
                }
            }

            // Default to true if condition type is unknown
            return true;
        }
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string text;
        public DialogueNode nextNode;
        public DialogueAction[] consequences;
        public string condition;
        public bool requiresCondition;
    }

    [System.Serializable]
    public class DialogueAction
    {
        public DialogueActionType type;
        public string parameter;
    }

    public enum DialogueActionType
    {
        SetFlag,
        GiveItem,
        StartQuest,
        ChangeRelationship,
        TriggerEvent,
        Custom
    }

    [System.Serializable]
    public class DialogueNode
    {
        public string text;
        public List<DialogueChoice> choices;
        public AudioClip audioClip;
    }
} 