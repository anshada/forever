using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Forever.Core;

namespace Forever.UI
{
    public class QuestLogEntry : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI questTitleText;
        public TextMeshProUGUI questDescriptionText;
        public TextMeshProUGUI questProgressText;
        public Image questTypeIcon;
        public Image progressBar;
        public Transform objectivesContainer;
        public GameObject objectivePrefab;
        
        [Header("Visual States")]
        public Color mainQuestColor = Color.yellow;
        public Color sideQuestColor = Color.cyan;
        public Color dailyQuestColor = Color.green;
        public Color hiddenQuestColor = Color.gray;
        public Color completedColor = Color.green;
        public Sprite mainQuestIcon;
        public Sprite sideQuestIcon;
        public Sprite dailyQuestIcon;
        public Sprite hiddenQuestIcon;
        
        private Quest quest;
        private QuestState questState;
        private Button expandButton;
        private bool isExpanded;
        private RectTransform rectTransform;
        private float collapsedHeight;
        private float expandedHeight;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            expandButton = GetComponent<Button>();
            
            if (expandButton != null)
            {
                expandButton.onClick.AddListener(ToggleExpand);
            }
            
            // Store initial heights
            collapsedHeight = rectTransform.sizeDelta.y;
            expandedHeight = collapsedHeight * 3f; // Adjust based on content
        }
        
        public void Initialize(Quest questData)
        {
            quest = questData;
            questState = QuestSystem.Instance?.GetQuestState(quest.questId);
            
            UpdateVisuals();
            CreateObjectiveEntries();
        }
        
        private void UpdateVisuals()
        {
            // Update quest title
            if (questTitleText != null)
            {
                questTitleText.text = quest.questName;
                questTitleText.color = GetQuestColor();
            }
            
            // Update description
            if (questDescriptionText != null)
            {
                questDescriptionText.text = quest.description;
                questDescriptionText.gameObject.SetActive(isExpanded);
            }
            
            // Update progress
            if (questProgressText != null && questState != null)
            {
                float progress = CalculateQuestProgress();
                questProgressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
            
            // Update progress bar
            if (progressBar != null && questState != null)
            {
                progressBar.fillAmount = CalculateQuestProgress();
                progressBar.color = questState.isCompleted ? completedColor : GetQuestColor();
            }
            
            // Update type icon
            if (questTypeIcon != null)
            {
                questTypeIcon.sprite = GetQuestTypeIcon();
                questTypeIcon.color = GetQuestColor();
            }
        }
        
        private void CreateObjectiveEntries()
        {
            if (objectivesContainer == null || objectivePrefab == null || quest.objectives == null)
                return;
                
            // Clear existing objectives
            foreach (Transform child in objectivesContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create new objective entries
            foreach (var objective in quest.objectives)
            {
                GameObject objectiveGO = Instantiate(objectivePrefab, objectivesContainer);
                QuestObjectiveEntry objectiveEntry = objectiveGO.GetComponent<QuestObjectiveEntry>();
                
                if (objectiveEntry != null)
                {
                    objectiveEntry.Initialize(objective, questState);
                }
            }
            
            // Hide objectives container when collapsed
            objectivesContainer.gameObject.SetActive(isExpanded);
        }
        
        private float CalculateQuestProgress()
        {
            if (questState == null || quest.objectives == null || quest.objectives.Length == 0)
                return 0f;
                
            float totalProgress = 0f;
            float totalWeight = 0f;
            
            foreach (var objective in quest.objectives)
            {
                if (questState.objectives.TryGetValue(objective.objectiveId, out float progress))
                {
                    totalProgress += progress * objective.weight;
                    totalWeight += objective.weight;
                }
            }
            
            return totalWeight > 0 ? totalProgress / totalWeight : 0f;
        }
        
        private Color GetQuestColor()
        {
            if (questState != null && questState.isCompleted)
                return completedColor;
                
            switch (quest.questType)
            {
                case QuestType.Main:
                    return mainQuestColor;
                case QuestType.Side:
                    return sideQuestColor;
                case QuestType.Daily:
                    return dailyQuestColor;
                case QuestType.Hidden:
                    return hiddenQuestColor;
                default:
                    return Color.white;
            }
        }
        
        private Sprite GetQuestTypeIcon()
        {
            switch (quest.questType)
            {
                case QuestType.Main:
                    return mainQuestIcon;
                case QuestType.Side:
                    return sideQuestIcon;
                case QuestType.Daily:
                    return dailyQuestIcon;
                case QuestType.Hidden:
                    return hiddenQuestIcon;
                default:
                    return null;
            }
        }
        
        private void ToggleExpand()
        {
            isExpanded = !isExpanded;
            
            // Animate height change
            float targetHeight = isExpanded ? expandedHeight : collapsedHeight;
            LeanTween.value(gameObject, rectTransform.sizeDelta.y, targetHeight, 0.3f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnUpdate((float val) =>
                {
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, val);
                });
            
            // Show/hide expanded content
            if (questDescriptionText != null)
            {
                questDescriptionText.gameObject.SetActive(isExpanded);
            }
            
            if (objectivesContainer != null)
            {
                objectivesContainer.gameObject.SetActive(isExpanded);
            }
            
            // Play sound effect
            AudioManager.Instance?.PlayUISound(UISoundType.PanelToggle);
        }
        
        private void OnDestroy()
        {
            // Clean up tweens
            LeanTween.cancel(gameObject);
            
            // Remove button listener
            if (expandButton != null)
            {
                expandButton.onClick.RemoveListener(ToggleExpand);
            }
        }
    }
} 