using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using Forever.Core;

namespace Forever.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [System.Serializable]
        public class SoundGroup
        {
            public string groupName;
            public AudioMixerGroup mixerGroup;
            public List<Sound> sounds;
        }

        [System.Serializable]
        public class Sound
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)]
            public float volume = 1f;
            [Range(0.1f, 3f)]
            public float pitch = 1f;
            public bool loop = false;
            public float spatialBlend = 0f; // 0 = 2D, 1 = 3D
            public float minDistance = 1f;
            public float maxDistance = 50f;
            
            [HideInInspector]
            public AudioSource source;
        }

        [Header("Audio Configuration")]
        public AudioMixer mainMixer;
        public List<SoundGroup> soundGroups;
        
        [Header("Music")]
        public Sound[] backgroundMusic;
        public float musicCrossfadeTime = 2f;
        public float musicLayerBlendTime = 1f;

        [Header("Ambient")]
        public Sound[] ambientSounds;
        public float ambientMinInterval = 5f;
        public float ambientMaxInterval = 15f;

        private Sound currentMusic;
        private Sound nextMusic;
        private Dictionary<string, Sound> soundDictionary;
        private List<AudioSource> activeAmbientSources;

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
            soundDictionary = new Dictionary<string, Sound>();
            activeAmbientSources = new List<AudioSource>();

            // Initialize all sound groups
            foreach (var group in soundGroups)
            {
                foreach (var sound in group.sounds)
                {
                    CreateAudioSource(sound, group.mixerGroup);
                    soundDictionary.Add(sound.name, sound);
                }
            }

            // Initialize music
            foreach (var music in backgroundMusic)
            {
                CreateAudioSource(music, GetMixerGroup("Music"));
                soundDictionary.Add(music.name, music);
            }

            // Initialize ambient sounds
            foreach (var ambient in ambientSounds)
            {
                CreateAudioSource(ambient, GetMixerGroup("Ambient"));
                soundDictionary.Add(ambient.name, ambient);
            }

            // Load saved volume settings
            LoadAudioSettings();
        }

        private void CreateAudioSource(Sound sound, AudioMixerGroup mixerGroup)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;
            sound.source.spatialBlend = sound.spatialBlend;
            sound.source.minDistance = sound.minDistance;
            sound.source.maxDistance = sound.maxDistance;
            sound.source.outputAudioMixerGroup = mixerGroup;
            sound.source.playOnAwake = false;
        }

        public void PlaySound(string name)
        {
            if (soundDictionary.TryGetValue(name, out Sound sound))
            {
                sound.source.Play();
            }
        }

        public void PlaySoundAtPosition(string name, Vector3 position)
        {
            if (soundDictionary.TryGetValue(name, out Sound sound))
            {
                sound.source.transform.position = position;
                sound.source.Play();
            }
        }

        public void StopSound(string name)
        {
            if (soundDictionary.TryGetValue(name, out Sound sound))
            {
                sound.source.Stop();
            }
        }

        public void PlayMusic(string name)
        {
            if (currentMusic != null && currentMusic.source.isPlaying)
            {
                StartCoroutine(CrossfadeMusic(name));
            }
            else
            {
                PlayMusicImmediate(name);
            }
        }

        private void PlayMusicImmediate(string name)
        {
            if (soundDictionary.TryGetValue(name, out Sound music))
            {
                if (currentMusic != null)
                {
                    currentMusic.source.Stop();
                }
                currentMusic = music;
                currentMusic.source.Play();
            }
        }

        private System.Collections.IEnumerator CrossfadeMusic(string nextMusicName)
        {
            if (!soundDictionary.TryGetValue(nextMusicName, out Sound nextMusic))
                yield break;

            float timeElapsed = 0;
            nextMusic.source.volume = 0;
            nextMusic.source.Play();

            while (timeElapsed < musicCrossfadeTime)
            {
                timeElapsed += Time.deltaTime;
                float t = timeElapsed / musicCrossfadeTime;

                currentMusic.source.volume = Mathf.Lerp(currentMusic.volume, 0, t);
                nextMusic.source.volume = Mathf.Lerp(0, nextMusic.volume, t);

                yield return null;
            }

            currentMusic.source.Stop();
            currentMusic = nextMusic;
        }

        public void StartAmbientSounds()
        {
            foreach (var ambient in ambientSounds)
            {
                StartCoroutine(PlayRandomAmbientSound(ambient));
            }
        }

        private System.Collections.IEnumerator PlayRandomAmbientSound(Sound ambient)
        {
            while (true)
            {
                float delay = Random.Range(ambientMinInterval, ambientMaxInterval);
                yield return new WaitForSeconds(delay);

                if (ambient.source != null)
                {
                    ambient.source.Play();
                    activeAmbientSources.Add(ambient.source);
                    yield return new WaitForSeconds(ambient.clip.length);
                    activeAmbientSources.Remove(ambient.source);
                }
            }
        }

        public void StopAllAmbientSounds()
        {
            StopAllCoroutines();
            foreach (var source in activeAmbientSources)
            {
                source.Stop();
            }
            activeAmbientSources.Clear();
        }

        public void SetVolume(string parameterName, float normalizedValue)
        {
            float mixerValue = Mathf.Log10(Mathf.Max(normalizedValue, 0.0001f)) * 20f;
            mainMixer.SetFloat(parameterName, mixerValue);
            SaveAudioSettings();
        }

        private AudioMixerGroup GetMixerGroup(string groupName)
        {
            foreach (var group in soundGroups)
            {
                if (group.groupName == groupName)
                    return group.mixerGroup;
            }
            return null;
        }

        private void SaveAudioSettings()
        {
            float masterVolume, musicVolume, sfxVolume;
            mainMixer.GetFloat("MasterVolume", out masterVolume);
            mainMixer.GetFloat("MusicVolume", out musicVolume);
            mainMixer.GetFloat("SFXVolume", out sfxVolume);

            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }

        private void LoadAudioSettings()
        {
            if (PlayerPrefs.HasKey("MasterVolume"))
            {
                mainMixer.SetFloat("MasterVolume", PlayerPrefs.GetFloat("MasterVolume"));
                mainMixer.SetFloat("MusicVolume", PlayerPrefs.GetFloat("MusicVolume"));
                mainMixer.SetFloat("SFXVolume", PlayerPrefs.GetFloat("SFXVolume"));
            }
        }

        private void OnDestroy()
        {
            StopAllAmbientSounds();
            SaveAudioSettings();
        }
    }
} 