using UnityEngine;
using Forever.Characters;

namespace Forever.Interactables
{
    public abstract class InteractableObject : MonoBehaviour, IInteractable, IEnhanceable, IPaintable, ITransformable, IHealable
    {
        [Header("Interaction Settings")]
        public string objectName = "Interactable";
        public string interactionPrompt = "Press F to interact";
        public bool requiresSpecificCharacter = false;
        public CharacterType requiredCharacterType;
        
        [Header("Visual Feedback")]
        public GameObject interactionPromptPrefab;
        public GameObject highlightEffect;
        public Material highlightMaterial;
        
        protected bool isInteractable = true;
        protected bool isHighlighted = false;
        protected GameObject promptInstance;
        protected Material originalMaterial;
        protected Renderer objectRenderer;

        protected virtual void Awake()
        {
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                originalMaterial = objectRenderer.material;
            }
        }

        protected virtual void Start()
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(false);
            }
        }

        #region IInteractable Implementation
        public virtual void Interact()
        {
            if (!isInteractable) return;

            if (requiresSpecificCharacter)
            {
                var currentCharacter = Core.GameManager.Instance.currentCharacter;
                if (currentCharacter == null || currentCharacter.characterType != requiredCharacterType)
                {
                    ShowWrongCharacterPrompt();
                    return;
                }
            }

            OnInteract();
        }

        public virtual void ShowInteractionPrompt()
        {
            if (promptInstance == null && interactionPromptPrefab != null)
            {
                promptInstance = Instantiate(interactionPromptPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
                promptInstance.transform.SetParent(transform);
            }
        }

        public virtual void HideInteractionPrompt()
        {
            if (promptInstance != null)
            {
                Destroy(promptInstance);
                promptInstance = null;
            }
        }
        #endregion

        #region IEnhanceable Implementation
        public virtual void Enhance()
        {
            // Default enhancement behavior
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(true);
            }
        }

        public virtual void ResetEnhancement()
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(false);
            }
        }
        #endregion

        #region IPaintable Implementation
        public virtual void Paint(Color color)
        {
            if (objectRenderer != null)
            {
                Material paintMaterial = new Material(originalMaterial);
                paintMaterial.color = color;
                objectRenderer.material = paintMaterial;
            }
        }

        public virtual void ClearPaint()
        {
            if (objectRenderer != null)
            {
                objectRenderer.material = originalMaterial;
            }
        }
        #endregion

        #region ITransformable Implementation
        public virtual bool IsTransformed { get; protected set; }

        public virtual void Transform()
        {
            IsTransformed = true;
            // Override in derived classes for specific transformation behavior
        }

        public virtual void Revert()
        {
            IsTransformed = false;
            // Override in derived classes for specific reversion behavior
        }
        #endregion

        #region IHealable Implementation
        public virtual void Heal(float amount)
        {
            // Override in derived classes for specific healing behavior
        }
        #endregion

        protected virtual void OnInteract()
        {
            // Override in derived classes for specific interaction behavior
        }

        protected virtual void ShowWrongCharacterPrompt()
        {
            Debug.Log($"This object requires {requiredCharacterType} to interact with it.");
            // TODO: Show UI prompt for wrong character
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                ShowInteractionPrompt();
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                HideInteractionPrompt();
            }
        }

        protected virtual void OnDestroy()
        {
            HideInteractionPrompt();
        }
    }
} 