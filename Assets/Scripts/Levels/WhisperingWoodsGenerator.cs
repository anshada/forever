using UnityEngine;
using System.Collections.Generic;
using Forever.Interactables;
using Forever.Puzzles;

namespace Forever.Levels
{
    public class WhisperingWoodsGenerator : MonoBehaviour
    {
        [System.Serializable]
        public class TerrainSettings
        {
            public int width = 256;
            public int length = 256;
            public float scale = 20f;
            public float heightMultiplier = 10f;
            public int octaves = 4;
            public float persistence = 0.5f;
            public float lacunarity = 2f;
        }

        [System.Serializable]
        public class VegetationSettings
        {
            public GameObject[] trees;
            public GameObject[] flowers;
            public GameObject[] rocks;
            public float treeDensity = 0.3f;
            public float flowerDensity = 0.5f;
            public float rockDensity = 0.2f;
            public float minScale = 0.8f;
            public float maxScale = 1.2f;
        }

        [Header("Level Generation")]
        public TerrainSettings terrainSettings;
        public VegetationSettings vegetationSettings;
        public Material terrainMaterial;
        
        [Header("Interactive Elements")]
        public GameObject magicalPlantPrefab;
        public GameObject lightCrystalPrefab;
        public int magicalPlantCount = 10;
        public int lightCrystalCount = 5;

        [Header("Lighting")]
        public Light mainLight;
        public Light[] ambientLights;
        public Material skyboxMaterial;
        public Color fogColor;
        public float fogDensity = 0.01f;

        private Terrain terrain;
        private List<GameObject> spawnedObjects = new List<GameObject>();

        private void Awake()
        {
            GenerateLevel();
        }

        private void GenerateLevel()
        {
            CreateTerrain();
            AddVegetation();
            SetupLighting();
            AddInteractiveElements();
        }

        private void CreateTerrain()
        {
            // Create terrain data
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = terrainSettings.width + 1;
            terrainData.size = new Vector3(terrainSettings.width, terrainSettings.heightMultiplier, terrainSettings.length);

            // Generate heightmap
            float[,] heights = GenerateHeights();
            terrainData.SetHeights(0, 0, heights);

            // Create terrain game object
            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrain = terrainObject.GetComponent<Terrain>();
            terrain.materialTemplate = terrainMaterial;

            // Add terrain collider
            terrainObject.AddComponent<TerrainCollider>().terrainData = terrainData;
        }

        private float[,] GenerateHeights()
        {
            float[,] heights = new float[terrainSettings.width, terrainSettings.length];
            System.Random prng = new System.Random(GetRandomSeed());
            Vector2[] octaveOffsets = new Vector2[terrainSettings.octaves];

            for (int i = 0; i < terrainSettings.octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000);
                float offsetY = prng.Next(-100000, 100000);
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            for (int x = 0; x < terrainSettings.width; x++)
            {
                for (int y = 0; y < terrainSettings.length; y++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < terrainSettings.octaves; i++)
                    {
                        float sampleX = x / terrainSettings.scale * frequency + octaveOffsets[i].x;
                        float sampleY = y / terrainSettings.scale * frequency + octaveOffsets[i].y;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= terrainSettings.persistence;
                        frequency *= terrainSettings.lacunarity;
                    }

                    heights[x, y] = noiseHeight;
                }
            }

            return heights;
        }

        private void AddVegetation()
        {
            for (int x = 0; x < terrainSettings.width; x += 5)
            {
                for (int z = 0; z < terrainSettings.length; z += 5)
                {
                    float height = terrain.SampleHeight(new Vector3(x, 0, z));
                    Vector3 position = new Vector3(x, height, z);

                    // Add trees
                    if (Random.value < vegetationSettings.treeDensity)
                    {
                        SpawnVegetation(vegetationSettings.trees, position);
                    }

                    // Add flowers
                    if (Random.value < vegetationSettings.flowerDensity)
                    {
                        SpawnVegetation(vegetationSettings.flowers, position);
                    }

                    // Add rocks
                    if (Random.value < vegetationSettings.rockDensity)
                    {
                        SpawnVegetation(vegetationSettings.rocks, position);
                    }
                }
            }
        }

        private void SpawnVegetation(GameObject[] prefabs, Vector3 position)
        {
            if (prefabs == null || prefabs.Length == 0) return;

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            GameObject instance = Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0, 360), 0));
            
            float scale = Random.Range(vegetationSettings.minScale, vegetationSettings.maxScale);
            instance.transform.localScale *= scale;
            
            spawnedObjects.Add(instance);
        }

        private void SetupLighting()
        {
            // Set skybox and fog
            RenderSettings.skybox = skyboxMaterial;
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;

            // Setup main directional light
            if (mainLight != null)
            {
                mainLight.intensity = 1.2f;
                mainLight.shadows = LightShadows.Soft;
            }

            // Setup ambient lights
            foreach (var light in ambientLights)
            {
                if (light != null)
                {
                    light.intensity = 0.5f;
                    light.shadows = LightShadows.None;
                }
            }
        }

        private void AddInteractiveElements()
        {
            // Add magical plants
            for (int i = 0; i < magicalPlantCount; i++)
            {
                Vector3 randomPosition = GetRandomTerrainPosition();
                GameObject plant = Instantiate(magicalPlantPrefab, randomPosition, Quaternion.identity);
                spawnedObjects.Add(plant);
            }

            // Add light crystals
            List<LightCrystalPuzzle.CrystalNode> crystalNodes = new List<LightCrystalPuzzle.CrystalNode>();
            for (int i = 0; i < lightCrystalCount; i++)
            {
                Vector3 randomPosition = GetRandomTerrainPosition();
                GameObject crystal = Instantiate(lightCrystalPrefab, randomPosition, Quaternion.identity);
                spawnedObjects.Add(crystal);

                // Setup crystal node
                var node = new LightCrystalPuzzle.CrystalNode
                {
                    crystal = crystal.transform,
                    targetColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f),
                    isActivated = false
                };
                crystalNodes.Add(node);
            }

            // Connect crystal nodes
            var puzzle = FindObjectOfType<LightCrystalPuzzle>();
            if (puzzle != null)
            {
                puzzle.crystalNodes = crystalNodes;
            }
        }

        private Vector3 GetRandomTerrainPosition()
        {
            float x = Random.Range(0, terrainSettings.width);
            float z = Random.Range(0, terrainSettings.length);
            float y = terrain.SampleHeight(new Vector3(x, 0, z));
            return new Vector3(x, y, z);
        }

        private int GetRandomSeed()
        {
            return System.DateTime.Now.Millisecond;
        }

        private void OnDestroy()
        {
            // Cleanup spawned objects
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            spawnedObjects.Clear();
        }
    }
} 