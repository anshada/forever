using UnityEngine;
using System.Collections.Generic;
using Forever.Characters;

namespace Forever.VFX
{
    public class ParticleSystemManager : MonoBehaviour
    {
        public static ParticleSystemManager Instance { get; private set; }

        [System.Serializable]
        public class ParticleEffectPreset
        {
            public string effectName;
            public GameObject particlePrefab;
            public float duration = 2f;
            public bool autoDestroy = true;
            public bool useWorldSpace = true;
            [Range(0.1f, 2f)]
            public float baseScale = 1f;
            public AudioClip soundEffect;
        }

        [System.Serializable]
        public class CharacterEffects
        {
            public CharacterType characterType;
            public ParticleEffectPreset[] abilities;
            public ParticleEffectPreset movement;
            public ParticleEffectPreset interaction;
        }

        [Header("Character VFX")]
        public CharacterEffects[] characterEffects;

        [Header("Environmental VFX")]
        public ParticleEffectPreset[] environmentalEffects;
        public ParticleEffectPreset[] weatherEffects;
        public ParticleEffectPreset[] magicalEffects;

        [Header("Pooling Settings")]
        public int defaultPoolSize = 10;
        public bool autoExpandPool = true;

        private Dictionary<string, Queue<GameObject>> particlePool;
        private Dictionary<string, ParticleEffectPreset> effectPresets;
        private List<ParticleSystem> activeEffects;
        private Transform poolContainer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeParticleSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeParticleSystem()
        {
            particlePool = new Dictionary<string, Queue<GameObject>>();
            effectPresets = new Dictionary<string, ParticleEffectPreset>();
            activeEffects = new List<ParticleSystem>();

            // Create pool container
            poolContainer = new GameObject("ParticlePool").transform;
            poolContainer.SetParent(transform);

            // Initialize effect presets dictionary
            RegisterEffectPresets();

            // Pre-populate particle pools
            foreach (var preset in effectPresets.Values)
            {
                CreateParticlePool(preset);
            }
        }

        private void RegisterEffectPresets()
        {
            // Register character effects
            foreach (var charEffect in characterEffects)
            {
                foreach (var ability in charEffect.abilities)
                {
                    RegisterPreset(ability);
                }
                RegisterPreset(charEffect.movement);
                RegisterPreset(charEffect.interaction);
            }

            // Register environmental effects
            foreach (var effect in environmentalEffects)
            {
                RegisterPreset(effect);
            }

            // Register weather effects
            foreach (var effect in weatherEffects)
            {
                RegisterPreset(effect);
            }

            // Register magical effects
            foreach (var effect in magicalEffects)
            {
                RegisterPreset(effect);
            }
        }

        private void RegisterPreset(ParticleEffectPreset preset)
        {
            if (preset != null && !effectPresets.ContainsKey(preset.effectName))
            {
                effectPresets.Add(preset.effectName, preset);
            }
        }

        private void CreateParticlePool(ParticleEffectPreset preset)
        {
            if (!particlePool.ContainsKey(preset.effectName))
            {
                Queue<GameObject> pool = new Queue<GameObject>();
                for (int i = 0; i < defaultPoolSize; i++)
                {
                    CreatePooledParticle(preset, pool);
                }
                particlePool.Add(preset.effectName, pool);
            }
        }

        private void CreatePooledParticle(ParticleEffectPreset preset, Queue<GameObject> pool)
        {
            GameObject instance = Instantiate(preset.particlePrefab, poolContainer);
            instance.SetActive(false);
            pool.Enqueue(instance);
        }

