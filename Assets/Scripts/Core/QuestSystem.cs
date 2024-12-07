using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Forever.UI;

namespace Forever.Core
{
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        [Header("Quest Settings")]
        public float questUpdateInterval = 1f;
        public int maxActiveQuests = 5;
        public float questMarkerRange = 50f;
        
        [Header("Quest Data")]
        public Quest[] mainQuests;
        public Quest[] sideQuests;
        public Quest[] dailyQuests;

        public event Action<Quest> OnQuestStarted;
        public event Action<Quest> OnQuestCompleted;
        public event Action<Quest> OnQuestFailed;
        public event Action<Quest, float> OnQuestProgress;

        private Dictionary<string, QuestState> questStates = new Dictionary<string, QuestState>();
        private List<Quest> activeQuests = new List<Quest>();
        private SaveSystem saveSystem;
        private GameManager gameManager;
        private UIManager uiManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSystem()
        {
            // Get system references
            saveSystem = FindObjectOfType<SaveSystem>();
            gameManager = FindObjectOfType<GameManager>();
            uiManager = FindObjectOfType<UIManager>();
            
            // Initialize quest states
            InitializeQuestStates();
            
            // Start quest update routine
            InvokeRepeating(nameof(UpdateQuests), questUpdateInterval, questUpdateInterval);
        }

        private void InitializeQuestStates()
        {
            InitializeQuestArray(mainQuests);
            InitializeQuestArray(sideQuests);
            InitializeQuestArray(dailyQuests);

            // Load saved quest states
            LoadQuestStates();
        }

        private void InitializeQuestArray(Quest[] quests)
        {
            if (quests == null) return;
            
            foreach (var quest in quests)
            {
                questStates[quest.questId] = new QuestState
                {
                    isUnlocked = quest.isAvailableFromStart,
                    isCompleted = false,
                    isActive = false,
                    objectives = new Dictionary<string, float>()
                };
                
                foreach (var objective in quest.objectives)
                {
                    questStates[quest.questId].objectives[objective.objectiveId] = 0f;
                }
            }
        }

        private void LoadQuestStates()
        {
            if (saveSystem == null) return;

            var savedData = saveSystem.GetSavedData<QuestSaveData>("QuestData");
            if (savedData != null)
            {
                foreach (var questId in savedData.activeQuestIds)
                {
                    var quest = FindQuest(questId);
                    if (quest != null)
                    {
                        activeQuests.Add(quest);
                        questStates[questId].isActive = true;
                    }
                }

                foreach (var questId in savedData.completedQuestIds)
                {
                    if (questStates.ContainsKey(questId))
                    {
                        questStates[questId].isCompleted = true;
                    }
                }

                foreach (var progress in savedData.questProgress)
                {
                    if (questStates.ContainsKey(progress.Key))
                    {
                        UpdateQuestProgress(progress.Key, progress.Value);
                    }
                }
            }
        }

        private void UpdateQuests()
        {
            foreach (var quest in activeQuests.ToArray())
            {
                UpdateQuestObjectives(quest);
            }
        }

        private void UpdateQuestObjectives(Quest quest)
        {
            if (!questStates.ContainsKey(quest.questId)) return;

            var state = questStates[quest.questId];
            float totalProgress = CalculateQuestProgress(quest);
            OnQuestProgress?.Invoke(quest, totalProgress);

            if (totalProgress >= 1f)
            {
                CompleteQuest(quest);
            }
        }

        public void StartQuest(string questId)
        {
            Quest quest = FindQuest(questId);
            if (quest == null || !CanStartQuest(quest)) return;

            if (activeQuests.Count >= maxActiveQuests)
            {
                Debug.LogWarning($"Cannot start quest {questId}: Maximum active quests reached");
                return;
            }

            activeQuests.Add(quest);
            questStates[questId].isActive = true;
            OnQuestStarted?.Invoke(quest);
            SaveQuestData();
        }

        private bool CanStartQuest(Quest quest)
        {
            if (quest == null) return false;

            QuestState state = questStates[quest.questId];
            if (state.isCompleted || state.isActive) return false;

            // Check prerequisites
            foreach (string prereq in quest.prerequisites)
            {
                if (!questStates.ContainsKey(prereq) || !questStates[prereq].isCompleted)
                    return false;
            }

            // Check level requirement
            if (gameManager != null && gameManager.playerLevel < quest.requiredLevel)
                return false;

            return true;
        }

