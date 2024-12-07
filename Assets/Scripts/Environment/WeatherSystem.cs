using UnityEngine;
using Forever.VFX;
using Forever.Audio;

namespace Forever.Environment
{
    public class WeatherSystem : MonoBehaviour
    {
        [System.Serializable]
        public class TimeOfDaySettings
        {
            public Color ambientColor;
            public Color fogColor;
            public float fogDensity;
            public float shadowStrength;
            public float bloomIntensity;
            public AnimationCurve lightIntensityCurve;
        }

        [System.Serializable]
        public class WeatherSettings
        {
            public WeatherType type;
            public float transitionDuration = 2f;
            public float minDuration = 60f;
            public float maxDuration = 300f;
            public Color fogColor;
            public float fogDensity;
            public float windIntensity;
            public AudioClip[] weatherSounds;
            [Range(0f, 1f)]
            public float probability = 0.3f;
        }

        [Header("Time Settings")]
        public float dayLength = 1200f; // 20 minutes in real time
        public float startTime = 8f; // Start at 8 AM
        [Range(0f, 1f)]
        public float timeOfDay = 0f;
        public bool freezeTime = false;

        [Header("Time of Day")]
        public Light sunLight;
        public Light moonLight;
        public TimeOfDaySettings daySettings;
        public TimeOfDaySettings nightSettings;
        public Material skyboxMaterial;
        
        [Header("Weather")]
        public WeatherSettings[] weatherSettings;
        public WeatherType currentWeather = WeatherType.Clear;
        public float weatherChangeDelay = 30f;
        
        [Header("Environment Response")]
        public float grassSwayAmount = 1f;
        public float treeSwayAmount = 0.5f;
        public Material grassMaterial;
        public Material[] treeMaterials;

        private float currentWeatherDuration;
        private float weatherTimer;
        private WeatherSettings currentWeatherSettings;
        private AudioSource weatherAudioSource;
        private ParticleSystem[] weatherParticleSystems;

        private void Start()
        {
            InitializeWeatherSystem();
        }

        private void InitializeWeatherSystem()
        {
            // Setup audio source for weather sounds
            weatherAudioSource = gameObject.AddComponent<AudioSource>();
            weatherAudioSource.loop = true;
            weatherAudioSource.spatialBlend = 0f; // 2D sound
            weatherAudioSource.priority = 0; // High priority

            // Get weather particle systems
            weatherParticleSystems = GetComponentsInChildren<ParticleSystem>();

            // Initialize time of day
            timeOfDay = startTime / 24f;
            UpdateTimeOfDay();

            // Start with clear weather
            SetWeather(WeatherType.Clear);

            // Start weather cycle
            StartCoroutine(WeatherCycle());
        }

        private void Update()
        {
            if (!freezeTime)
            {
                // Update time of day
                timeOfDay += Time.deltaTime / dayLength;
                if (timeOfDay >= 1f)
                    timeOfDay = 0f;

                UpdateTimeOfDay();
            }

            // Update weather effects
            if (currentWeatherSettings != null)
            {
                weatherTimer += Time.deltaTime;
                if (weatherTimer >= currentWeatherDuration)
                {
                    StartCoroutine(WeatherCycle());
                }
            }
        }

        private void UpdateTimeOfDay()
        {
            // Calculate sun and moon positions
            float sunRotation = timeOfDay * 360f;
            sunLight.transform.rotation = Quaternion.Euler(sunRotation, 0f, 0f);
            moonLight.transform.rotation = Quaternion.Euler(sunRotation + 180f, 0f, 0f);

            // Determine if it's day or night
            bool isDay = timeOfDay > 0.25f && timeOfDay < 0.75f;
            TimeOfDaySettings currentSettings = isDay ? daySettings : nightSettings;

            // Interpolate lighting settings
            float transitionProgress = Mathf.InverseLerp(0.25f, 0.75f, timeOfDay);
            Color ambientColor = Color.Lerp(nightSettings.ambientColor, daySettings.ambientColor, transitionProgress);
            Color fogColor = Color.Lerp(nightSettings.fogColor, daySettings.fogColor, transitionProgress);
            float fogDensity = Mathf.Lerp(nightSettings.fogDensity, daySettings.fogDensity, transitionProgress);
            float shadowStrength = Mathf.Lerp(nightSettings.shadowStrength, daySettings.shadowStrength, transitionProgress);

            // Apply lighting settings
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;

            // Update light intensities
            sunLight.intensity = daySettings.lightIntensityCurve.Evaluate(timeOfDay);
            moonLight.intensity = nightSettings.lightIntensityCurve.Evaluate(timeOfDay);

            // Update skybox
            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetFloat("_Exposure", isDay ? 1f : 0.5f);
            }

