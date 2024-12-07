using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Forever.Audio;
using System.Collections;

namespace Forever.UI
{
    public class QuestLogEntry : MonoBehaviour
    {
        [Header("References")]
        public TextMeshProUGUI questTitle;
        public TextMeshProUGUI questDescription;
        public TextMeshProUGUI progressText;
        public Image progressBar;
        public Image background;
        public Button expandButton;
        public RectTransform contentPanel;
        
        [Header("Animation Settings")]
        public float expandDuration = 0.3f;
        public float progressBarDuration = 0.5f;
        public float fadeInDuration = 0.3f;
        public float fadeOutDuration = 0.2f;
        
        private bool isExpanded;
        private float originalHeight;
        private Vector2 collapsedSize;
        private Vector2 expandedSize;
        private Coroutine currentAnimation;
        
        private void Awake()
        {
            if (expandButton != null)
            {
                expandButton.onClick.AddListener(ToggleExpand);
            }
            
            // Store original sizes
            originalHeight = contentPanel.sizeDelta.y;
            collapsedSize = new Vector2(contentPanel.sizeDelta.x, 0);
            expandedSize = new Vector2(contentPanel.sizeDelta.x, originalHeight);
            
            // Initialize in collapsed state
            contentPanel.sizeDelta = collapsedSize;
            isExpanded = false;
        }
        
        public void SetQuestInfo(string title, string description, float progress)
        {
            questTitle.text = title;
            questDescription.text = description;
            
            // Animate progress bar
            if (progressBar != null)
            {
                if (currentAnimation != null)
                    StopCoroutine(currentAnimation);
                    
                currentAnimation = StartCoroutine(AnimateProgressBar(progress));
            }
            
            if (progressText != null)
            {
                progressText.text = $"{(progress * 100):F0}%";
            }
            
            // Fade in
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(FadeIn());
        }
        
        private IEnumerator AnimateProgressBar(float targetFill)
        {
            float startFill = progressBar.fillAmount;
            float elapsed = 0f;
            
            while (elapsed < progressBarDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / progressBarDuration;
                float smoothT = 1f - (1f - t) * (1f - t); // Ease out quad
                
                progressBar.fillAmount = Mathf.Lerp(startFill, targetFill, smoothT);
                
                yield return null;
            }
            
            progressBar.fillAmount = targetFill;
        }
        
        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            Color startColor = background.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);
            Vector3 startScale = Vector3.zero;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease out
                
                background.color = Color.Lerp(startColor, targetColor, smoothT);
                transform.localScale = Vector3.Lerp(startScale, Vector3.one, smoothT);
                
                yield return null;
            }
            
            background.color = targetColor;
            transform.localScale = Vector3.one;
        }
        
        public void ToggleExpand()
        {
            isExpanded = !isExpanded;
            
            AudioManager.Instance.PlayUISound(UISoundType.PanelToggle);
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(AnimateExpand());
        }
        
        private IEnumerator AnimateExpand()
        {
            float elapsed = 0f;
            Vector2 startSize = contentPanel.sizeDelta;
            Vector2 targetSize = isExpanded ? expandedSize : collapsedSize;
            Vector3 startRotation = expandButton.transform.eulerAngles;
            Vector3 targetRotation = new Vector3(0, 0, isExpanded ? 180 : 0);
            
            while (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / expandDuration;
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease out
                
                contentPanel.sizeDelta = Vector2.Lerp(startSize, targetSize, smoothT);
                
                if (expandButton != null)
                {
                    expandButton.transform.eulerAngles = Vector3.Lerp(startRotation, targetRotation, smoothT);
                }
                
                yield return null;
            }
            
            contentPanel.sizeDelta = targetSize;
            if (expandButton != null)
            {
                expandButton.transform.eulerAngles = targetRotation;
            }
        }
        
        public void Hide()
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(FadeOut());
        }
        
        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            Color startColor = background.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            Vector3 startScale = transform.localScale;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                float smoothT = t * t; // Ease in
                
                background.color = Color.Lerp(startColor, targetColor, smoothT);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothT);
                
                yield return null;
            }
            
            gameObject.SetActive(false);
        }
        
        private void OnDestroy()
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            if (expandButton != null)
            {
                expandButton.onClick.RemoveListener(ToggleExpand);
            }
        }
    }
} 