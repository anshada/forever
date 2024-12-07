using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Forever.UI
{
    public class QuestObjectiveEntry : MonoBehaviour
    {
        [Header("References")]
        public TextMeshProUGUI objectiveText;
        public Image checkmark;
        public Image background;
        
        [Header("Animation Settings")]
        public float fadeInDuration = 0.3f;
        public float fadeOutDuration = 0.2f;
        public float completionScaleDuration = 0.5f;
        public float completionScaleMultiplier = 1.2f;
        
        private Vector3 originalScale;
        private Coroutine currentAnimation;
        
        private void Awake()
        {
            originalScale = transform.localScale;
            
            if (checkmark != null)
                checkmark.gameObject.SetActive(false);
        }
        
        public void SetObjective(string text, bool isCompleted = false)
        {
            objectiveText.text = text;
            
            // Reset state
            transform.localScale = originalScale;
            background.color = new Color(background.color.r, background.color.g, background.color.b, 0f);
            
            if (checkmark != null)
            {
                checkmark.gameObject.SetActive(isCompleted);
                checkmark.color = new Color(checkmark.color.r, checkmark.color.g, checkmark.color.b, isCompleted ? 1f : 0f);
            }
            
            // Fade in
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(FadeIn());
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
        
        public void Complete()
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
                
            currentAnimation = StartCoroutine(CompleteAnimation());
        }
        
        private IEnumerator CompleteAnimation()
        {
            if (checkmark != null)
                checkmark.gameObject.SetActive(true);
            
            // Scale up
            float elapsed = 0f;
            Vector3 startScale = originalScale;
            Vector3 targetScale = originalScale * completionScaleMultiplier;
            
            while (elapsed < completionScaleDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (completionScaleDuration * 0.5f);
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease out
                
                transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                
                if (checkmark != null)
                {
                    Color color = checkmark.color;
                    color.a = smoothT;
                    checkmark.color = color;
                }
                
                yield return null;
            }
            
            // Scale back to normal
            elapsed = 0f;
            startScale = targetScale;
            targetScale = originalScale;
            
            while (elapsed < completionScaleDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (completionScaleDuration * 0.5f);
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease out
                
                transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                
                yield return null;
            }
            
            transform.localScale = originalScale;
            
            if (checkmark != null)
                checkmark.color = new Color(checkmark.color.r, checkmark.color.g, checkmark.color.b, 1f);
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
                
                if (checkmark != null)
                {
                    Color color = checkmark.color;
                    color.a = 1f - smoothT;
                    checkmark.color = color;
                }
                
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