        public ParticleSystem PlayEffect(string effectName, Vector3 position, Quaternion rotation = default, Transform parent = null)
        {
            if (!effectPresets.TryGetValue(effectName, out ParticleEffectPreset preset))
            {
                Debug.LogWarning($"Effect preset '{effectName}' not found!");
                return null;
            }

            GameObject particleObj = GetParticleFromPool(preset);
            if (particleObj == null) return null;

            // Setup particle object
            particleObj.transform.position = position;
            particleObj.transform.rotation = rotation == default ? Quaternion.identity : rotation;
            particleObj.transform.localScale = Vector3.one * preset.baseScale;
            
            if (parent != null && !preset.useWorldSpace)
            {
                particleObj.transform.SetParent(parent);
            }

            // Activate and play
            particleObj.SetActive(true);
            var particleSystem = particleObj.GetComponent<ParticleSystem>();
            particleSystem.Play(true);
            activeEffects.Add(particleSystem);

            // Play sound effect if available
            if (preset.soundEffect != null)
            {
                Audio.AudioManager.Instance.PlaySoundAtPosition(preset.soundEffect.name, position);
            }

            // Handle cleanup
            if (preset.autoDestroy)
            {
                StartCoroutine(ReturnToPool(particleSystem, preset));
            }

            return particleSystem;
        }

        private GameObject GetParticleFromPool(ParticleEffectPreset preset)
        {
            if (!particlePool.TryGetValue(preset.effectName, out Queue<GameObject> pool))
            {
                CreateParticlePool(preset);
                pool = particlePool[preset.effectName];
            }

            if (pool.Count == 0 && autoExpandPool)
            {
                CreatePooledParticle(preset, pool);
            }

            return pool.Count > 0 ? pool.Dequeue() : null;
        }

        private System.Collections.IEnumerator ReturnToPool(ParticleSystem particleSystem, ParticleEffectPreset preset)
        {
            yield return new WaitForSeconds(preset.duration);

            if (particleSystem != null)
            {
                ReturnParticleToPool(particleSystem.gameObject, preset.effectName);
                activeEffects.Remove(particleSystem);
            }
        }

        public void ReturnParticleToPool(GameObject particleObj, string effectName)
        {
            if (particlePool.TryGetValue(effectName, out Queue<GameObject> pool))
            {
                particleObj.SetActive(false);
                particleObj.transform.SetParent(poolContainer);
                pool.Enqueue(particleObj);
            }
        }

        public void StopEffect(ParticleSystem particleSystem, bool immediate = false)
        {
            if (particleSystem != null)
            {
                if (immediate)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ReturnParticleToPool(particleSystem.gameObject, GetEffectName(particleSystem));
                }
                else
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        public void StopAllEffects(bool immediate = false)
        {
            foreach (var effect in activeEffects.ToArray())
            {
                StopEffect(effect, immediate);
            }
            activeEffects.Clear();
        }

        private string GetEffectName(ParticleSystem particleSystem)
        {
            foreach (var kvp in effectPresets)
            {
                if (kvp.Value.particlePrefab.GetComponent<ParticleSystem>() == particleSystem)
                {
                    return kvp.Key;
                }
            }
            return string.Empty;
        }

        public ParticleSystem PlayCharacterEffect(CharacterType characterType, string abilityName, Vector3 position, Transform parent = null)
        {
            foreach (var charEffect in characterEffects)
            {
                if (charEffect.characterType == characterType)
                {
                    foreach (var ability in charEffect.abilities)
                    {
                        if (ability.effectName == abilityName)
                        {
                            return PlayEffect(ability.effectName, position, default, parent);
                        }
                    }
                }
            }
            return null;
        }

        public void PlayWeatherEffect(string effectName, Vector3 position)
        {
            foreach (var effect in weatherEffects)
            {
                if (effect.effectName == effectName)
                {
                    PlayEffect(effect.effectName, position);
                    break;
                }
            }
        }

        public ParticleSystem PlayMagicalEffect(string effectName, Vector3 position, float scale = 1f)
        {
            foreach (var effect in magicalEffects)
            {
                if (effect.effectName == effectName)
                {
                    var particleSystem = PlayEffect(effect.effectName, position);
                    if (particleSystem != null)
                    {
                        particleSystem.transform.localScale *= scale;
                    }
                    return particleSystem;
                }
            }
            return null;
        }

        private void OnDestroy()
        {
            StopAllEffects(true);
        }
    }
} 