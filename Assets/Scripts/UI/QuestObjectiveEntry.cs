using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Forever.Core;

namespace Forever.UI
{
    public class QuestObjectiveEntry : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI objectiveText;
        public TextMeshProUGUI progressText;
        public Image progressBar;
        public Image objectiveIcon;
        public Image checkmarkIcon;
        
        [Header("Visual States")]
        public Color incompleteColor = new Color(0.7f, 0.7f, 0.7f);
        public Color completeColor = Color.green;
        public Color optionalColor = new Color(0.5f, 0.5f, 1f);
        public Color failedColor = Color.red;
        
        [Header("Icons")]
        public Sprite collectIcon;
        public Sprite interactIcon;
        public Sprite defeatIcon;
        public Sprite protectIcon;
        public Sprite exploreIcon;
        public Sprite escortIcon;
        public Sprite solveIcon;
        public Sprite checkmarkSprite;
        
        private QuestObjective objective;
        private QuestState questState;
        private float currentProgress;
        private bool isComplete;
        private RectTransform rectTransform;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        public void Initialize(QuestObjective objectiveData, QuestState state)
        {
            objective = objectiveData;
            questState = state;
            
            // Get current progress
            if (questState != null && questState.objectives.TryGetValue(objective.objectiveId, out float progress))
            {
                currentProgress = progress;
                isComplete = Mathf.Approximately(progress, 1f);
            }
            
            UpdateVisuals();
        }
        
        private void UpdateVisuals()
        {
            // Update objective text
            if (objectiveText != null)
            {
                string optionalPrefix = objective.isOptional ? "[Optional] " : "";
                objectiveText.text = optionalPrefix + objective.description;
                objectiveText.color = GetObjectiveColor();
            }
            
            // Update progress text
            if (progressText != null)
            {
                if (objective.targetValue > 0)
                {
                    float currentValue = currentProgress * objective.targetValue;
                    progressText.text = $"{Mathf.RoundToInt(currentValue)}/{Mathf.RoundToInt(objective.targetValue)}";
                }
                else
                {
                    progressText.gameObject.SetActive(false);
                }
            }
            
            // Update progress bar
            if (progressBar != null)
            {
                progressBar.fillAmount = currentProgress;
                progressBar.color = GetObjectiveColor();
            }
            
            // Update objective icon
            if (objectiveIcon != null)
            {
                objectiveIcon.sprite = GetObjectiveIcon();
                objectiveIcon.color = GetObjectiveColor();
            }
            
            // Update checkmark
            if (checkmarkIcon != null)
            {
                checkmarkIcon.gameObject.SetActive(isComplete);
                if (isComplete)
                {
                    checkmarkIcon.sprite = checkmarkSprite;
                    checkmarkIcon.color = completeColor;
                }
            }
            
            // Apply fade effect if complete
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = isComplete ? 0.7f : 1f;
            }
        }
        
        private Color GetObjectiveColor()
        {
            if (isComplete)
                return completeColor;
                
            if (objective.isOptional)
                return optionalColor;
                
            return incompleteColor;
        }
        
        private Sprite GetObjectiveIcon()
        {
            switch (objective.type)
            {
                case ObjectiveType.Collect:
                    return collectIcon;
                case ObjectiveType.Interact:
                    return interactIcon;
                case ObjectiveType.Defeat:
                    return defeatIcon;
                case ObjectiveType.Protect:
                    return protectIcon;
                case ObjectiveType.Explore:
                    return exploreIcon;
                case ObjectiveType.Escort:
                    return escortIcon;
                case ObjectiveType.Solve:
                    return solveIcon;
                default:
                    return null;
            }
        }
        
        public void UpdateProgress(float progress)
        {
            currentProgress = progress;
            isComplete = Mathf.Approximately(progress, 1f);
            
            // Animate progress change
            if (progressBar != null)
            {
                LeanTween.value(gameObject, progressBar.fillAmount, progress, 0.5f)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setOnUpdate((float val) =>
                    {
                        progressBar.fillAmount = val;
                    });
            }
            
            UpdateVisuals();
            
            // Play completion effect if just completed
            if (isComplete && Mathf.Approximately(progressBar.fillAmount, progress))
            {
                PlayCompletionEffect();
            }
        }
        
        private void PlayCompletionEffect()
        {
            // Scale pulse animation
            LeanTween.scale(gameObject, Vector3.one * 1.1f, 0.2f)
                .setEase(LeanTweenType.easeOutQuad)
                .setLoopPingPong(1);
            
            // Fade in checkmark
            if (checkmarkIcon != null)
            {
                checkmarkIcon.gameObject.SetActive(true);
                checkmarkIcon.color = new Color(1f, 1f, 1f, 0f);
                LeanTween.alpha(checkmarkIcon.rectTransform, 1f, 0.3f)
                    .setEase(LeanTweenType.easeOutQuad);
            }
            
            // Play sound effect
            AudioManager.Instance?.PlayUISound(UISoundType.ObjectiveComplete);
        }
        
        private void OnDestroy()
        {
            // Clean up tweens
            LeanTween.cancel(gameObject);
        }
    }
} 