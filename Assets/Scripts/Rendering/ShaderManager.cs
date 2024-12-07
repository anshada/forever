using UnityEngine;
using System.Collections.Generic;

namespace Forever.Rendering
{
    public class ShaderManager : MonoBehaviour
    {
        public static ShaderManager Instance { get; private set; }

        [Header("Global Shader Properties")]
        public float globalMagicIntensity = 1f;
        public float globalWindStrength = 1f;
        public float globalTimeScale = 1f;
        
        [Header("Interaction Settings")]
        public int maxInteractionPoints = 10;
        public float interactionDecayRate = 0.5f;
        
        private List<InteractionPoint> interactionPoints;
        private int currentInteractionIndex;
        
        // Shader property IDs
        private int magicIntensityID;
        private int windStrengthID;
        private int timeScaleID;
        private int interactionPointsID;
        private int interactionCountID;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeShaderSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeShaderSystem()
        {
            // Initialize interaction points
            interactionPoints = new List<InteractionPoint>();
            currentInteractionIndex = 0;
            
            // Cache shader property IDs
            magicIntensityID = Shader.PropertyToID("_GlobalMagicIntensity");
            windStrengthID = Shader.PropertyToID("_GlobalWindStrength");
            timeScaleID = Shader.PropertyToID("_GlobalTimeScale");
            interactionPointsID = Shader.PropertyToID("_InteractionPoints");
            interactionCountID = Shader.PropertyToID("_InteractionPointCount");
            
            // Set initial global values
            Shader.SetGlobalFloat(magicIntensityID, globalMagicIntensity);
            Shader.SetGlobalFloat(windStrengthID, globalWindStrength);
            Shader.SetGlobalFloat(timeScaleID, globalTimeScale);
            Shader.SetGlobalInt(interactionCountID, 0);
        }
        
        private void Update()
        {
            UpdateInteractionPoints();
            UpdateGlobalShaderProperties();
        }
        
        private void UpdateInteractionPoints()
        {
            // Update decay and remove expired points
            for (int i = interactionPoints.Count - 1; i >= 0; i--)
            {
                InteractionPoint point = interactionPoints[i];
                point.intensity -= interactionDecayRate * Time.deltaTime;
                
                if (point.intensity <= 0)
                {
                    interactionPoints.RemoveAt(i);
                }
            }
            
            // Update shader array
            Vector4[] pointsArray = new Vector4[maxInteractionPoints];
            for (int i = 0; i < interactionPoints.Count; i++)
            {
                InteractionPoint point = interactionPoints[i];
                pointsArray[i] = new Vector4(
                    point.position.x,
                    point.position.y,
                    point.position.z,
                    point.intensity
                );
            }
            
            Shader.SetGlobalVectorArray(interactionPointsID, pointsArray);
            Shader.SetGlobalInt(interactionCountID, interactionPoints.Count);
        }
        
        private void UpdateGlobalShaderProperties()
        {
            Shader.SetGlobalFloat(magicIntensityID, globalMagicIntensity);
            Shader.SetGlobalFloat(windStrengthID, globalWindStrength);
            Shader.SetGlobalFloat(timeScaleID, globalTimeScale);
        }
        
        public void UpdateMagicInteraction(Vector3 position, float intensity)
        {
            // Find existing point or create new one
            InteractionPoint existingPoint = interactionPoints.Find(p => 
                Vector3.Distance(p.position, position) < 0.1f);
                
            if (existingPoint != null)
            {
                existingPoint.intensity = Mathf.Max(existingPoint.intensity, intensity);
            }
            else if (interactionPoints.Count < maxInteractionPoints)
            {
                interactionPoints.Add(new InteractionPoint
                {
                    position = position,
                    intensity = intensity
                });
            }
            else
            {
                // Replace oldest point
                currentInteractionIndex = (currentInteractionIndex + 1) % maxInteractionPoints;
                interactionPoints[currentInteractionIndex] = new InteractionPoint
                {
                    position = position,
                    intensity = intensity
                };
            }
        }
        
        public void SetGlobalMagicIntensity(float intensity)
        {
            globalMagicIntensity = intensity;
        }
        
        public void SetGlobalWindStrength(float strength)
        {
            globalWindStrength = strength;
        }
        
        public void SetGlobalTimeScale(float scale)
        {
            globalTimeScale = scale;
        }
        
        private void OnDestroy()
        {
            // Reset global shader properties
            Shader.SetGlobalFloat(magicIntensityID, 1f);
            Shader.SetGlobalFloat(windStrengthID, 1f);
            Shader.SetGlobalFloat(timeScaleID, 1f);
            Shader.SetGlobalInt(interactionCountID, 0);
        }
    }
    
    public class InteractionPoint
    {
        public Vector3 position;
        public float intensity;
    }
} 