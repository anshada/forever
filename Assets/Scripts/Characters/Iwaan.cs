using UnityEngine;
using System.Collections.Generic;

namespace Forever.Characters
{
    public class Iwaan : Character
    {
        [Header("Creativity Abilities")]
        public float paintRadius = 10f;
        public float transformDuration = 6f;
        public LayerMask transformableLayer;
        public Color[] creativeColors;
        
        private bool isTransformed = false;
        private float transformTimer = 0f;
        private List<ITransformable> transformedObjects = new List<ITransformable>();
        private ParticleSystem paintParticles;

        [SerializeField]
        private GameObject paintBrushEffect;

        protected override void Awake()
        {
            base.Awake();
            characterType = CharacterType.Iwaan;
            characterName = "Iwaan";
            paintParticles = GetComponentInChildren<ParticleSystem>();
        }

        protected override void Update()
        {
            base.Update();
            UpdateTransformation();
            HandlePainting();
        }

        private void UpdateTransformation()
        {
            if (isTransformed)
            {
                transformTimer += Time.deltaTime;
                if (transformTimer >= transformDuration)
                {
                    RevertTransformations();
                }
            }
        }

        private void HandlePainting()
        {
            if (Input.GetMouseButton(0) && !isTransformed)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, paintRadius))
                {
                    IPaintable paintable = hit.collider.GetComponent<IPaintable>();
                    if (paintable != null)
                    {
                        Paint(paintable, hit.point);
                    }
                }
            }
        }

        protected override void UseSpecialAbility()
        {
            if (currentCooldown <= 0)
            {
                TransformEnvironment();
                currentCooldown = specialAbilityCooldown;
            }
        }

        private void Paint(IPaintable paintable, Vector3 position)
        {
            Color randomColor = creativeColors[Random.Range(0, creativeColors.Length)];
            paintable.Paint(randomColor);
            
            // Spawn paint effect
            if (paintBrushEffect != null)
            {
                GameObject effect = Instantiate(paintBrushEffect, position, Quaternion.identity);
                ParticleSystem particles = effect.GetComponent<ParticleSystem>();
                var main = particles.main;
                main.startColor = randomColor;
                Destroy(effect, particles.main.duration);
            }

            animator?.SetTrigger("Paint");
        }

        private void TransformEnvironment()
        {
            isTransformed = true;
            transformTimer = 0f;
            
            // Find and transform nearby objects
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, paintRadius, transformableLayer);
            foreach (var hitCollider in hitColliders)
            {
                ITransformable transformable = hitCollider.GetComponent<ITransformable>();
                if (transformable != null && !transformedObjects.Contains(transformable))
                {
                    transformable.Transform();
                    transformedObjects.Add(transformable);
                }
            }

            // Start particle effect
            if (paintParticles != null)
            {
                paintParticles.Play();
            }

            animator?.SetTrigger("Transform");
        }

        private void RevertTransformations()
        {
            isTransformed = false;
            
            // Revert all transformed objects
            foreach (var transformable in transformedObjects)
            {
                if (transformable != null)
                {
                    transformable.Revert();
                }
            }
            transformedObjects.Clear();

            // Stop particle effect
            if (paintParticles != null)
            {
                paintParticles.Stop();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize paint radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, paintRadius);
        }
    }

    public interface IPaintable
    {
        void Paint(Color color);
        void ClearPaint();
    }

    public interface ITransformable
    {
        void Transform();
        void Revert();
        bool IsTransformed { get; }
    }
} 