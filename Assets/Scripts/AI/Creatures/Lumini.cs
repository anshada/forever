using UnityEngine;
using System.Collections;
using Forever.VFX;

namespace Forever.AI.Creatures
{
    [RequireComponent(typeof(Light))]
    public class Lumini : MagicalCreature
    {
        [Header("Lumini Settings")]
        public float baseLightIntensity = 2f;
        public float maxLightIntensity = 5f;
        public float lightChangeSpeed = 2f;
        public float energyTransferRate = 10f;
        public float lightPulseFrequency = 1f;
        public float lightPulseAmplitude = 0.2f;
        
        [Header("Light Colors")]
        public Color calmColor = new Color(0.5f, 1f, 0.8f); // Soft cyan
        public Color excitedColor = new Color(1f, 0.8f, 0.2f); // Warm yellow
        public Color magicColor = new Color(0.8f, 0.4f, 1f); // Purple
        
        [Header("Swarm Behavior")]
        public float minDistanceToOthers = 2f;
        public float maxDistanceToOthers = 5f;
        public float alignmentWeight = 1f;
        public float cohesionWeight = 1f;
        public float separationWeight = 1f;
        public LayerMask luminiLayer;
        
        private Light lightComponent;
        private float targetLightIntensity;
        private float currentLightIntensity;
        private ParticleSystem.LightsModule lightsModule;
        
        protected override void Awake()
        {
            base.Awake();
            
            lightComponent = GetComponent<Light>();
            if (magicVFX != null)
                lightsModule = magicVFX.lights;
                
            // Initialize light settings
            lightComponent.intensity = baseLightIntensity;
            lightComponent.color = calmColor;
            targetLightIntensity = baseLightIntensity;
            currentLightIntensity = baseLightIntensity;
            
            // Customize behavior weights for Lumini
            curiosityWeight = 1.2f; // More curious than average
            sociabilityWeight = 1.5f; // Very social
            cautionWeight = 0.8f; // Less cautious
            
            creatureName = "Lumini";
        }
        
        protected override void Update()
        {
            base.Update();
            
            // Update light intensity
            UpdateLight();
            
            // Apply swarm behavior if other Lumini are nearby
            ApplySwarmBehavior();
        }
        
        private void UpdateLight()
        {
            // Calculate pulsing effect
            float pulseEffect = Mathf.Sin(Time.time * lightPulseFrequency) * lightPulseAmplitude;
            
            // Smoothly change light intensity
            currentLightIntensity = Mathf.Lerp(currentLightIntensity, targetLightIntensity, Time.deltaTime * lightChangeSpeed);
            float finalIntensity = Mathf.Clamp(currentLightIntensity + pulseEffect, 0, maxLightIntensity);
            
            // Apply intensity and color
            lightComponent.intensity = finalIntensity;
            lightComponent.color = Color.Lerp(calmColor, excitedColor, mood);
            
            // Update VFX lights if available
            if (magicVFX != null)
            {
                lightsModule.intensity = finalIntensity * 0.5f;
                lightsModule.light = lightComponent;
            }
        }
        
        private void ApplySwarmBehavior()
        {
            Collider[] nearbyLumini = Physics.OverlapSphere(transform.position, maxDistanceToOthers, luminiLayer);
            
            if (nearbyLumini.Length <= 1) return; // No other Lumini nearby
            
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int count = 0;
            
            foreach (Collider other in nearbyLumini)
            {
                if (other.gameObject == gameObject) continue;
                
                Vector3 otherPos = other.transform.position;
                float distance = Vector3.Distance(transform.position, otherPos);
                
                // Separation
                if (distance < minDistanceToOthers)
                {
                    Vector3 awayFromOther = transform.position - otherPos;
                    separation += awayFromOther.normalized / distance;
                }
                
                // Alignment
                Lumini otherLumini = other.GetComponent<Lumini>();
                if (otherLumini != null)
                {
                    alignment += otherLumini.transform.forward;
                }
                
                // Cohesion
                cohesion += otherPos;
                count++;
            }
            
            if (count > 0)
            {
                // Average and apply weights
                separation = separation.normalized * separationWeight;
                alignment = (alignment / count).normalized * alignmentWeight;
                cohesion = ((cohesion / count) - transform.position).normalized * cohesionWeight;
                
                // Combine forces
                Vector3 combinedForce = separation + alignment + cohesion;
                
                // Apply to movement
                if (combinedForce.magnitude > 0.1f)
                {
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(transform.position + combinedForce, out hit, wanderRadius, 1))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
            }
        }
        
        protected override void OnMagicCast(Vector3 target)
        {
            // Create a burst of light energy
            StartCoroutine(LightBurstRoutine(target));
        }
        
        private IEnumerator LightBurstRoutine(Vector3 target)
        {
            // Increase light intensity temporarily
            float originalIntensity = targetLightIntensity;
            targetLightIntensity = maxLightIntensity;
            lightComponent.color = magicColor;
            
            // Create expanding light wave
            if (magicVFX != null)
            {
                magicVFX.transform.LookAt(target);
                var main = magicVFX.main;
                main.startColor = magicColor;
            }
            
            // Transfer energy to nearby magical plants or crystals
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, magicCastRange, interactableLayer);
            foreach (Collider obj in nearbyObjects)
            {
                MagicalPlant plant = obj.GetComponent<MagicalPlant>();
                if (plant != null)
                {
                    plant.ReceiveEnergy(energyTransferRate * (1 - Vector3.Distance(transform.position, obj.transform.position) / magicCastRange));
                }
            }
            
            // Hold the burst
            yield return new WaitForSeconds(1f);
            
            // Gradually return to normal
            float returnTime = 2f;
            float elapsed = 0f;
            
            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnTime;
                
                targetLightIntensity = Mathf.Lerp(maxLightIntensity, originalIntensity, t);
                lightComponent.color = Color.Lerp(magicColor, GetMoodColor(), t);
                
                yield return null;
            }
            
            targetLightIntensity = originalIntensity;
        }
        
        public override void ReactToMagic(Vector3 source, float intensity)
        {
            base.ReactToMagic(source, intensity);
            
            // Lumini react to magic by temporarily increasing their light output
            targetLightIntensity = Mathf.Lerp(baseLightIntensity, maxLightIntensity, intensity);
        }
        
        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            
            // React to dark areas by increasing light intensity
            if (other.CompareTag("DarkArea"))
            {
                targetLightIntensity = maxLightIntensity * 0.8f;
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("DarkArea"))
            {
                targetLightIntensity = baseLightIntensity;
            }
        }
        
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // Draw swarm ranges
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, minDistanceToOthers);
            
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, maxDistanceToOthers);
        }
    }
} 