using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Forever.Core;

namespace Forever.UI
{
    public class DialogueChoiceButton : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI choiceText;
        public Image backgroundImage;
        public Image iconImage;
        
        [Header("Visual States")]
        public Color normalColor = Color.white;
        public Color hoverColor = Color.gray;
        public Color selectedColor = Color.yellow;
        public float scaleOnHover = 1.1f;
        public float animationSpeed = 5f;
        
        private DialogueChoice choice;
        private int choiceIndex;
        private Button button;
        private Vector3 originalScale;
        private bool isHovered;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            originalScale = transform.localScale;
            
            // Set up button events
            button.onClick.AddListener(OnClick);
            
            // Add hover events
            var eventTrigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { OnPointerEnter(); });
            eventTrigger.triggers.Add(enterEntry);
            
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { OnPointerExit(); });
            eventTrigger.triggers.Add(exitEntry);
        }
        
        public void SetChoice(DialogueChoice dialogueChoice, int index)
        {
            choice = dialogueChoice;
            choiceIndex = index;
            
            if (choiceText != null)
            {
                choiceText.text = dialogueChoice.text;
            }
            
            // Set initial visual state
            UpdateVisualState(false);
        }
        
        private void OnClick()
        {
            if (DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.MakeChoice(choiceIndex);
            }
            
            // Visual feedback
            UpdateVisualState(true);
            
            // Play sound effect
            AudioManager.Instance?.PlayUISound(UISoundType.ButtonClick);
        }
        
        private void OnPointerEnter()
        {
            isHovered = true;
            
            // Scale up
            LeanTween.scale(gameObject, originalScale * scaleOnHover, 1f / animationSpeed)
                .setEase(LeanTweenType.easeOutQuad);
            
            // Color transition
            if (backgroundImage != null)
            {
                LeanTween.color(backgroundImage.rectTransform, hoverColor, 1f / animationSpeed)
                    .setEase(LeanTweenType.easeOutQuad);
            }
            
            // Play hover sound
            AudioManager.Instance?.PlayUISound(UISoundType.ButtonHover);
        }
        
        private void OnPointerExit()
        {
            isHovered = false;
            
            // Scale back
            LeanTween.scale(gameObject, originalScale, 1f / animationSpeed)
                .setEase(LeanTweenType.easeOutQuad);
            
            // Color transition
            if (backgroundImage != null)
            {
                LeanTween.color(backgroundImage.rectTransform, normalColor, 1f / animationSpeed)
                    .setEase(LeanTweenType.easeOutQuad);
            }
        }
        
        private void UpdateVisualState(bool selected)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedColor : (isHovered ? hoverColor : normalColor);
            }
            
            // Update any additional visual elements based on state
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(selected);
            }
        }
        
        private void OnDestroy()
        {
            // Clean up tweens
            LeanTween.cancel(gameObject);
            
            // Remove button listener
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }
        }
    }
} 