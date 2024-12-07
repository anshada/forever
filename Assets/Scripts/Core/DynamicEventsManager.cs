using UnityEngine;
using System;
using System.Collections.Generic;
using Forever.Environment;
using Forever.AI;
using Forever.Characters;

namespace Forever.Core
{
    public class DynamicEventsManager : MonoBehaviour
    {
        public static DynamicEventsManager Instance { get; private set; }

        [Header("Event Settings")]
        public float eventCheckInterval = 5f;
        public float minTimeBetweenEvents = 30f;
        public float maxEventsPerArea = 3f;
        
        [Header("Story Events")]
        public StoryEvent[] mainStoryEvents;
        public SideEvent[] sideEvents;
        public EnvironmentalEvent[] environmentalEvents;
        
        [Header("Event Triggers")]
        public float characterProximityTrigger = 10f;
        public float magicIntensityTrigger = 0.7f;
        public float timeOfDayTrigger = 0.5f;
        public float weatherIntensityTrigger = 0.6f;
        
        private Dictionary<string, EventState> eventStates;
        private List<ActiveEvent> activeEvents;
        private float lastEventTime;
        private WeatherSystem weatherSystem;
        private EnvironmentResponseSystem environmentSystem;
        private GameManager gameManager;
        private SaveSystem saveSystem;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSystem()
        {
            eventStates = new Dictionary<string, EventState>();
            activeEvents = new List<ActiveEvent>();
            
            // Get system references
            weatherSystem = FindObjectOfType<WeatherSystem>();
            environmentSystem = FindObjectOfType<EnvironmentResponseSystem>();
            gameManager = FindObjectOfType<GameManager>();
            saveSystem = FindObjectOfType<SaveSystem>();
            
            // Initialize event states
            InitializeEventStates();
            
            // Start event check routine
            InvokeRepeating("CheckForEvents", eventCheckInterval, eventCheckInterval);
        }
        
        private void InitializeEventStates()
        {
            // Initialize main story events
            foreach (var storyEvent in mainStoryEvents)
            {
                eventStates[storyEvent.eventId] = new EventState
                {
                    isCompleted = false,
                    progress = 0f,
                    isActive = false,
                    prerequisites = storyEvent.prerequisites
                };
            }
            
            // Initialize side events
            foreach (var sideEvent in sideEvents)
            {
                eventStates[sideEvent.eventId] = new EventState
                {
                    isCompleted = false,
                    progress = 0f,
                    isActive = false,
                    prerequisites = sideEvent.prerequisites
                };
            }
            
            // Load saved event states
            if (saveSystem != null)
            {
                LoadEventStates();
            }
        }
        
        private void LoadEventStates()
        {
            // TODO: Implement loading event states from save system
        }
        
        private void CheckForEvents()
        {
            if (Time.time - lastEventTime < minTimeBetweenEvents)
                return;
                
            if (activeEvents.Count >= maxEventsPerArea)
                return;
                
            // Check for potential events
            CheckStoryEvents();
            CheckSideEvents();
            CheckEnvironmentalEvents();
            
            // Update active events
            UpdateActiveEvents();
        }
        
        private void CheckStoryEvents()
        {
            foreach (var storyEvent in mainStoryEvents)
            {
                if (CanTriggerEvent(storyEvent))
                {
                    TriggerEvent(storyEvent);
                    break;
                }
            }
        }
        
        private void CheckSideEvents()
        {
            foreach (var sideEvent in sideEvents)
            {
                if (CanTriggerEvent(sideEvent))
                {
                    TriggerEvent(sideEvent);
                }
            }
        }
        
        private void CheckEnvironmentalEvents()
        {
            foreach (var envEvent in environmentalEvents)
            {
                if (CanTriggerEnvironmentalEvent(envEvent))
                {
                    TriggerEnvironmentalEvent(envEvent);
                }
            }
        }
        
        private bool CanTriggerEvent(BaseEvent eventData)
        {
            if (!eventStates.ContainsKey(eventData.eventId))
                return false;
                
            EventState state = eventStates[eventData.eventId];
            
            if (state.isCompleted || state.isActive)
                return false;
                
            // Check prerequisites
            foreach (string prereq in state.prerequisites)
            {
                if (!eventStates.ContainsKey(prereq) || !eventStates[prereq].isCompleted)
                    return false;
            }
            
            return true;
        }
        
        private bool CanTriggerEnvironmentalEvent(EnvironmentalEvent eventData)
        {
            if (weatherSystem == null || environmentSystem == null)
                return false;
                
            // Check weather conditions
            if (eventData.requiresSpecificWeather && 
                weatherSystem.CurrentWeatherIntensity < weatherIntensityTrigger)
                return false;
                
            // Check magical energy in the area
            if (eventData.requiresMagicalEnergy && 
                environmentSystem.GetResponseIntensity(transform) < magicIntensityTrigger)
                return false;
                
            return true;
        }
        
        private void TriggerEvent(BaseEvent eventData)
        {
            ActiveEvent newEvent = new ActiveEvent
            {
                eventData = eventData,
                startTime = Time.time,
                progress = 0f
            };
            
            activeEvents.Add(newEvent);
            eventStates[eventData.eventId].isActive = true;
            lastEventTime = Time.time;
            
            // Spawn event-specific elements
            SpawnEventElements(eventData);
            
            // Notify relevant systems
            OnEventTriggered(eventData);
        }
        
