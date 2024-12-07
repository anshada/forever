using UnityEngine;

namespace Forever.VFX
{
    public class MagicalAura : MonoBehaviour
    {
        [Header("Aura Settings")]
        public float baseIntensity = 1f;
        public float pulseSpeed = 1f;
        public float pulseAmount = 0.2f;
        public float rotationSpeed = 30f;
        
        [Header("Visual Elements")]
        public ParticleSystem auraParticles;
        public Material auraMaterial;
        public Light auraLight;
        
        private float currentIntensity;
        private Color currentColor;
        
        private void Awake()
        {
            if (auraMaterial == null && GetComponent<Renderer>())
            {
                auraMaterial = GetComponent<Renderer>().material;
            }
            
            if (auraLight == null)
            {
                auraLight = GetComponent<Light>();
            }
        }
        
        private void Update()
        {
            // Animate aura
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            float finalIntensity = currentIntensity * (1f + pulse);
            
            // Update material
            if (auraMaterial != null)
            {
                auraMaterial.SetFloat("_Intensity", finalIntensity);
                auraMaterial.SetColor("_EmissionColor", currentColor * finalIntensity);
            }
            
            // Update particles
            if (auraParticles != null)
            {
                var emission = auraParticles.emission;
                emission.rateOverTime = baseIntensity * 10f * finalIntensity;
                
                var main = auraParticles.main;
                main.startColor = currentColor;
            }
            
            // Update light
            if (auraLight != null)
            {
                auraLight.intensity = finalIntensity;
                auraLight.color = currentColor;
            }
            
            // Rotate aura
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        
        public void SetIntensity(float intensity)
        {
            currentIntensity = intensity * baseIntensity;
        }
        
        public void SetColor(Color color)
        {
            currentColor = color;
        }
    }
} 