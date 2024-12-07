using UnityEngine;
using System.Collections.Generic;
using Forever.Characters;
using Forever.Environment;

namespace Forever.VFX
{
    public class WhisperingWoodsVFX : MonoBehaviour
    {
        public static WhisperingWoodsVFX Instance { get; private set; }

        [System.Serializable]
        public class MagicalPlantVFX
        {
            public ParticleSystem glowEffect;
            public ParticleSystem healingEffect;
            public ParticleSystem transformEffect;
            public ParticleSystem growthEffect;
            public ParticleSystem colorChangeEffect;
            public float glowIntensity = 1f;
            public Gradient colorGradient;
        }

        [System.Serializable]
        public class CrystalVFX
        {
            public ParticleSystem resonanceEffect;
            public ParticleSystem activationEffect;
            public ParticleSystem beamEffect;
            public LineRenderer beamRenderer;
            public Material beamMaterial;
            public float beamWidth = 0.2f;
            public float pulseSpeed = 1f;
            public Gradient beamGradient;
        }

        [System.Serializable]
        public class EnvironmentalVFX
        {
            public ParticleSystem[] floatingSpores;
            public ParticleSystem[] fireflies;
            public ParticleSystem[] magicalWisps;
            public ParticleSystem[] glowingMushrooms;
            public float spawnRadius = 20f;
            public float heightRange = 5f;
            public int maxActiveEffects = 20;
        }

        [Header("Plant Effects")]
        public MagicalPlantVFX plantVFX;
        
        [Header("Crystal Effects")]
        public CrystalVFX crystalVFX;
        
        [Header("Environmental Effects")]
        public EnvironmentalVFX environmentVFX;
        
        [Header("Weather Effects")]
        public ParticleSystem rainParticles;
        public ParticleSystem snowParticles;
        public ParticleSystem fogParticles;
        public ParticleSystem windParticles;
        public ParticleSystem magicalSparkles;

        private List<ParticleSystem> activeEnvironmentalEffects = new List<ParticleSystem>();
        private Dictionary<Transform, ParticleSystem> activeGlowEffects = new Dictionary<Transform, ParticleSystem>();
        private Camera mainCamera;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeEffects();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeEffects()
        {
            // Initialize all particle systems in stopped state
            StopAllEffects();
            
            // Ensure environmentVFX is properly initialized
            if (environmentVFX == null)
            {
                environmentVFX = new EnvironmentalVFX();
            }
        }
        
        public void StopAllEffects()
        {
            if (rainParticles) rainParticles.Stop();
            if (snowParticles) snowParticles.Stop();
            if (fogParticles) fogParticles.Stop();
            if (windParticles) windParticles.Stop();
            if (magicalSparkles) magicalSparkles.Stop();
            
            if (environmentVFX != null)
            {
                StopArrayEffects(environmentVFX.fireflies);
                StopArrayEffects(environmentVFX.floatingSpores);
                StopArrayEffects(environmentVFX.magicalWisps);
                StopArrayEffects(environmentVFX.glowingMushrooms);
            }
        }
        
        private void StopArrayEffects(ParticleSystem[] effects)
        {
            if (effects == null) return;
            foreach (var effect in effects)
            {
                if (effect) effect.Stop();
            }
        }
        
        public void SetWeatherEffects(WeatherType type, float intensity)
        {
            StopAllWeatherEffects();
            
            switch (type)
            {
                case WeatherType.Rain:
                    if (rainParticles)
                    {
                        var emission = rainParticles.emission;
                        emission.rateOverTime = 100f * intensity;
                        rainParticles.Play();
                    }
                    break;
                    
                case WeatherType.Snow:
                    if (snowParticles)
                    {
                        var emission = snowParticles.emission;
                        emission.rateOverTime = 50f * intensity;
                        snowParticles.Play();
                    }
                    break;
                    
                case WeatherType.Fog:
                    if (fogParticles)
                    {
                        var emission = fogParticles.emission;
                        emission.rateOverTime = 20f * intensity;
                        fogParticles.Play();
                    }
                    break;
                    
                case WeatherType.Wind:
                    if (windParticles)
                    {
                        var emission = windParticles.emission;
                        emission.rateOverTime = 30f * intensity;
                        windParticles.Play();
                    }
                    break;
            }
        }
        
        private void StopAllWeatherEffects()
        {
            if (rainParticles) rainParticles.Stop();
            if (snowParticles) snowParticles.Stop();
            if (fogParticles) fogParticles.Stop();
            if (windParticles) windParticles.Stop();
        }
        
        public void SetEnvironmentEffects(bool enabled, float intensity = 1f)
        {
            if (environmentVFX != null)
            {
                SetArrayEffects(environmentVFX.fireflies, enabled, intensity);
                SetArrayEffects(environmentVFX.floatingSpores, enabled, intensity);
                SetArrayEffects(environmentVFX.magicalWisps, enabled, intensity);
                SetArrayEffects(environmentVFX.glowingMushrooms, enabled, intensity);
            }
        }
        
        private void SetArrayEffects(ParticleSystem[] effects, bool enabled, float intensity)
        {
            if (effects == null) return;
            foreach (var effect in effects)
            {
                if (effect)
                {
                    if (enabled)
                    {
                        var emission = effect.emission;
                        emission.rateOverTime = emission.rateOverTime.constant * intensity;
                        effect.Play();
                    }
                    else
                    {
                        effect.Stop();
                    }
                }
            }
        }
        
        public void PlayMagicalEffect(Vector3 position, float intensity)
        {
            if (magicalSparkles)
            {
                magicalSparkles.transform.position = position;
                var emission = magicalSparkles.emission;
                emission.rateOverTime = 50f * intensity;
                magicalSparkles.Play();
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
} 