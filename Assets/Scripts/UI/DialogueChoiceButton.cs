using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Forever.Audio;
using System.Collections;

namespace Forever.UI
{
    public class DialogueChoiceButton : MonoBehaviour
    {
        [Header("References")]
        public Button button;
        public TextMeshProUGUI choiceText;
        public Image background;
        
        [Header("Animation Settings")]
        public float hoverScale = 1.1f;
        public float hoverDuration = 0.2f;
        public float clickScale = 0.9f;
        public float clickDuration = 0.1f;
        public float fadeInDuration = 0.3f;
        public float fadeOutDuration = 0.2f;
        
        private Vector3 originalScale;
        private Coroutine currentAnimation;
        
        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (choiceText == null) choiceText = GetComponentInChildren<TextMeshProUGUI>();
            if (background == null) background = GetComponent<Image>();
            
            originalScale = transform.localScale;
            
            // Setup button events
            button.onClick.AddListener(OnClick);
            button.onSelect.AddListener(OnSelect);
            button.onDeselect.AddListener(OnDeselect);
        }
        
        public void SetChoice(string text, bool isEnabled = true)
        {
            choiceText.text = text;
            button.interactable = isEnabled;
            
            // Reset state
            transform.localScale = originalScale;
            background.color = new Color(background.color.r, background.color.g, background.color.b, 0f);
            
            // Fade in
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(FadeIn());
        }
        
        private void OnSelect(UnityEngine.EventSystems.BaseEventData eventData)
        {
            AudioManager.Instance.PlayUISound(UISoundType.ButtonHover);
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(ScaleTo(originalScale * hoverScale, hoverDuration));
        }
        
        private void OnDeselect(UnityEngine.EventSystems.BaseEventData eventData)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(ScaleTo(originalScale, hoverDuration));
        }
        
        private void OnClick()
        {
            AudioManager.Instance.PlayUISound(UISoundType.ButtonClick);
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(ClickAnimation());
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
                transform.localScale = Vector3.Lerp(startScale, originalScale, smoothT);
                
                yield return null;
            }
            
            background.color = targetColor;
            transform.localScale = originalScale;
        }
        
        private IEnumerator ScaleTo(Vector3 targetScale, float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease out
                
                transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                
                yield return null;
            }
            
            transform.localScale = targetScale;
        }
        
        private IEnumerator ClickAnimation()
        {
            // Scale down
            yield return StartCoroutine(ScaleTo(originalScale * clickScale, clickDuration));
            
            // Scale back up
            yield return StartCoroutine(ScaleTo(originalScale, clickDuration));
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
        }
    }
} 