using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Forever.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource musicSource;
        public AudioSource uiSource;
        public AudioSource[] sfxSources;
        
        [Header("Audio Settings")]
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public float uiVolume = 1f;
        public int sfxSourcesCount = 5;
        
        [Header("UI Sounds")]
        public AudioClip buttonClick;
        public AudioClip buttonHover;
        public AudioClip panelOpen;
        public AudioClip panelClose;
        public AudioClip itemSelect;
        public AudioClip questComplete;
        public AudioClip dialogueNext;
        public AudioClip notification;
        public AudioClip error;
        public AudioClip success;
        
        private Dictionary<UISoundType, AudioClip> uiSounds;
        private int currentSfxSource;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeAudio()
        {
            // Initialize audio sources if not assigned
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
            }
            
            if (uiSource == null)
            {
                uiSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Initialize SFX sources
            if (sfxSources == null || sfxSources.Length == 0)
            {
                sfxSources = new AudioSource[sfxSourcesCount];
                for (int i = 0; i < sfxSourcesCount; i++)
                {
                    sfxSources[i] = gameObject.AddComponent<AudioSource>();
                }
            }
            
            // Initialize UI sounds dictionary
            uiSounds = new Dictionary<UISoundType, AudioClip>
            {
                { UISoundType.ButtonClick, buttonClick },
                { UISoundType.ButtonHover, buttonHover },
                { UISoundType.PanelOpen, panelOpen },
                { UISoundType.PanelClose, panelClose },
                { UISoundType.ItemSelect, itemSelect },
                { UISoundType.QuestComplete, questComplete },
                { UISoundType.DialogueNext, dialogueNext },
                { UISoundType.NotificationShow, notification },
                { UISoundType.Error, error },
                { UISoundType.Success, success }
            };
            
            // Apply initial volumes
            UpdateVolumes();
        }
        
        public void PlayUISound(UISoundType soundType)
        {
            if (uiSounds.TryGetValue(soundType, out AudioClip clip) && clip != null)
            {
                uiSource.PlayOneShot(clip, uiVolume * masterVolume);
            }
        }
        
        public void PlaySound(string clipName, float volume = 1f)
        {
            AudioSource source = sfxSources[currentSfxSource];
            currentSfxSource = (currentSfxSource + 1) % sfxSources.Length;
            
            AudioClip clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            if (clip != null)
            {
                source.PlayOneShot(clip, volume * sfxVolume * masterVolume);
            }
        }
        
        public void PlaySoundAtPosition(string clipName, Vector3 position, float volume = 1f)
        {
            AudioSource source = sfxSources[currentSfxSource];
            currentSfxSource = (currentSfxSource + 1) % sfxSources.Length;
            
            source.transform.position = position;
            source.spatialBlend = 1f;
            
            AudioClip clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            if (clip != null)
            {
                source.PlayOneShot(clip, volume * sfxVolume * masterVolume);
            }
        }
        
        public void PlayMusic(string musicName, float fadeTime = 1f)
        {
            AudioClip newMusic = Resources.Load<AudioClip>($"Music/{musicName}");
            if (newMusic != null)
            {
                StartCoroutine(CrossFadeMusic(newMusic, fadeTime));
            }
        }
        
        private IEnumerator CrossFadeMusic(AudioClip newMusic, float fadeTime)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            
            // Fade out current music
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
                yield return null;
            }
            
            // Change music clip
            musicSource.clip = newMusic;
            musicSource.Play();
            
            // Fade in new music
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume * masterVolume, elapsed / fadeTime);
                yield return null;
            }
        }
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
        
        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
        
        private void UpdateVolumes()
        {
            musicSource.volume = musicVolume * masterVolume;
            uiSource.volume = uiVolume * masterVolume;
            
            foreach (var source in sfxSources)
            {
                source.volume = sfxVolume * masterVolume;
            }
        }
        
        public void PlayEventSound(AudioClip clip)
        {
            if (clip != null)
            {
                PlaySound(clip.name);
            }
        }

        public void PlayEventSound(string clipName)
        {
            PlaySound(clipName, 1f);
        }

        public void PlayEventSound(AudioClip clip, float volume)
        {
            if (clip != null)
            {
                PlaySound(clip.name, volume);
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