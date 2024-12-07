using UnityEngine;
using System.Collections.Generic;
using Forever.Characters;

namespace Forever.VFX
{
    public class WhisperingWoodsVFX : MonoBehaviour
    {
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
        public ParticleSystem rainEffect;
        public ParticleSystem mistEffect;
        public ParticleSystem magicalAuraEffect;

        private List<ParticleSystem> activeEnvironmentalEffects = new List<ParticleSystem>();
        private Dictionary<Transform, ParticleSystem> activeGlowEffects = new Dictionary<Transform, ParticleSystem>();
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
            InitializeEnvironmentalEffects();
        }

        private void InitializeEnvironmentalEffects()
        {
            // Spawn initial environmental effects
            SpawnEnvironmentalEffects(environmentVFX.floatingSpores, 5);
            SpawnEnvironmentalEffects(environmentVFX.fireflies, 8);
            SpawnEnvironmentalEffects(environmentVFX.magicalWisps, 3);
            SpawnEnvironmentalEffects(environmentVFX.glowingMushrooms, 10);

            // Start weather effects
            if (mistEffect != null)
            {
                mistEffect.Play();
            }
        }

        private void SpawnEnvironmentalEffects(ParticleSystem[] effectPrefabs, int count)
        {
            if (effectPrefabs == null || effectPrefabs.Length == 0) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 randomPosition = GetRandomSpawnPosition();
                ParticleSystem effectPrefab = effectPrefabs[Random.Range(0, effectPrefabs.Length)];
                
                ParticleSystem instance = Instantiate(effectPrefab, randomPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                instance.Play();
                activeEnvironmentalEffects.Add(instance);
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * environmentVFX.spawnRadius;
            Vector3 position = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            position.y += Random.Range(0f, environmentVFX.heightRange);
            
            // Raycast to find ground position
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                position.y = hit.point.y;
            }
            
            return position;
        }

        public void OnMagicalPlantInteraction(Transform plant, CharacterType characterType)
        {
            switch (characterType)
            {
                case CharacterType.Shibna:
                    PlayHealingEffect(plant);
                    break;
                case CharacterType.Iwaan:
                    PlayColorChangeEffect(plant);
                    break;
                case CharacterType.Anshad:
                    PlayTransformEffect(plant);
                    break;
                case CharacterType.Inaya:
                    PlayGrowthEffect(plant);
                    break;
                case CharacterType.Ilan:
                    PlayAnalysisEffect(plant);
                    break;
            }
        }

        private void PlayHealingEffect(Transform target)
        {
            if (plantVFX.healingEffect != null)
            {
                ParticleSystem healingInstance = Instantiate(plantVFX.healingEffect, target.position, Quaternion.identity);
                healingInstance.Play();
                Destroy(healingInstance.gameObject, healingInstance.main.duration);
            }
        }

        private void PlayColorChangeEffect(Transform target)
        {
            if (plantVFX.colorChangeEffect != null)
            {
                ParticleSystem colorInstance = Instantiate(plantVFX.colorChangeEffect, target.position, Quaternion.identity);
                var mainModule = colorInstance.main;
                mainModule.startColor = plantVFX.colorGradient.Evaluate(Random.value);
                colorInstance.Play();
                Destroy(colorInstance.gameObject, mainModule.duration);
            }
        }

        private void PlayTransformEffect(Transform target)
        {
            if (plantVFX.transformEffect != null)
            {
                ParticleSystem transformInstance = Instantiate(plantVFX.transformEffect, target.position, Quaternion.identity);
                transformInstance.Play();
                Destroy(transformInstance.gameObject, transformInstance.main.duration);
            }
        }

        private void PlayGrowthEffect(Transform target)
        {
            if (plantVFX.growthEffect != null)
            {
                ParticleSystem growthInstance = Instantiate(plantVFX.growthEffect, target.position, Quaternion.identity);
                growthInstance.transform.SetParent(target);
                growthInstance.Play();
                StartCoroutine(ScaleEffectWithTarget(growthInstance, target));
            }
        }

