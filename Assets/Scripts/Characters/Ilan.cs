using UnityEngine;
using System.Collections.Generic;

namespace Forever.Characters
{
    public class Ilan : Character
    {
        [Header("Logic Abilities")]
        public float scanRadius = 12f;
        public float timeSlowDuration = 5f;
        public float timeSlowScale = 0.3f;
        public LayerMask puzzleLayer;
        
        private bool isTimeSlowed = false;
        private float timeSlowTimer = 0f;
        private List<IPuzzleElement> scannedElements = new List<IPuzzleElement>();
        private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

        [SerializeField]
        private Material highlightMaterial;

        protected override void Awake()
        {
            base.Awake();
            characterType = CharacterType.Ilan;
            characterName = "Ilan";
        }

        protected override void Update()
        {
            base.Update();
            UpdateTimeSlow();
            HandleScanning();
        }

        private void UpdateTimeSlow()
        {
            if (isTimeSlowed)
            {
                timeSlowTimer += Time.unscaledDeltaTime;
                if (timeSlowTimer >= timeSlowDuration)
                {
                    DeactivateTimeSlow();
                }
            }
        }

        private void HandleScanning()
        {
            // Clear previous scanned elements
            foreach (var element in scannedElements)
            {
                if (element != null)
                {
                    ResetHighlight(((MonoBehaviour)element).gameObject);
                }
            }
            scannedElements.Clear();

            // Scan for new puzzle elements
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, scanRadius, puzzleLayer);
            foreach (var hitCollider in hitColliders)
            {
                IPuzzleElement element = hitCollider.GetComponent<IPuzzleElement>();
                if (element != null)
                {
                    scannedElements.Add(element);
                    HighlightObject(hitCollider.gameObject);
                    element.Reveal();
                }
            }
        }

        protected override void UseSpecialAbility()
        {
            if (currentCooldown <= 0)
            {
                ActivateTimeSlow();
                currentCooldown = specialAbilityCooldown;
            }
        }

        private void ActivateTimeSlow()
        {
            isTimeSlowed = true;
            timeSlowTimer = 0f;
            Time.timeScale = timeSlowScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            
            animator?.SetTrigger("SlowTime");
            // TODO: Add time slow visual effect
        }

        private void DeactivateTimeSlow()
        {
            isTimeSlowed = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            
            // TODO: Remove time slow visual effect
        }

        private void HighlightObject(GameObject obj)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && highlightMaterial != null)
            {
                if (!originalMaterials.ContainsKey(obj))
                {
                    originalMaterials[obj] = renderer.material;
                    renderer.material = highlightMaterial;
                }
            }
        }

        private void ResetHighlight(GameObject obj)
        {
            if (originalMaterials.TryGetValue(obj, out Material originalMaterial))
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = originalMaterial;
                }
                originalMaterials.Remove(obj);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize scan radius
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, scanRadius);
        }

        private void OnDestroy()
        {
            // Reset time scale when destroyed
            if (isTimeSlowed)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
        }
    }

    public interface IPuzzleElement
    {
        void Reveal();
        bool IsActive { get; }
        PuzzleElementType ElementType { get; }
    }

    public enum PuzzleElementType
    {
        Switch,
        Lever,
        Pattern,
        Sequence
    }
} 