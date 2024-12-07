using UnityEngine;
using System.Collections.Generic;
using Forever.Core;

namespace Forever.Audio
{
    public class WhisperingWoodsAudio : MonoBehaviour
    {
        [System.Serializable]
        public class WeatherSoundSet
        {
            public AudioClip[] windSounds;
            public AudioClip[] leafRustleSounds;
            public AudioClip[] birdSongs;
            public AudioClip[] creatureSounds;
        }

        [Header("Environment Audio")]
        public WeatherSoundSet daySounds;
        public WeatherSoundSet nightSounds;
        public AudioClip[] magicalEffects;
        
        [Header("Interactive Audio")]
        public AudioClip[] crystalHums;
        public AudioClip[] plantGrowthSounds;
        public AudioClip[] magicalPlantEffects;
        
        [Header("Music")]
        public AudioClip mainTheme;
        public AudioClip mysteriousTheme;
        public AudioClip puzzleTheme;

        [Header("Sound Settings")]
        public float minTimeBetweenSounds = 5f;
        public float maxTimeBetweenSounds = 15f;
        public float fadeInDuration = 2f;
        public float fadeOutDuration = 2f;

        private bool isNightTime = false;
        private List<AudioSource> activeSources = new List<AudioSource>();
        private float nextSoundTime;

        private void Start()
        {
            InitializeAudio();
        }

        private void InitializeAudio()
        {
            // Start main theme
            AudioManager.Instance.PlayMusic("WhisperingWoodsTheme");
            
            // Start ambient sounds
            AudioManager.Instance.StartAmbientSounds();
            
            // Schedule first environmental sound
            nextSoundTime = Time.time + Random.Range(minTimeBetweenSounds, maxTimeBetweenSounds);
        }

        private void Update()
        {
            if (Time.time >= nextSoundTime)
            {
                PlayRandomEnvironmentalSound();
                nextSoundTime = Time.time + Random.Range(minTimeBetweenSounds, maxTimeBetweenSounds);
            }
        }

        private void PlayRandomEnvironmentalSound()
        {
            WeatherSoundSet currentSoundSet = isNightTime ? nightSounds : daySounds;
            
            // Randomly select sound type
            int soundType = Random.Range(0, 4);
            AudioClip[] selectedArray = null;
            
            switch (soundType)
            {
                case 0:
                    selectedArray = currentSoundSet.windSounds;
                    break;
                case 1:
                    selectedArray = currentSoundSet.leafRustleSounds;
                    break;
                case 2:
                    selectedArray = currentSoundSet.birdSongs;
                    break;
                case 3:
                    selectedArray = currentSoundSet.creatureSounds;
                    break;
            }

            if (selectedArray != null && selectedArray.Length > 0)
            {
                AudioClip selectedClip = selectedArray[Random.Range(0, selectedArray.Length)];
                PlaySoundWithRandomPosition(selectedClip);
            }
        }

        private void PlaySoundWithRandomPosition(AudioClip clip)
        {
            // Get random position within level bounds
            Vector3 randomPosition = GetRandomPositionInLevel();
            AudioManager.Instance.PlaySoundAtPosition(clip.name, randomPosition);
        }

        private Vector3 GetRandomPositionInLevel()
        {
            // Get level bounds from the WhisperingWoodsGenerator
            var levelGenerator = FindObjectOfType<Levels.WhisperingWoodsGenerator>();
            if (levelGenerator != null)
            {
                float width = levelGenerator.terrainSettings.width;
                float length = levelGenerator.terrainSettings.length;
                
                float x = Random.Range(0, width);
                float z = Random.Range(0, length);
                float y = Terrain.activeTerrain.SampleHeight(new Vector3(x, 0, z));
                
                return new Vector3(x, y + 2f, z); // Add small height offset for better sound positioning
            }
            
            return transform.position;
        }

        public void OnMagicalPlantInteraction(Vector3 position, int interactionType)
        {
            if (magicalPlantEffects.Length > interactionType)
            {
                AudioManager.Instance.PlaySoundAtPosition(magicalPlantEffects[interactionType].name, position);
            }
        }

        public void OnCrystalInteraction(Vector3 position)
        {
            if (crystalHums.Length > 0)
            {
                AudioClip hum = crystalHums[Random.Range(0, crystalHums.Length)];
                AudioManager.Instance.PlaySoundAtPosition(hum.name, position);
            }
        }

        public void OnPlantGrowth(Vector3 position)
        {
            if (plantGrowthSounds.Length > 0)
            {
                AudioClip growth = plantGrowthSounds[Random.Range(0, plantGrowthSounds.Length)];
                AudioManager.Instance.PlaySoundAtPosition(growth.name, position);
            }
        }

        public void OnPuzzleStart()
        {
            AudioManager.Instance.PlayMusic("WhisperingWoodsPuzzle");
        }

        public void OnPuzzleComplete()
        {
            AudioManager.Instance.PlayMusic("WhisperingWoodsTheme");
            
            if (magicalEffects.Length > 0)
            {
                AudioClip effect = magicalEffects[Random.Range(0, magicalEffects.Length)];
                AudioManager.Instance.PlaySound(effect.name);
            }
        }

        public void SetTimeOfDay(bool isNight)
        {
            isNightTime = isNight;
            
            // Crossfade to appropriate theme
            string themeName = isNight ? "WhisperingWoodsNight" : "WhisperingWoodsDay";
            AudioManager.Instance.PlayMusic(themeName);
        }

        public void OnMysteriousArea(bool entering)
        {
            if (entering)
            {
                AudioManager.Instance.PlayMusic("WhisperingWoodsMystery");
            }
            else
            {
                AudioManager.Instance.PlayMusic("WhisperingWoodsTheme");
            }
        }

        private void OnDestroy()
        {
            // Clean up any active audio sources
            foreach (var source in activeSources)
            {
                if (source != null)
                {
                    Destroy(source);
                }
            }
            activeSources.Clear();
        }
    }
} 