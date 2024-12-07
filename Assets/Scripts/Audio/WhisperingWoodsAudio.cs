using UnityEngine;
using System.Collections.Generic;
using Forever.Core;

namespace Forever.Audio
{
    public class WhisperingWoodsAudio : MonoBehaviour
    {
        public static WhisperingWoodsAudio Instance { get; private set; }

        [Header("Ambient Audio")]
        public AudioClip[] dayAmbience;
        public AudioClip[] nightAmbience;
        public AudioClip[] rainAmbience;
        public AudioClip[] windAmbience;
        
        [Header("Nature Sounds")]
        public AudioClip[] birdSongs;
        public AudioClip[] insectSounds;
        public AudioClip[] leafRustling;
        public AudioClip[] waterSounds;
        
        [Header("Magical Sounds")]
        public AudioClip[] crystalHums;
        public AudioClip[] magicalChimes;
        public AudioClip[] mysticalWhispers;
        
        [Header("Audio Sources")]
        public AudioSource primaryAmbienceSource;
        public AudioSource secondaryAmbienceSource;
        public AudioSource effectsSource;
        public AudioSource magicalSource;
        
        private Dictionary<string, float> soundCooldowns = new Dictionary<string, float>();
        private float minSoundInterval = 1f;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeAudio()
        {
            // Setup audio sources if not assigned
            if (primaryAmbienceSource == null)
            {
                primaryAmbienceSource = gameObject.AddComponent<AudioSource>();
                primaryAmbienceSource.loop = true;
                primaryAmbienceSource.spatialBlend = 0f;
            }
            
            if (secondaryAmbienceSource == null)
            {
                secondaryAmbienceSource = gameObject.AddComponent<AudioSource>();
                secondaryAmbienceSource.loop = true;
                secondaryAmbienceSource.spatialBlend = 0f;
            }
            
            if (effectsSource == null)
            {
                effectsSource = gameObject.AddComponent<AudioSource>();
                effectsSource.spatialBlend = 1f;
            }
            
            if (magicalSource == null)
            {
                magicalSource = gameObject.AddComponent<AudioSource>();
                magicalSource.spatialBlend = 0.5f;
            }
        }
        
        public void SetTimeOfDayAmbience(bool isDay, float blendDuration = 2f)
        {
            AudioClip newClip = isDay ? 
                GetRandomClip(dayAmbience) : 
                GetRandomClip(nightAmbience);
                
            CrossFadeAmbience(newClip, blendDuration);
        }
        
        public void SetWeatherAmbience(WeatherType type, float intensity, float blendDuration = 2f)
        {
            AudioClip weatherClip = null;
            
            switch (type)
            {
                case WeatherType.Rain:
                    weatherClip = GetRandomClip(rainAmbience);
                    break;
                case WeatherType.Wind:
                    weatherClip = GetRandomClip(windAmbience);
                    break;
            }
            
            if (weatherClip != null)
            {
                secondaryAmbienceSource.volume = intensity;
                CrossFadeSecondaryAmbience(weatherClip, blendDuration);
            }
            else
            {
                FadeOutSecondaryAmbience(blendDuration);
            }
        }
        
        private void CrossFadeAmbience(AudioClip newClip, float duration)
        {
            if (primaryAmbienceSource.clip == newClip) return;
            
            // Start fading out current ambience
            StartCoroutine(FadeAudioSource(primaryAmbienceSource, 0f, duration));
            
            // Start new ambience
            primaryAmbienceSource.clip = newClip;
            primaryAmbienceSource.Play();
            StartCoroutine(FadeAudioSource(primaryAmbienceSource, 1f, duration));
        }
        
        private void CrossFadeSecondaryAmbience(AudioClip newClip, float duration)
        {
            if (secondaryAmbienceSource.clip == newClip) return;
            
            StartCoroutine(FadeAudioSource(secondaryAmbienceSource, 0f, duration));
            
            secondaryAmbienceSource.clip = newClip;
            secondaryAmbienceSource.Play();
            StartCoroutine(FadeAudioSource(secondaryAmbienceSource, secondaryAmbienceSource.volume, duration));
        }
        
        private void FadeOutSecondaryAmbience(float duration)
        {
            StartCoroutine(FadeAudioSource(secondaryAmbienceSource, 0f, duration));
        }
        
        private System.Collections.IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }
            
            source.volume = targetVolume;
            if (targetVolume == 0f && source.isPlaying)
            {
                source.Stop();
            }
        }
        
        public void PlayNatureSound(Vector3 position)
        {
            if (Time.time < GetSoundCooldown("nature")) return;
            
            AudioClip clip = null;
            float dayTime = System.DateTime.Now.Hour / 24f;
            
            if (dayTime > 0.6f && dayTime < 0.8f) // Dawn
            {
                clip = GetRandomClip(birdSongs);
            }
            else if (dayTime > 0.2f && dayTime < 0.4f) // Night
            {
                clip = GetRandomClip(insectSounds);
            }
            else
            {
                clip = GetRandomClip(leafRustling);
            }
            
            if (clip != null)
            {
                PlaySoundAtPosition(clip, position);
                SetSoundCooldown("nature", minSoundInterval);
            }
        }
        
        public void PlayMagicalSound(Vector3 position, float intensity = 1f)
        {
            if (Time.time < GetSoundCooldown("magical")) return;
            
            AudioClip clip = GetRandomClip(magicalChimes);
            if (clip != null)
            {
                PlaySoundAtPosition(clip, position, intensity);
                SetSoundCooldown("magical", minSoundInterval);
            }
        }
        
        public void PlayMysticalWhisper(Vector3 position)
        {
            if (Time.time < GetSoundCooldown("whisper")) return;
            
            AudioClip clip = GetRandomClip(mysticalWhispers);
            if (clip != null)
            {
                PlaySoundAtPosition(clip, position, 0.5f);
                SetSoundCooldown("whisper", minSoundInterval * 3f);
            }
        }
        
        private void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            effectsSource.transform.position = position;
            effectsSource.PlayOneShot(clip, volume);
        }
        
        private AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
        
        private float GetSoundCooldown(string soundType)
        {
            float cooldown;
            return soundCooldowns.TryGetValue(soundType, out cooldown) ? cooldown : 0f;
        }
        
        private void SetSoundCooldown(string soundType, float duration)
        {
            soundCooldowns[soundType] = Time.time + duration;
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