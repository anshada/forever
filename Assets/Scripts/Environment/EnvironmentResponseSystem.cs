using UnityEngine;
using System.Collections.Generic;
using Forever.VFX;
using Forever.AI;
using Forever.Core;
using Forever.Rendering;
using Forever.Audio;

namespace Forever.Environment
{
    public class EnvironmentResponseSystem : MonoBehaviour
    {
        public static EnvironmentResponseSystem Instance { get; private set; }

        [Header("Response Settings")]
        public float responseRadius = 10f;
        public float magicPropagationSpeed = 5f;
        public float magicDecayRate = 0.5f;
        public LayerMask responsiveLayerMask;
        
        [Header("Visual Effects")]
        public ParticleSystem magicPropagationVFX;
        public MagicalAura environmentalAura;
        public float vfxIntensityMultiplier = 1f;
        
        [Header("Audio")]
        public AudioSource environmentalAudioSource;
        public AudioClip[] magicResponseSounds;
        public float minPitchVariation = 0.9f;
        public float maxPitchVariation = 1.1f;
        
        private Dictionary<Transform, float> activeResponses;
        private List<MagicalNode> magicNodes;
        private WeatherSystem weatherSystem;
        private ShaderManager shaderManager;
        private ParticleSystemManager vfxManager;
        private AudioManager audioManager;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSystems();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSystems()
        {
            activeResponses = new Dictionary<Transform, float>();
            magicNodes = new List<MagicalNode>();
            
            // Get references to required systems
            weatherSystem = FindObjectOfType<WeatherSystem>();
            shaderManager = FindObjectOfType<ShaderManager>();
            vfxManager = FindObjectOfType<ParticleSystemManager>();
            audioManager = FindObjectOfType<AudioManager>();
            
            // Initialize audio
            if (environmentalAudioSource == null)
            {
                environmentalAudioSource = gameObject.AddComponent<AudioSource>();
                environmentalAudioSource.spatialBlend = 1f;
                environmentalAudioSource.rolloffMode = AudioRolloffMode.Linear;
                environmentalAudioSource.maxDistance = responseRadius * 2f;
            }
        }
        
        private void Update()
        {
            UpdateActiveResponses();
            UpdateMagicNodes();
        }
        
        private void UpdateActiveResponses()
        {
            List<Transform> completedResponses = new List<Transform>();
            
            foreach (var response in activeResponses)
            {
                float newIntensity = response.Value - (magicDecayRate * Time.deltaTime);
                
                if (newIntensity <= 0)
                {
                    completedResponses.Add(response.Key);
                }
                else
                {
                    activeResponses[response.Key] = newIntensity;
                    UpdateEnvironmentalEffects(response.Key, newIntensity);
                }
            }
            
            foreach (var completed in completedResponses)
            {
                activeResponses.Remove(completed);
            }
        }
        
        private void UpdateMagicNodes()
        {
            foreach (var node in magicNodes)
            {
                node.UpdateNode();
            }
        }
        
        public void TriggerResponse(Vector3 position, float intensity, ResponseType type)
        {
            // Find all responsive objects in range
            Collider[] affectedObjects = Physics.OverlapSphere(position, responseRadius, responsiveLayerMask);
            
            foreach (var obj in affectedObjects)
            {
                float distance = Vector3.Distance(position, obj.transform.position);
                float scaledIntensity = intensity * (1 - (distance / responseRadius));
                
                // Add or update response
                if (scaledIntensity > 0)
                {
                    if (activeResponses.ContainsKey(obj.transform))
                    {
                        activeResponses[obj.transform] = Mathf.Max(activeResponses[obj.transform], scaledIntensity);
                    }
                    else
                    {
                        activeResponses.Add(obj.transform, scaledIntensity);
                    }
                    
                    // Trigger specific responses based on type
                    HandleSpecificResponse(obj, scaledIntensity, type);
                }
            }
            
            // Trigger visual and audio effects
            SpawnResponseEffects(position, intensity, type);
            
            // Notify weather system
            if (weatherSystem != null)
            {
                weatherSystem.OnMagicalDisturbance(position, intensity);
            }
        }
        
