using UnityEngine;
using System.Collections;
using Forever.Characters;
using Forever.VFX;
using Forever.Audio;
using Forever.Interactables;

namespace Forever.AI
{
    public class MagicalCreature : AIBehaviorSystem
    {
        [Header("Creature Stats")]
        public string creatureName;
        public float maxHealth = 100f;
        public float currentHealth;
        public float magicalEnergy = 100f;
        public float regenerationRate = 5f;
        
        [Header("Magical Abilities")]
        public float magicCastRange = 8f;
        public float magicCooldown = 3f;
        public float magicCost = 20f;
        public ParticleSystem magicVFX;
        public AudioSource magicSFX;
        
        [Header("Interaction")]
        public float empathyRange = 12f;
        public float moodChangeRate = 0.1f;
        public float environmentalSensitivity = 1f;
        
        [Header("Visual Effects")]
        public MagicalAura auraEffect;
        public ParticleSystem emotionVFX;
        public Material creatureMaterial;
        
        protected float currentMagicCooldown;
        protected float mood;
        protected bool isChannelingMagic;
        protected Vector3 lastKnownMagicSource;
        
        protected override void Awake()
        {
            base.Awake();
            currentHealth = maxHealth;
            mood = 0.5f; // Neutral mood
            
            // Initialize material properties
            if (creatureMaterial != null)
            {
                creatureMaterial.SetFloat("_MagicIntensity", 0f);
                creatureMaterial.SetFloat("_EmissionIntensity", 0.5f);
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            // Update magic cooldown
            if (currentMagicCooldown > 0)
                currentMagicCooldown -= Time.deltaTime;
                
            // Regenerate magical energy
            if (magicalEnergy < 100f)
                magicalEnergy += regenerationRate * Time.deltaTime;
                
            // Update visual effects based on state and mood
            UpdateVisualEffects();
        }
        
        protected virtual void UpdateVisualEffects()
        {
            if (creatureMaterial != null)
            {
                // Update material properties based on magical energy and mood
                float magicIntensity = magicalEnergy / 100f;
                creatureMaterial.SetFloat("_MagicIntensity", magicIntensity);
                
                float emissionIntensity = 0.5f + (mood * 0.5f);
                creatureMaterial.SetFloat("_EmissionIntensity", emissionIntensity);
            }
            
            // Update aura effect
            if (auraEffect != null)
            {
                auraEffect.SetIntensity(mood);
                auraEffect.SetColor(GetMoodColor());
            }
        }
        
        protected virtual Color GetMoodColor()
        {
            // Convert mood (0-1) to color
            // Low mood (0) = Blue, Neutral (0.5) = Green, High mood (1) = Yellow
            return Color.Lerp(
                Color.Lerp(Color.blue, Color.green, mood * 2f),
                Color.yellow,
                Mathf.Max(0, (mood - 0.5f) * 2f)
            );
        }
        
        public virtual void CastMagic(Vector3 target)
        {
            if (currentMagicCooldown > 0 || magicalEnergy < magicCost)
                return;
                
            StartCoroutine(CastMagicRoutine(target));
        }
        
        protected virtual IEnumerator CastMagicRoutine(Vector3 target)
        {
            isChannelingMagic = true;
            
            // Start casting effects
            if (magicVFX != null)
                magicVFX.Play();
            if (magicSFX != null)
                magicSFX.Play();
                
            // Channel magic for a short duration
            float channelTime = 0.5f;
            while (channelTime > 0)
            {
                if (creatureMaterial != null)
                    creatureMaterial.SetFloat("_MagicIntensity", 1f);
                    
                channelTime -= Time.deltaTime;
                yield return null;
            }
            
            // Apply magic effect
            magicalEnergy -= magicCost;
            currentMagicCooldown = magicCooldown;
            
            // Implement specific magic effect in derived classes
            OnMagicCast(target);
            
            // End casting effects
            if (magicVFX != null)
                magicVFX.Stop();
                
            isChannelingMagic = false;
        }
        
        protected virtual void OnMagicCast(Vector3 target)
        {
            // Override in derived classes to implement specific magic effects
        }
        
        public virtual void ReactToMagic(Vector3 source, float intensity)
        {
            lastKnownMagicSource = source;
            
            // Adjust mood based on magic intensity
            float moodChange = intensity * environmentalSensitivity;
            mood = Mathf.Clamp01(mood + moodChange);
            
            // Show reaction VFX
            if (emotionVFX != null)
            {
                var emission = emotionVFX.emission;
                emission.rateOverTime = intensity * 10f;
                emotionVFX.Play();
            }
            
            // Potentially change state based on intensity
            if (intensity > 0.7f)
            {
                currentTarget = null;
                TransitionToState(AIState.React);
            }
        }
        
        public override void OnCharacterProximity(Character character)
        {
            base.OnCharacterProximity(character);
            
            // Additional creature-specific reactions
            if (character is Shibna)
            {
                // Shibna has special empathy with creatures
                float empathyBonus = 0.2f;
                mood = Mathf.Clamp01(mood + empathyBonus);
            }
            else if (character is Inaya)
            {
                // Inaya's agility makes creatures more playful
                float playfulnessBonus = 0.15f;
                mood = Mathf.Clamp01(mood + playfulnessBonus);
            }
        }
        
        public virtual void TakeDamage(float damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
            mood = Mathf.Max(0, mood - 0.2f); // Decrease mood when damaged
            
            if (currentHealth <= 0)
            {
                // Handle creature defeat/disappearance
                StartCoroutine(DisappearRoutine());
            }
        }
        
        protected virtual IEnumerator DisappearRoutine()
        {
            // Play disappearance effects
            if (magicVFX != null)
            {
                magicVFX.Play();
                yield return new WaitForSeconds(1f);
            }
            
            // Fade out
            float fadeTime = 1f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeTime);
                
                if (creatureMaterial != null)
                    creatureMaterial.SetFloat("_Alpha", alpha);
                    
                yield return null;
            }
            
            Destroy(gameObject);
        }
        
        protected virtual void OnTriggerEnter(Collider other)
        {
            // React to magical environmental effects
            MagicalPlant plant = other.GetComponent<MagicalPlant>();
            if (plant != null)
            {
                ReactToMagic(plant.transform.position, plant.magicalIntensity);
            }
        }
        
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // Draw magic cast range
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, magicCastRange);
            
            // Draw empathy range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, empathyRange);
        }
    }
} 