            // Notify other systems of time change
            if (WhisperingWoodsVFX.Instance != null)
            {
                WhisperingWoodsVFX.Instance.SetTimeOfDay(isDay);
            }

            if (WhisperingWoodsAudio.Instance != null)
            {
                WhisperingWoodsAudio.Instance.SetTimeOfDay(isDay);
            }
        }

        private System.Collections.IEnumerator WeatherCycle()
        {
            while (true)
            {
                yield return new WaitForSeconds(weatherChangeDelay);

                // Determine next weather
                WeatherType nextWeather = DetermineNextWeather();
                if (nextWeather != currentWeather)
                {
                    yield return StartCoroutine(TransitionWeather(nextWeather));
                }

                // Set duration for current weather
                currentWeatherDuration = Random.Range(
                    currentWeatherSettings.minDuration,
                    currentWeatherSettings.maxDuration
                );
                weatherTimer = 0f;
            }
        }

        private WeatherType DetermineNextWeather()
        {
            // Clear weather is always possible
            if (Random.value > 0.5f)
                return WeatherType.Clear;

            // Choose random weather based on probability
            float totalProbability = 0f;
            foreach (var weather in weatherSettings)
            {
                if (weather.type != WeatherType.Clear)
                    totalProbability += weather.probability;
            }

            float random = Random.value * totalProbability;
            float currentProb = 0f;

            foreach (var weather in weatherSettings)
            {
                if (weather.type != WeatherType.Clear)
                {
                    currentProb += weather.probability;
                    if (random <= currentProb)
                        return weather.type;
                }
            }

            return WeatherType.Clear;
        }

        private System.Collections.IEnumerator TransitionWeather(WeatherType newWeather)
        {
            WeatherSettings oldSettings = currentWeatherSettings;
            WeatherSettings newSettings = GetWeatherSettings(newWeather);

            if (newSettings == null)
                yield break;

            float elapsed = 0f;
            float duration = newSettings.transitionDuration;

            // Start new weather sound
            if (newSettings.weatherSounds != null && newSettings.weatherSounds.Length > 0)
            {
                AudioClip newSound = newSettings.weatherSounds[Random.Range(0, newSettings.weatherSounds.Length)];
                weatherAudioSource.clip = newSound;
                weatherAudioSource.Play();
            }

            // Transition weather effects
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Interpolate weather parameters
                if (oldSettings != null)
                {
                    RenderSettings.fogColor = Color.Lerp(oldSettings.fogColor, newSettings.fogColor, t);
                    RenderSettings.fogDensity = Mathf.Lerp(oldSettings.fogDensity, newSettings.fogDensity, t);
                    UpdateWindEffect(Mathf.Lerp(oldSettings.windIntensity, newSettings.windIntensity, t));
                }

                yield return null;
            }

            currentWeather = newWeather;
            currentWeatherSettings = newSettings;

            // Update VFX
            if (WhisperingWoodsVFX.Instance != null)
            {
                WhisperingWoodsVFX.Instance.SetWeatherEffect(currentWeather);
            }
        }

        private WeatherSettings GetWeatherSettings(WeatherType type)
        {
            foreach (var settings in weatherSettings)
            {
                if (settings.type == type)
                    return settings;
            }
            return null;
        }

        private void UpdateWindEffect(float intensity)
        {
            // Update grass sway
            if (grassMaterial != null)
            {
                grassMaterial.SetFloat("_WindStrength", intensity * grassSwayAmount);
            }

            // Update tree sway
            foreach (var material in treeMaterials)
            {
                if (material != null)
                {
                    material.SetFloat("_WindStrength", intensity * treeSwayAmount);
                }
            }
        }

        public void SetTime(float hour)
        {
            timeOfDay = Mathf.Clamp01(hour / 24f);
            UpdateTimeOfDay();
        }

        public void SetWeather(WeatherType type)
        {
            StopAllCoroutines();
            StartCoroutine(TransitionWeather(type));
        }

        private void OnDestroy()
        {
            // Cleanup
            if (weatherAudioSource != null)
            {
                weatherAudioSource.Stop();
            }
        }
    }
} 