        private void PlayAnalysisEffect(Transform target)
        {
            if (plantVFX.glowEffect != null)
            {
                if (!activeGlowEffects.ContainsKey(target))
                {
                    ParticleSystem glowInstance = Instantiate(plantVFX.glowEffect, target.position, Quaternion.identity);
                    glowInstance.transform.SetParent(target);
                    var emission = glowInstance.emission;
                    emission.rateOverTime = plantVFX.glowIntensity;
                    glowInstance.Play();
                    activeGlowEffects.Add(target, glowInstance);
                    StartCoroutine(FadeGlowEffect(target, glowInstance));
                }
            }
        }

        private System.Collections.IEnumerator ScaleEffectWithTarget(ParticleSystem effect, Transform target)
        {
            Vector3 initialScale = target.localScale;
            float duration = effect.main.duration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 2f, elapsed / duration);
                effect.transform.localScale = initialScale * scale;
                yield return null;
            }

            Destroy(effect.gameObject);
        }

        private System.Collections.IEnumerator FadeGlowEffect(Transform target, ParticleSystem glowEffect)
        {
            float duration = 3f;
            float elapsed = 0f;
            var emission = glowEffect.emission;
            float initialRate = emission.rateOverTime.constant;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                emission.rateOverTime = Mathf.Lerp(initialRate, 0f, elapsed / duration);
                yield return null;
            }

            activeGlowEffects.Remove(target);
            Destroy(glowEffect.gameObject);
        }

        public void PlayCrystalEffect(Transform crystal, bool activate)
        {
            if (activate)
            {
                if (crystalVFX.activationEffect != null)
                {
                    ParticleSystem activationInstance = Instantiate(crystalVFX.activationEffect, crystal.position, Quaternion.identity);
                    activationInstance.Play();
                    Destroy(activationInstance.gameObject, activationInstance.main.duration);
                }
            }
            else
            {
                if (crystalVFX.resonanceEffect != null)
                {
                    ParticleSystem resonanceInstance = Instantiate(crystalVFX.resonanceEffect, crystal.position, Quaternion.identity);
                    resonanceInstance.Play();
                    Destroy(resonanceInstance.gameObject, resonanceInstance.main.duration);
                }
            }
        }

        public void CreateCrystalBeam(Transform startCrystal, Transform endCrystal)
        {
            if (crystalVFX.beamEffect != null && crystalVFX.beamRenderer != null)
            {
                GameObject beamObj = new GameObject("CrystalBeam");
                LineRenderer beamRenderer = beamObj.AddComponent<LineRenderer>();
                beamRenderer.material = crystalVFX.beamMaterial;
                beamRenderer.startWidth = crystalVFX.beamWidth;
                beamRenderer.endWidth = crystalVFX.beamWidth;
                beamRenderer.positionCount = 2;
                beamRenderer.useWorldSpace = true;

                StartCoroutine(AnimateBeam(beamRenderer, startCrystal, endCrystal));
            }
        }

        private System.Collections.IEnumerator AnimateBeam(LineRenderer beam, Transform start, Transform end)
        {
            float elapsed = 0f;
            
            while (true)
            {
                elapsed += Time.deltaTime * crystalVFX.pulseSpeed;
                
                beam.SetPosition(0, start.position);
                beam.SetPosition(1, end.position);
                
                float pulseValue = Mathf.PingPong(elapsed, 1f);
                beam.startColor = crystalVFX.beamGradient.Evaluate(pulseValue);
                beam.endColor = crystalVFX.beamGradient.Evaluate((pulseValue + 0.5f) % 1f);
                
                yield return null;
            }
        }

        public void SetWeatherEffect(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Clear:
                    if (rainEffect != null) rainEffect.Stop();
                    if (mistEffect != null) mistEffect.Stop();
                    break;
                case WeatherType.Rain:
                    if (rainEffect != null) rainEffect.Play();
                    break;
                case WeatherType.Mist:
                    if (mistEffect != null) mistEffect.Play();
                    break;
            }
        }

        private void OnDestroy()
        {
            // Cleanup active effects
            foreach (var effect in activeEnvironmentalEffects)
            {
                if (effect != null)
                {
                    Destroy(effect.gameObject);
                }
            }
            
            foreach (var effect in activeGlowEffects.Values)
            {
                if (effect != null)
                {
                    Destroy(effect.gameObject);
                }
            }
        }
    }

    public enum WeatherType
    {
        Clear,
        Rain,
        Mist
    }
} 