        public void UpdateQuestProgress(string questId, string objectiveId, float progress)
        {
            if (!questStates.ContainsKey(questId)) return;

            QuestState state = questStates[questId];
            if (!state.isActive || !state.objectives.ContainsKey(objectiveId)) return;

            state.objectives[objectiveId] = Mathf.Clamp01(progress);
            Quest quest = FindQuest(questId);
            
            if (quest != null)
            {
                float totalProgress = CalculateQuestProgress(quest);
                OnQuestProgress?.Invoke(quest, totalProgress);

                if (totalProgress >= 1f)
                {
                    CompleteQuest(quest);
                }
            }
        }

        private float CalculateQuestProgress(Quest quest)
        {
            if (!questStates.ContainsKey(quest.questId)) return 0f;

            QuestState state = questStates[quest.questId];
            float totalProgress = 0f;
            float totalWeight = 0f;

            foreach (var objective in quest.objectives)
            {
                if (state.objectives.ContainsKey(objective.objectiveId))
                {
                    totalProgress += state.objectives[objective.objectiveId] * objective.weight;
                    totalWeight += objective.weight;
                }
            }

            return totalWeight > 0 ? totalProgress / totalWeight : 0f;
        }

        private void CompleteQuest(Quest quest)
        {
            if (!questStates.ContainsKey(quest.questId)) return;

            QuestState state = questStates[quest.questId];
            state.isCompleted = true;
            state.isActive = false;
            activeQuests.Remove(quest);

            // Grant rewards
            foreach (var reward in quest.rewards)
            {
                GrantReward(reward);
            }

            // Unlock dependent quests
            foreach (string unlockedQuest in quest.unlockedQuests)
            {
                if (questStates.ContainsKey(unlockedQuest))
                {
                    questStates[unlockedQuest].isUnlocked = true;
                }
            }

            OnQuestCompleted?.Invoke(quest);
            SaveQuestData();
        }

        private void GrantReward(QuestReward reward)
        {
            switch (reward.type)
            {
                case RewardType.Experience:
                    gameManager?.GainExperience((int)reward.value);
                    break;
                case RewardType.Item:
                    // Handle item rewards through inventory system
                    break;
                case RewardType.Currency:
                    gameManager?.GainCurrency((int)reward.value);
                    break;
            }
        }

        private Quest FindQuest(string questId)
        {
            foreach (var quest in mainQuests)
                if (quest.questId == questId) return quest;

            foreach (var quest in sideQuests)
                if (quest.questId == questId) return quest;

            foreach (var quest in dailyQuests)
                if (quest.questId == questId) return quest;

            return null;
        }

        public List<Quest> GetActiveQuests()
        {
            return activeQuests;
        }

        private void SaveQuestData()
        {
            if (saveSystem == null) return;

            var completedQuestIds = questStates.Keys.ToList().Where(id => questStates[id].isCompleted);
            var questProgressDict = questStates.ToDictionary(
                kvp => kvp.Key,
                kvp => CalculateQuestProgress(FindQuest(kvp.Key))
            );

            saveSystem.SaveData("QuestData", new QuestSaveData
            {
                activeQuestIds = activeQuests.ConvertAll(q => q.questId),
                completedQuestIds = completedQuestIds.ToList(),
                questProgress = questProgressDict
            });
        }

        public QuestState GetQuestState(string questId)
        {
            return questStates.TryGetValue(questId, out var state) ? state : null;
        }
    }

    [System.Serializable]
    public class Quest
    {
        public string questId;
        public string questName;
        public string description;
        public QuestType questType;
        public int requiredLevel;
        public bool isAvailableFromStart;
        public string[] prerequisites;
        public string[] unlockedQuests;
        public QuestObjective[] objectives;
        public QuestReward[] rewards;
        public QuestElement[] questElements;
    }

    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType type;
        public float targetValue;
        public float weight = 1f;
        public bool isOptional;
        public string[] requiredItems;
    }

    [System.Serializable]
    public class QuestReward
    {
        public RewardType type;
        public string rewardId;
        public float value;
        public string description;
        public GameObject rewardPrefab;
    }

    [System.Serializable]
    public class QuestElement
    {
        public string elementId;
        public GameObject prefab;
        public float spawnRadius;
        public bool despawnOnComplete;
    }

    public class QuestState
    {
        public bool isUnlocked;
        public bool isCompleted;
        public bool isActive;
        public Dictionary<string, float> objectives;
    }

    public enum QuestType
    {
        Main,
        Side,
        Daily,
        Hidden
    }

    public enum ObjectiveType
    {
        Collect,
        Interact,
        Defeat,
        Protect,
        Explore,
        Escort,
        Solve
    }

    public enum RewardType
    {
        Experience,
        Item,
        Ability,
        Currency,
        Reputation
    }

    [System.Serializable]
    public class QuestSaveData
    {
        public List<string> activeQuestIds;
        public List<string> completedQuestIds;
        public Dictionary<string, float> questProgress;
    }
} 