        private void TriggerEnvironmentalEvent(EnvironmentalEvent eventData)
        {
            // Trigger environmental response
            if (environmentSystem != null)
            {
                environmentSystem.TriggerResponse(
                    transform.position,
                    eventData.intensity,
                    eventData.responseType
                );
            }
            
            // Spawn environmental effects
            SpawnEventElements(eventData);
            
            // Notify weather system
            if (weatherSystem != null && eventData.affectsWeather)
            {
                weatherSystem.OnMagicalDisturbance(transform.position, eventData.intensity);
            }
        }
        
        private void SpawnEventElements(BaseEvent eventData)
        {
            // Spawn creatures
            foreach (var creature in eventData.creaturesToSpawn)
            {
                Vector3 spawnPos = GetValidSpawnPosition(creature.spawnRadius);
                if (spawnPos != Vector3.zero)
                {
                    Instantiate(creature.prefab, spawnPos, Quaternion.identity);
                }
            }
            
            // Spawn objects
            foreach (var obj in eventData.objectsToSpawn)
            {
                Vector3 spawnPos = GetValidSpawnPosition(obj.spawnRadius);
                if (spawnPos != Vector3.zero)
                {
                    Instantiate(obj.prefab, spawnPos, Quaternion.identity);
                }
            }
        }
        
        private Vector3 GetValidSpawnPosition(float radius)
        {
            // TODO: Implement proper spawn position validation using NavMesh
            return transform.position + UnityEngine.Random.insideUnitSphere * radius;
        }
        
        private void UpdateActiveEvents()
        {
            List<ActiveEvent> completedEvents = new List<ActiveEvent>();
            
            foreach (var activeEvent in activeEvents)
            {
                // Update event progress
                activeEvent.progress = CalculateEventProgress(activeEvent.eventData);
                
                // Check for completion
                if (activeEvent.progress >= 1f)
                {
                    CompleteEvent(activeEvent.eventData);
                    completedEvents.Add(activeEvent);
                }
                // Check for timeout
                else if (Time.time - activeEvent.startTime > activeEvent.eventData.timeLimit)
                {
                    FailEvent(activeEvent.eventData);
                    completedEvents.Add(activeEvent);
                }
            }
            
            // Remove completed events
            foreach (var completed in completedEvents)
            {
                activeEvents.Remove(completed);
            }
        }
        
        private float CalculateEventProgress(BaseEvent eventData)
        {
            // TODO: Implement proper event progress calculation based on objectives
            return 0f;
        }
        
        private void CompleteEvent(BaseEvent eventData)
        {
            if (!eventStates.ContainsKey(eventData.eventId))
                return;
                
            EventState state = eventStates[eventData.eventId];
            state.isCompleted = true;
            state.isActive = false;
            
            // Grant rewards
            if (gameManager != null)
            {
                // TODO: Implement reward system
            }
            
            OnEventCompleted(eventData);
        }
        
        private void FailEvent(BaseEvent eventData)
        {
            if (!eventStates.ContainsKey(eventData.eventId))
                return;
                
            EventState state = eventStates[eventData.eventId];
            state.isActive = false;
            
            OnEventFailed(eventData);
        }
        
        private void OnEventTriggered(BaseEvent eventData)
        {
            // Notify UI
            UIManager.Instance?.ShowEventNotification(eventData.eventName, eventData.description);
            
            // Play event start sound
            AudioManager.Instance?.PlayEventSound(eventData.startSound);
        }
        
        private void OnEventCompleted(BaseEvent eventData)
        {
            // Notify UI
            UIManager.Instance?.ShowEventCompletion(eventData.eventName, eventData.completionMessage);
            
            // Play completion sound
            AudioManager.Instance?.PlayEventSound(eventData.completionSound);
            
            // Save progress
            if (saveSystem != null)
            {
                saveSystem.SaveEventState(eventData.eventId, true);
            }
        }
        
        private void OnEventFailed(BaseEvent eventData)
        {
            // Notify UI
            UIManager.Instance?.ShowEventFailure(eventData.eventName, eventData.failureMessage);
            
            // Play failure sound
            AudioManager.Instance?.PlayEventSound(eventData.failureSound);
        }
    }
    
    [System.Serializable]
    public class BaseEvent
    {
        public string eventId;
        public string eventName;
        public string description;
        public string completionMessage;
        public string failureMessage;
        public float timeLimit;
        public string[] prerequisites;
        public SpawnableCreature[] creaturesToSpawn;
        public SpawnableObject[] objectsToSpawn;
        public AudioClip startSound;
        public AudioClip completionSound;
        public AudioClip failureSound;
    }
    
    [System.Serializable]
    public class StoryEvent : BaseEvent
    {
        public bool isRequired;
        public string[] unlockedEvents;
        public string[] unlockedAreas;
    }
    
    [System.Serializable]
    public class SideEvent : BaseEvent
    {
        public float rewardValue;
        public bool repeatable;
        public float repeatCooldown;
    }
    
    [System.Serializable]
    public class EnvironmentalEvent : BaseEvent
    {
        public ResponseType responseType;
        public float intensity;
        public bool requiresSpecificWeather;
        public bool requiresMagicalEnergy;
        public bool affectsWeather;
    }
    
    [System.Serializable]
    public class SpawnableCreature
    {
        public GameObject prefab;
        public float spawnRadius;
        public bool despawnOnComplete;
    }
    
    [System.Serializable]
    public class SpawnableObject
    {
        public GameObject prefab;
        public float spawnRadius;
        public bool persistAfterEvent;
    }
    
    public class EventState
    {
        public bool isCompleted;
        public float progress;
        public bool isActive;
        public string[] prerequisites;
    }
    
    public class ActiveEvent
    {
        public BaseEvent eventData;
        public float startTime;
        public float progress;
    }
} 