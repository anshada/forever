using UnityEngine;
using Forever.VFX;
using Forever.Audio;

namespace Forever.Environment
{
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        [Header("Weather Settings")]
        public WeatherType currentWeather = WeatherType.Clear;
        public float weatherIntensity = 0f;
        public float weatherTransitionSpeed = 1f;
        public float magicalDisturbanceThreshold = 0.7f;
        
        [Header("Time Settings")]
        public float dayLength = 1200f; // 20 minutes per day
        public float startTime = 0.25f; // Start at 6 AM
        
        private float currentTime;
        private float targetWeatherIntensity;
        private WeatherType targetWeather;
        private bool isTransitioning;
        
        public float CurrentWeatherIntensity => weatherIntensity;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeWeather();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeWeather()
        {
            currentTime = startTime;
            targetWeatherIntensity = weatherIntensity;
            targetWeather = currentWeather;
            
            // Initial weather setup
            UpdateWeatherEffects();
        }
        
        private void Update()
        {
            UpdateTimeOfDay();
            UpdateWeather();
        }
        
        private void UpdateTimeOfDay()
        {
            currentTime += Time.deltaTime / dayLength;
            if (currentTime >= 1f)
            {
                currentTime -= 1f;
            }
            
            // Update environment based on time
            bool isDay = currentTime > 0.25f && currentTime < 0.75f;
            WhisperingWoodsVFX.Instance?.SetEnvironmentEffects(isDay);
            WhisperingWoodsAudio.Instance?.SetTimeOfDayAmbience(isDay);
        }
        
        private void UpdateWeather()
        {
            if (isTransitioning)
            {
                weatherIntensity = Mathf.MoveTowards(weatherIntensity, targetWeatherIntensity, 
                    weatherTransitionSpeed * Time.deltaTime);
                    
                if (Mathf.Approximately(weatherIntensity, targetWeatherIntensity))
                {
                    isTransitioning = false;
                    if (weatherIntensity <= 0f)
                    {
                        currentWeather = targetWeather;
                    }
                }
                
                UpdateWeatherEffects();
            }
        }
        
        private void UpdateWeatherEffects()
        {
            // Update visual effects
            WhisperingWoodsVFX.Instance?.SetWeatherEffects(currentWeather, weatherIntensity);
            
            // Update audio
            WhisperingWoodsAudio.Instance?.SetWeatherAmbience(currentWeather, weatherIntensity);
        }
        
        public void SetWeather(WeatherType type, float intensity, float transitionDuration = 1f)
        {
            targetWeather = type;
            targetWeatherIntensity = intensity;
            weatherTransitionSpeed = 1f / transitionDuration;
            isTransitioning = true;
            
            if (intensity > 0f)
            {
                currentWeather = type;
            }
        }
        
        public void OnMagicalDisturbance(Vector3 position, float intensity)
        {
            if (intensity >= magicalDisturbanceThreshold)
            {
                // Create weather anomaly
                WeatherType disturbanceType = Random.value > 0.5f ? WeatherType.Wind : WeatherType.Rain;
                float disturbanceRadius = intensity * 10f;
                
                // Apply localized weather effect
                WhisperingWoodsVFX.Instance?.PlayMagicalEffect(position, intensity);
                WhisperingWoodsAudio.Instance?.PlayMagicalSound(position, intensity);
                
                // Potentially change global weather if disturbance is strong enough
                if (intensity > 0.9f)
                {
                    SetWeather(disturbanceType, intensity, 2f);
                }
            }
        }
        
        public float GetTimeOfDay()
        {
            return currentTime;
        }
        
        public bool IsDay()
        {
            return currentTime > 0.25f && currentTime < 0.75f;
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
    
    public enum WeatherType
    {
        Clear,
        Rain,
        Snow,
        Fog,
        Wind
    }
} 