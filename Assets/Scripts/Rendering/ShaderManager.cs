using UnityEngine;
using System.Collections.Generic;

namespace Forever.Rendering
{
    public class ShaderManager : MonoBehaviour
    {
        public static ShaderManager Instance { get; private set; }

        [System.Serializable]
        public class ShaderProperties
        {
            public string propertyName;
            public ShaderPropertyType propertyType;
            public float floatValue;
            public Color colorValue;
            public Vector4 vectorValue;
            public Texture textureValue;
        }

        [System.Serializable]
        public class MaterialPreset
        {
            public string presetName;
            public Material material;
            public List<ShaderProperties> properties;
        }

        public enum ShaderPropertyType
        {
            Float,
            Color,
            Vector,
            Texture
        }

        [Header("Material Presets")]
        public List<MaterialPreset> materialPresets;

        [Header("Global Properties")]
        public float windStrength = 1f;
        public float windSpeed = 1f;
        public Vector4 windDirection = new Vector4(1f, 0f, 0f, 0f);
        public float timeScale = 1f;

        private Dictionary<string, MaterialPreset> presetDictionary;
        private Dictionary<Material, List<ShaderProperties>> materialProperties;
        private List<Material> dynamicMaterials;

        private int windStrengthID;
        private int windSpeedID;
        private int windDirectionID;
        private int timeID;
        private int noiseTextureID;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeShaderSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeShaderSystem()
        {
            // Initialize collections
            presetDictionary = new Dictionary<string, MaterialPreset>();
            materialProperties = new Dictionary<Material, List<ShaderProperties>>();
            dynamicMaterials = new List<Material>();

            // Cache shader property IDs
            windStrengthID = Shader.PropertyToID("_WindStrength");
            windSpeedID = Shader.PropertyToID("_WindSpeed");
            windDirectionID = Shader.PropertyToID("_WindDirection");
            timeID = Shader.PropertyToID("_Time");
            noiseTextureID = Shader.PropertyToID("_NoiseTexture");

            // Register material presets
            foreach (var preset in materialPresets)
            {
                RegisterMaterialPreset(preset);
            }

            // Initialize noise texture
            GenerateNoiseTexture();
        }

        private void Update()
        {
            UpdateGlobalShaderProperties();
            UpdateDynamicMaterials();
        }

        private void UpdateGlobalShaderProperties()
        {
            // Update global wind properties
            Shader.SetGlobalFloat(windStrengthID, windStrength);
            Shader.SetGlobalFloat(windSpeedID, windSpeed);
            Shader.SetGlobalVector(windDirectionID, windDirection);

            // Update time
            float time = Time.time * timeScale;
            Shader.SetGlobalFloat(timeID, time);
        }

        private void UpdateDynamicMaterials()
        {
            foreach (var material in dynamicMaterials)
            {
                if (materialProperties.TryGetValue(material, out List<ShaderProperties> properties))
                {
                    foreach (var prop in properties)
                    {
                        UpdateMaterialProperty(material, prop);
                    }
                }
            }
        }

        private void RegisterMaterialPreset(MaterialPreset preset)
        {
            if (!presetDictionary.ContainsKey(preset.presetName))
            {
                presetDictionary.Add(preset.presetName, preset);
                if (preset.material != null && !materialProperties.ContainsKey(preset.material))
                {
                    materialProperties.Add(preset.material, preset.properties);
                    dynamicMaterials.Add(preset.material);
                }
            }
        }

        public Material CreateMaterialFromPreset(string presetName)
        {
            if (presetDictionary.TryGetValue(presetName, out MaterialPreset preset))
            {
                Material newMaterial = new Material(preset.material);
                materialProperties.Add(newMaterial, preset.properties);
                dynamicMaterials.Add(newMaterial);
                return newMaterial;
            }
            return null;
        }

        public void UpdateMaterialProperty(Material material, string propertyName, object value)
        {
            if (materialProperties.TryGetValue(material, out List<ShaderProperties> properties))
            {
                var property = properties.Find(p => p.propertyName == propertyName);
                if (property != null)
                {
                    switch (property.propertyType)
                    {
                        case ShaderPropertyType.Float:
                            property.floatValue = (float)value;
                            break;
                        case ShaderPropertyType.Color:
                            property.colorValue = (Color)value;
                            break;
                        case ShaderPropertyType.Vector:
                            property.vectorValue = (Vector4)value;
                            break;
                        case ShaderPropertyType.Texture:
                            property.textureValue = (Texture)value;
                            break;
                    }
                    UpdateMaterialProperty(material, property);
                }
            }
        }

        private void UpdateMaterialProperty(Material material, ShaderProperties property)
        {
            switch (property.propertyType)
            {
                case ShaderPropertyType.Float:
                    material.SetFloat(property.propertyName, property.floatValue);
                    break;
                case ShaderPropertyType.Color:
                    material.SetColor(property.propertyName, property.colorValue);
                    break;
                case ShaderPropertyType.Vector:
                    material.SetVector(property.propertyName, property.vectorValue);
                    break;
                case ShaderPropertyType.Texture:
                    material.SetTexture(property.propertyName, property.textureValue);
                    break;
            }
        }

        private void GenerateNoiseTexture()
        {
            int resolution = 256;
            Texture2D noiseTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            noiseTexture.filterMode = FilterMode.Bilinear;
            noiseTexture.wrapMode = TextureWrapMode.Repeat;

            Color[] pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    pixels[y * resolution + x] = new Color(noise, noise, noise, 1f);
                }
            }

            noiseTexture.SetPixels(pixels);
            noiseTexture.Apply();

            Shader.SetGlobalTexture(noiseTextureID, noiseTexture);
        }

        public void SetWindParameters(float strength, float speed, Vector4 direction)
        {
            windStrength = strength;
            windSpeed = speed;
            windDirection = direction;
        }

        public void SetTimeScale(float scale)
        {
            timeScale = scale;
        }

        private void OnDestroy()
        {
            // Cleanup dynamic materials
            foreach (var material in dynamicMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }
    }
} 