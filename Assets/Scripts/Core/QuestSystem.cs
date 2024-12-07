using UnityEngine;
using System;
using System.Collections.Generic;
using Forever.Characters;

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
        
        private Dictionary<string, QuestState> questStates;
        private List<Quest> activeQuests;
        private SaveSystem saveSystem;
        private GameManager gameManager;
        private UIManager uiManager;
        
        public event Action<Quest> OnQuestStarted;
        public event Action<Quest> OnQuestCompleted;
        public event Action<Quest> OnQuestFailed;
        public event Action<Quest, float> OnQuestProgress;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSystem()
        {
            questStates = new Dictionary<string, QuestState>();
            activeQuests = new List<Quest>();
            
            // Get system references
            saveSystem = FindObjectOfType<SaveSystem>();
            gameManager = FindObjectOfType<GameManager>();
            uiManager = FindObjectOfType<UIManager>();
            
            // Initialize quest states
            InitializeQuestStates();
            
            // Start quest update routine
            InvokeRepeating("UpdateQuests", questUpdateInterval, questUpdateInterval);
        }
        
        private void InitializeQuestStates()
        {
            // Initialize main quests
            foreach (var quest in mainQuests)
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
            
            // Initialize side quests
            foreach (var quest in sideQuests)
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
            
            // Load saved quest states
            if (saveSystem != null)
            {
                LoadQuestStates();
            }
        }
        
        private void LoadQuestStates()
        {
            // TODO: Implement loading quest states from save system
        }
        
        private void UpdateQuests()
        {
            foreach (var quest in activeQuests)
            {
                UpdateQuestProgress(quest);
            }
        }
        
        public void StartQuest(string questId)
        {
            Quest quest = FindQuest(questId);
            if (quest == null || !CanStartQuest(quest))
                return;
                
            // Add to active quests
            if (activeQuests.Count >= maxActiveQuests)
            {
                Debug.LogWarning($"Cannot start quest {questId}: Maximum active quests reached");
                return;
            }
            
            activeQuests.Add(quest);
            questStates[questId].isActive = true;
            
            // Initialize objectives
            foreach (var objective in quest.objectives)
            {
                questStates[questId].objectives[objective.objectiveId] = 0f;
            }
            
            // Notify systems
            OnQuestStarted?.Invoke(quest);
            uiManager?.ShowQuestStarted(quest);
            
            // Spawn quest-specific elements
            SpawnQuestElements(quest);
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
            if (gameManager != null && gameManager.PlayerLevel < quest.requiredLevel)
                return false;
                
            return true;
        }
        
        private void SpawnQuestElements(Quest quest)
        {
            foreach (var element in quest.questElements)
            {
                if (element.prefab != null)
                {
                    Vector3 spawnPos = GetValidSpawnPosition(element.spawnRadius);
                    if (spawnPos != Vector3.zero)
                    {
                        GameObject spawned = Instantiate(element.prefab, spawnPos, Quaternion.identity);
                        spawned.name = $"{quest.questId}_{element.elementId}";
                    }
                }
            }
        }
        
        private Vector3 GetValidSpawnPosition(float radius)
        {
            // TODO: Implement proper spawn position validation using NavMesh
            return transform.position + UnityEngine.Random.insideUnitSphere * radius;
        }
        
        public void UpdateObjectiveProgress(string questId, string objectiveId, float progress)
        {
            if (!questStates.ContainsKey(questId))
                return;
                
            QuestState state = questStates[questId];
            if (!state.isActive || !state.objectives.ContainsKey(objectiveId))
                return;
                
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
            if (!questStates.ContainsKey(quest.questId))
                return 0f;
                
            QuestState state = questStates[quest.questId];
            float totalProgress = 0f;
            
            foreach (var objective in quest.objectives)
            {
                if (state.objectives.ContainsKey(objective.objectiveId))
                {
                    totalProgress += state.objectives[objective.objectiveId] * objective.weight;
                }
            }
            
            return totalProgress / quest.objectives.Length;
        }
        
        private void CompleteQuest(Quest quest)
        {
            if (!questStates.ContainsKey(quest.questId))
                return;
                
            QuestState state = questStates[quest.questId];
            state.isCompleted = true;
            state.isActive = false;
            
            // Remove from active quests
            activeQuests.Remove(quest);
            
            // Grant rewards
            if (gameManager != null)
            {
                foreach (var reward in quest.rewards)
                {
                    // TODO: Implement reward system
                }
            }
            
            // Unlock dependent quests
            foreach (string unlockedQuest in quest.unlockedQuests)
            {
                if (questStates.ContainsKey(unlockedQuest))
                {
                    questStates[unlockedQuest].isUnlocked = true;
                }
            }
            
            // Notify systems
            OnQuestCompleted?.Invoke(quest);
            uiManager?.ShowQuestCompleted(quest);
            
            // Save progress
            if (saveSystem != null)
            {
                saveSystem.SaveQuestState(quest.questId, true);
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
        
        public QuestState GetQuestState(string questId)
        {
            QuestState state;
            return questStates.TryGetValue(questId, out state) ? state : null;
        }
        
        public Quest[] GetAvailableQuests()
        {
            List<Quest> available = new List<Quest>();
            
            foreach (var pair in questStates)
            {
                if (pair.Value.isUnlocked && !pair.Value.isCompleted && !pair.Value.isActive)
                {
                    Quest quest = FindQuest(pair.Key);
                    if (quest != null && CanStartQuest(quest))
                    {
                        available.Add(quest);
                    }
                }
            }
            
            return available.ToArray();
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
} 