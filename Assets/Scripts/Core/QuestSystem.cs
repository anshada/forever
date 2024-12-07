using UnityEngine;
using System;
using System.Collections.Generic;

namespace Forever.Core
{
    public class QuestSystem : MonoBehaviour
    {
        private Dictionary<string, Quest> activeQuests = new Dictionary<string, Quest>();
        private SaveSystem saveSystem;

        private void Start()
        {
            saveSystem = SaveSystem.Instance;
            LoadQuestProgress();
        }

        public void UpdateQuestProgress(string questId)
        {
            if (activeQuests.TryGetValue(questId, out Quest quest))
            {
                UpdateQuestProgress(questId, quest.currentObjective, quest.currentProgress);
            }
        }

        public void UpdateQuestProgress(string questId, string objectiveId, float progress)
        {
            if (activeQuests.TryGetValue(questId, out Quest quest))
            {
                quest.currentProgress = Mathf.Clamp01(progress);
                quest.currentObjective = objectiveId;

                // Check for quest completion
                if (quest.currentProgress >= 1f)
                {
                    CompleteQuest(questId);
                }

                SaveQuestProgress();
            }
        }

        private void CompleteQuest(string questId)
        {
            if (activeQuests.TryGetValue(questId, out Quest quest))
            {
                // Award rewards
                if (quest.rewards != null)
                {
                    foreach (var reward in quest.rewards)
                    {
                        AwardQuestReward(reward);
                    }
                }

                // Remove from active quests
                activeQuests.Remove(questId);
                SaveQuestProgress();
            }
        }

        private void AwardQuestReward(QuestReward reward)
        {
            switch (reward.type)
            {
                case QuestRewardType.Experience:
                    GameManager.Instance?.GainExperience(reward.amount);
                    break;
                case QuestRewardType.Currency:
                    GameManager.Instance?.GainCurrency((int)reward.amount);
                    break;
                case QuestRewardType.Item:
                    GameManager.Instance?.AddInventoryItem(reward.itemId);
                    break;
            }
        }

        private void SaveQuestProgress()
        {
            if (saveSystem != null)
            {
                saveSystem.SaveData("QuestProgress", activeQuests);
            }
        }

        private void LoadQuestProgress()
        {
            if (saveSystem != null)
            {
                var savedQuests = saveSystem.GetSavedData<Dictionary<string, Quest>>("QuestProgress");
                if (savedQuests != null)
                {
                    activeQuests = savedQuests;
                }
            }
        }
    }

    [System.Serializable]
    public class Quest
    {
        public string questId;
        public string title;
        public string description;
        public string currentObjective;
        public float currentProgress;
        public QuestReward[] rewards;
    }

    [System.Serializable]
    public class QuestReward
    {
        public QuestRewardType type;
        public float amount;
        public string itemId;
    }

    public enum QuestRewardType
    {
        Experience,
        Currency,
        Item
    }
} 