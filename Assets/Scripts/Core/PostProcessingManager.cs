using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace Forever.Core
{
    public class PostProcessingManager : MonoBehaviour
    {
        public static PostProcessingManager Instance { get; private set; }

        [System.Serializable]
        public class PostProcessProfile
        {
            public string profileName;
            public VolumeProfile volumeProfile;
            public float transitionTime = 1f;
        }

        [Header("Post-Processing Profiles")]
        public List<PostProcessProfile> profiles;
        public PostProcessProfile defaultProfile;

        [Header("Effect Settings")]
        public float defaultBloomIntensity = 1f;
        public float magicalBloomIntensity = 2f;
        public float depthOfFieldFocusDistance = 10f;
        public float motionBlurIntensity = 0.5f;
        public float vignetteIntensity = 0.4f;

        private Volume postProcessVolume;
        private PostProcessProfile currentProfile;
        private Dictionary<string, PostProcessProfile> profileDictionary;

        // Effect components
        private Bloom bloomEffect;
        private DepthOfField depthOfField;
        private MotionBlur motionBlur;
        private ColorAdjustments colorAdjustments;
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePostProcessing();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePostProcessing()
        {
            // Create dictionary for quick profile lookup
            profileDictionary = new Dictionary<string, PostProcessProfile>();
            foreach (var profile in profiles)
            {
                profileDictionary.Add(profile.profileName, profile);
            }

            // Get or create post process volume
            postProcessVolume = gameObject.GetComponent<Volume>();
            if (postProcessVolume == null)
            {
                postProcessVolume = gameObject.AddComponent<Volume>();
                postProcessVolume.isGlobal = true;
            }

            // Set default profile
            if (defaultProfile != null)
            {
                currentProfile = defaultProfile;
                postProcessVolume.profile = defaultProfile.volumeProfile;
            }

            // Get effect components
            if (postProcessVolume.profile.TryGet(out bloomEffect)) { }
            if (postProcessVolume.profile.TryGet(out depthOfField)) { }
            if (postProcessVolume.profile.TryGet(out motionBlur)) { }
            if (postProcessVolume.profile.TryGet(out colorAdjustments)) { }
            if (postProcessVolume.profile.TryGet(out vignette)) { }
            if (postProcessVolume.profile.TryGet(out chromaticAberration)) { }

            // Initialize default settings
            SetDefaultEffectSettings();
        }

        private void SetDefaultEffectSettings()
        {
            if (bloomEffect != null)
            {
                bloomEffect.intensity.value = defaultBloomIntensity;
            }

            if (motionBlur != null)
            {
                motionBlur.intensity.value = motionBlurIntensity;
            }

            if (vignette != null)
            {
                vignette.intensity.value = vignetteIntensity;
            }
        }

        public void TransitionToProfile(string profileName)
        {
            if (profileDictionary.TryGetValue(profileName, out PostProcessProfile newProfile))
            {
                StartCoroutine(TransitionProfile(newProfile));
            }
        }

        private System.Collections.IEnumerator TransitionProfile(PostProcessProfile targetProfile)
        {
            float elapsedTime = 0f;
            VolumeProfile startProfile = postProcessVolume.profile;
            float transitionDuration = targetProfile.transitionTime;

            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / transitionDuration;

                // Interpolate effect values
                InterpolateEffects(startProfile, targetProfile.volumeProfile, t);

                yield return null;
            }

            postProcessVolume.profile = targetProfile.volumeProfile;
            currentProfile = targetProfile;
        }

        private void InterpolateEffects(VolumeProfile from, VolumeProfile to, float t)
        {
            // Interpolate bloom
            Bloom fromBloom, toBloom;
            if (from.TryGet(out fromBloom) && to.TryGet(out toBloom))
            {
                bloomEffect.intensity.value = Mathf.Lerp(fromBloom.intensity.value, toBloom.intensity.value, t);
            }

            // Interpolate depth of field
            DepthOfField fromDof, toDof;
            if (from.TryGet(out fromDof) && to.TryGet(out toDof))
            {
                depthOfField.focusDistance.value = Mathf.Lerp(fromDof.focusDistance.value, toDof.focusDistance.value, t);
            }

            // Interpolate other effects as needed...
        }

        public void SetBloomIntensity(float intensity)
        {
            if (bloomEffect != null)
            {
                bloomEffect.intensity.value = intensity;
            }
        }

        public void SetDepthOfField(float focusDistance)
        {
            if (depthOfField != null)
            {
                depthOfField.focusDistance.value = focusDistance;
            }
        }

        public void SetMotionBlur(float intensity)
        {
            if (motionBlur != null)
            {
                motionBlur.intensity.value = intensity;
            }
        }

        public void SetVignette(float intensity)
        {
            if (vignette != null)
            {
                vignette.intensity.value = intensity;
            }
        }

        public void SetChromaticAberration(float intensity)
        {
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = intensity;
            }
        }

        public void OnMagicalEffect(Vector3 position)
        {
            // Temporarily increase bloom and add chromatic aberration
            StartCoroutine(MagicalEffectSequence());
        }

        private System.Collections.IEnumerator MagicalEffectSequence()
        {
            float originalBloom = bloomEffect.intensity.value;
            float originalAberration = chromaticAberration.intensity.value;

            // Ramp up effects
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                bloomEffect.intensity.value = Mathf.Lerp(originalBloom, magicalBloomIntensity, t);
                chromaticAberration.intensity.value = Mathf.Lerp(originalAberration, 1f, t);

                yield return null;
            }

            // Hold for a moment
            yield return new WaitForSeconds(0.5f);

            // Ramp down effects
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                bloomEffect.intensity.value = Mathf.Lerp(magicalBloomIntensity, originalBloom, t);
                chromaticAberration.intensity.value = Mathf.Lerp(1f, originalAberration, t);

                yield return null;
            }
        }
    }
} 