        private void HandleSpecificResponse(Collider obj, float intensity, ResponseType type)
        {
            // Check for different types of responsive objects
            MagicalPlant plant = obj.GetComponent<MagicalPlant>();
            if (plant != null)
            {
                plant.ReceiveEnergy(intensity);
            }
            
            MagicalCreature creature = obj.GetComponent<MagicalCreature>();
            if (creature != null)
            {
                creature.ReactToMagic(obj.transform.position, intensity);
            }
            
            // Update shader properties
            if (shaderManager != null)
            {
                shaderManager.UpdateMagicInteraction(obj.transform.position, intensity);
            }
        }
        
        private void SpawnResponseEffects(Vector3 position, float intensity, ResponseType type)
        {
            // Spawn propagation VFX
            if (magicPropagationVFX != null)
            {
                ParticleSystem vfx = Instantiate(magicPropagationVFX, position, Quaternion.identity);
                var main = vfx.main;
                main.startSize = responseRadius * 2f;
                main.startLifetime = responseRadius / magicPropagationSpeed;
                
                var emission = vfx.emission;
                emission.rateOverTime = intensity * vfxIntensityMultiplier;
                
                if (vfxManager != null)
                {
                    vfxManager.RegisterEffect(vfx);
                }
            }
            
            // Update environmental aura
            if (environmentalAura != null)
            {
                environmentalAura.SetIntensity(intensity);
                environmentalAura.transform.position = position;
            }
            
            // Play response sound
            if (magicResponseSounds != null && magicResponseSounds.Length > 0)
            {
                AudioClip clip = magicResponseSounds[Random.Range(0, magicResponseSounds.Length)];
                if (audioManager != null)
                {
                    audioManager.PlaySoundAtPosition(clip, position, intensity);
                }
                else
                {
                    environmentalAudioSource.pitch = Random.Range(minPitchVariation, maxPitchVariation);
                    environmentalAudioSource.PlayOneShot(clip, intensity);
                }
            }
        }
        
        public void RegisterMagicNode(MagicalNode node)
        {
            if (!magicNodes.Contains(node))
            {
                magicNodes.Add(node);
            }
        }
        
        public void UnregisterMagicNode(MagicalNode node)
        {
            magicNodes.Remove(node);
        }
        
        public float GetResponseIntensity(Transform target)
        {
            float intensity;
            return activeResponses.TryGetValue(target, out intensity) ? intensity : 0f;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            foreach (var response in activeResponses)
            {
                Gizmos.DrawWireSphere(response.Key.position, response.Value * responseRadius);
            }
        }
    }
    
    public enum ResponseType
    {
        Magic,
        Light,
        Nature,
        Crystal,
        Water,
        Wind
    }
    
    [System.Serializable]
    public class MagicalNode
    {
        public Transform transform;
        public float baseEnergy;
        public float currentEnergy;
        public float energyCapacity;
        public float regenerationRate;
        public float transferRadius;
        public LayerMask transferMask;
        
        private float lastUpdateTime;
        
        public void UpdateNode()
        {
            // Regenerate energy
            if (currentEnergy < energyCapacity)
            {
                currentEnergy = Mathf.Min(energyCapacity, currentEnergy + (regenerationRate * Time.deltaTime));
            }
            
            // Transfer energy to nearby nodes
            if (Time.time - lastUpdateTime >= 1f)
            {
                TransferEnergy();
                lastUpdateTime = Time.time;
            }
        }
        
        private void TransferEnergy()
        {
            if (currentEnergy <= 0) return;
            
            Collider[] nearbyNodes = Physics.OverlapSphere(transform.position, transferRadius, transferMask);
            if (nearbyNodes.Length <= 1) return;
            
            float energyPerNode = (currentEnergy * 0.1f) / (nearbyNodes.Length - 1);
            
            foreach (var other in nearbyNodes)
            {
                if (other.transform == transform) continue;
                
                MagicalNode otherNode = other.GetComponent<MagicalNode>();
                if (otherNode != null && otherNode.currentEnergy < otherNode.energyCapacity)
                {
                    float transferAmount = Mathf.Min(energyPerNode, otherNode.energyCapacity - otherNode.currentEnergy);
                    otherNode.currentEnergy += transferAmount;
                    currentEnergy -= transferAmount;
                }
            }
        }
    }
} 