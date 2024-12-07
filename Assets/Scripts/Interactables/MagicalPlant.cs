using UnityEngine;
using Forever.VFX;

namespace Forever.Interactables
{
    public class MagicalPlant : MonoBehaviour
    {
        [Header("Plant Settings")]
        public float magicalIntensity = 1f;
        public float energyCapacity = 100f;
        public float currentEnergy;
        public float energyDecayRate = 0.1f;
        public float minIntensityThreshold = 0.2f;
        
        [Header("Visual Effects")]
        public ParticleSystem energyVFX;
        public MagicalAura plantAura;
        public Material plantMaterial;
        public float glowIntensityMultiplier = 1f;
        
        [Header("Growth")]
        public float maxGrowthScale = 2f;
        public float growthRate = 0.5f;
        public AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        private Vector3 originalScale;
        private float currentGrowthProgress;
        private bool isFullyGrown;
        
        private void Awake()
        {
            originalScale = transform.localScale;
            currentEnergy = energyCapacity * 0.5f; // Start at half capacity
            
            if (plantMaterial == null && GetComponent<Renderer>())
            {
                plantMaterial = GetComponent<Renderer>().material;
            }
            
            UpdateVisuals();
        }
        
        private void Update()
        {
            // Decay energy over time
            if (currentEnergy > 0)
            {
                currentEnergy = Mathf.Max(0, currentEnergy - (energyDecayRate * Time.deltaTime));
                UpdateVisuals();
            }
            
            // Update growth
            if (!isFullyGrown && currentEnergy > energyCapacity * 0.8f)
            {
                UpdateGrowth();
            }
        }
        
        public void ReceiveEnergy(float amount)
        {
            float previousEnergy = currentEnergy;
            currentEnergy = Mathf.Min(energyCapacity, currentEnergy + amount);
            
            // Trigger growth if energy threshold is reached
            if (previousEnergy < energyCapacity * 0.8f && currentEnergy >= energyCapacity * 0.8f)
            {
                StartGrowth();
            }
            
            UpdateVisuals();
        }
        
        private void UpdateVisuals()
        {
            float normalizedEnergy = currentEnergy / energyCapacity;
            
            // Update material properties
            if (plantMaterial != null)
            {
                plantMaterial.SetFloat("_GlowIntensity", normalizedEnergy * glowIntensityMultiplier);
                plantMaterial.SetFloat("_MagicIntensity", normalizedEnergy);
            }
            
            // Update particle effects
            if (energyVFX != null)
            {
                var emission = energyVFX.emission;
                emission.rateOverTime = normalizedEnergy * 20f;
                
                var main = energyVFX.main;
                main.startLifetime = normalizedEnergy * 2f;
            }
            
            // Update aura
            if (plantAura != null)
            {
                plantAura.SetIntensity(normalizedEnergy);
                plantAura.SetColor(Color.Lerp(Color.cyan, Color.green, normalizedEnergy));
            }
        }
        
        private void StartGrowth()
        {
            if (!isFullyGrown)
            {
                currentGrowthProgress = 0f;
            }
        }
        
        private void UpdateGrowth()
        {
            if (currentGrowthProgress < 1f)
            {
                currentGrowthProgress += growthRate * Time.deltaTime;
                float growthScale = growthCurve.Evaluate(currentGrowthProgress);
                transform.localScale = Vector3.Lerp(originalScale, originalScale * maxGrowthScale, growthScale);
                
                if (currentGrowthProgress >= 1f)
                {
                    isFullyGrown = true;
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Visualize magical influence range
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, magicalIntensity * 2f);
        }
    }
} 