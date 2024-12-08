using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Forever.Audio;
using System;
using System.Collections;

namespace Forever.UI
{
    public class DialogueChoiceButton : MonoBehaviour, 
        IPointerEnterHandler, 
        IPointerExitHandler, 
        ISelectHandler, 
        IDeselectHandler
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
        private Action onClickCallback;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (choiceText == null) choiceText = GetComponentInChildren<TextMeshProUGUI>();
            if (background == null) background = GetComponent<Image>();
            
            originalScale = transform.localScale;
            
            // Setup button events
            button.onClick.AddListener(OnClick);
        }

        public void SetChoice(string text, Action onClick)
        {
            choiceText.text = text;
            onClickCallback = onClick;
            
            // Reset state
            transform.localScale = originalScale;
            background.color = new Color(background.color.r, background.color.g, background.color.b, 0f);
            
            // Fade in
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(FadeIn());
        }

        public void SetEnabled(bool enabled)
        {
            button.interactable = enabled;

            Color color = background.color;
            color.a = enabled ? 1f : 0.5f;
            background.color = color;

            color = choiceText.color;
            color.a = enabled ? 1f : 0.5f;
            choiceText.color = color;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            AudioManager.Instance?.PlaySound(UISoundType.ButtonHover.ToString());
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(ScaleTo(originalScale * hoverScale, hoverDuration));
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(ScaleTo(originalScale, hoverDuration));
        }
        
        public void OnSelect(BaseEventData eventData)
        {
            if (!button.interactable) return;
            
            AudioManager.Instance?.PlaySound(UISoundType.ButtonHover.ToString());
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(ScaleTo(originalScale * hoverScale, hoverDuration));
        }
        
        public void OnDeselect(BaseEventData eventData)
        {
            if (!button.interactable) return;
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(ScaleTo(originalScale, hoverDuration));
        }

        private void OnClick()
        {
            if (!button.interactable) return;
            
            AudioManager.Instance?.PlaySound(UISoundType.ButtonClick.ToString());
            onClickCallback?.Invoke();
            
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