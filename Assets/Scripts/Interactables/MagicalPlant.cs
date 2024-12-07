using UnityEngine;
using System.Collections;
using Forever.Characters;

namespace Forever.Interactables
{
    public class MagicalPlant : InteractableObject
    {
        [Header("Plant Settings")]
        public float growthDuration = 3f;
        public float maxHeight = 5f;
        public float healingThreshold = 50f;
        public Color[] possibleColors;
        
        [Header("Visual Effects")]
        public ParticleSystem growthParticles;
        public ParticleSystem healingParticles;
        public GameObject glowEffect;
        
        private Vector3 originalScale;
        private float currentHealth = 100f;
        private bool isGrowing = false;
        private bool isWilted = false;
        private Color currentColor;

        protected override void Awake()
        {
            base.Awake();
            originalScale = transform.localScale;
            currentColor = possibleColors[Random.Range(0, possibleColors.Length)];
            
            if (objectRenderer != null)
            {
                objectRenderer.material.color = currentColor;
            }
        }

        protected override void Start()
        {
            base.Start();
            if (glowEffect != null)
            {
                glowEffect.SetActive(false);
            }
        }

        protected override void OnInteract()
        {
            var character = Core.GameManager.Instance.currentCharacter;
            if (character == null) return;

            switch (character.characterType)
            {
                case CharacterType.Shibna:
                    HandleEmpathyInteraction();
                    break;
                case CharacterType.Iwaan:
                    HandleCreativityInteraction();
                    break;
                case CharacterType.Anshad:
                    HandleIngenuityInteraction();
                    break;
                case CharacterType.Inaya:
                    HandleAgilityInteraction();
                    break;
                case CharacterType.Ilan:
                    HandleLogicInteraction();
                    break;
            }
        }

        private void HandleEmpathyInteraction()
        {
            if (isWilted)
            {
                StartCoroutine(RevivePlant());
            }
            else
            {
                // Create a healing aura that affects nearby plants
                if (healingParticles != null)
                {
                    healingParticles.Play();
                }
            }
        }

        private void HandleCreativityInteraction()
        {
            // Change the plant's color and create a beautiful pattern
            Color newColor = possibleColors[Random.Range(0, possibleColors.Length)];
            while (newColor == currentColor)
            {
                newColor = possibleColors[Random.Range(0, possibleColors.Length)];
            }
            Paint(newColor);
            currentColor = newColor;

            if (growthParticles != null)
            {
                var main = growthParticles.main;
                main.startColor = newColor;
                growthParticles.Play();
            }
        }

        private void HandleIngenuityInteraction()
        {
            // Enhance the plant's properties
            if (!IsTransformed)
            {
                Transform();
                if (glowEffect != null)
                {
                    glowEffect.SetActive(true);
                }
            }
        }

        private void HandleAgilityInteraction()
        {
            // Make the plant grow rapidly to create a climbing point
            if (!isGrowing)
            {
                StartCoroutine(GrowPlant());
            }
        }

        private void HandleLogicInteraction()
        {
            // Analyze the plant and reveal hidden properties
            Enhance();
            // TODO: Show plant properties UI
        }

        public override void Heal(float amount)
        {
            base.Heal(amount);
            currentHealth = Mathf.Min(currentHealth + amount, 100f);
            
            if (currentHealth >= healingThreshold && isWilted)
            {
                StartCoroutine(RevivePlant());
            }
        }

        public override void Transform()
        {
            base.Transform();
            // Enhance the plant's properties
            StartCoroutine(EnhancePlant());
        }

        private IEnumerator GrowPlant()
        {
            isGrowing = true;
            Vector3 targetScale = originalScale + Vector3.up * maxHeight;
            float elapsed = 0f;

            if (growthParticles != null)
            {
                growthParticles.Play();
            }

            while (elapsed < growthDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / growthDuration;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            yield return new WaitForSeconds(5f); // Stay grown for 5 seconds

            elapsed = 0f;
            while (elapsed < growthDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / growthDuration;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            if (growthParticles != null)
            {
                growthParticles.Stop();
            }

            isGrowing = false;
        }

        private IEnumerator RevivePlant()
        {
            if (healingParticles != null)
            {
                healingParticles.Play();
            }

            float elapsed = 0f;
            Color wiltedColor = Color.gray;
            
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                if (objectRenderer != null)
                {
                    objectRenderer.material.color = Color.Lerp(wiltedColor, currentColor, elapsed);
                }
                yield return null;
            }

            isWilted = false;
            if (healingParticles != null)
            {
                healingParticles.Stop();
            }
        }

        private IEnumerator EnhancePlant()
        {
            if (glowEffect != null)
            {
                glowEffect.SetActive(true);
            }

            // Pulse effect
            float elapsed = 0f;
            float pulseDuration = 2f;
            
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float pulse = 1f + Mathf.Sin(elapsed * 10f) * 0.1f;
                transform.localScale = originalScale * pulse;
                yield return null;
            }

            transform.localScale = originalScale;
        }

        public void Wilt()
        {
            if (!isWilted)
            {
                isWilted = true;
                if (objectRenderer != null)
                {
                    objectRenderer.material.color = Color.gray;
                }
                currentHealth = 0f;
            }
        }

        private void OnValidate()
        {
            if (possibleColors == null || possibleColors.Length == 0)
            {
                possibleColors = new Color[]
                {
                    new Color(0.5f, 1f, 0.5f), // Light green
                    new Color(0f, 0.8f, 0.2f), // Forest green
                    new Color(0.8f, 1f, 0.2f), // Yellow-green
                    new Color(0.3f, 0.9f, 0.7f) // Turquoise
                };
            }
        }
    }
} 