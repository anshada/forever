using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;

namespace Forever.AI
{
    public enum AIState
    {
        Idle,
        Wander,
        Follow,
        Flee,
        Interact,
        React,
        Patrol
    }

    public class AIBehaviorSystem : MonoBehaviour
    {
        [Header("AI Settings")]
        public float detectionRadius = 10f;
        public float interactionRadius = 3f;
        public float wanderRadius = 15f;
        public float minWanderTime = 3f;
        public float maxWanderTime = 8f;
        public float patrolWaitTime = 2f;
        
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float rotationSpeed = 120f;
        public float accelerationRate = 2f;
        public float stoppingDistance = 0.5f;
        
        [Header("Behavior Weights")]
        public float curiosityWeight = 1f;
        public float cautionWeight = 1f;
        public float sociabilityWeight = 1f;
        public float playfulnessWeight = 1f;
        
        [Header("References")]
        public Transform[] patrolPoints;
        public LayerMask obstacleLayer;
        public LayerMask interactableLayer;
        
        protected NavMeshAgent agent;
        protected Animator animator;
        protected AIState currentState;
        protected float stateTimer;
        protected int currentPatrolIndex;
        protected Transform currentTarget;
        protected Dictionary<Type, float> characterAffinities;
        
        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            characterAffinities = new Dictionary<Type, float>();
            InitializeAffinities();
            
            // Configure NavMeshAgent
            agent.speed = moveSpeed;
            agent.angularSpeed = rotationSpeed;
            agent.acceleration = accelerationRate;
            agent.stoppingDistance = stoppingDistance;
        }
        
        protected virtual void Start()
        {
            TransitionToState(AIState.Idle);
        }
        
        protected virtual void Update()
        {
            UpdateCurrentState();
            UpdateAnimator();
        }
        
        protected virtual void UpdateCurrentState()
        {
            stateTimer -= Time.deltaTime;
            
            switch (currentState)
            {
                case AIState.Idle:
                    UpdateIdleState();
                    break;
                case AIState.Wander:
                    UpdateWanderState();
                    break;
                case AIState.Follow:
                    UpdateFollowState();
                    break;
                case AIState.Flee:
                    UpdateFleeState();
                    break;
                case AIState.Interact:
                    UpdateInteractState();
                    break;
                case AIState.React:
                    UpdateReactState();
                    break;
                case AIState.Patrol:
                    UpdatePatrolState();
                    break;
            }
        }
        
        protected virtual void UpdateIdleState()
        {
            if (stateTimer <= 0)
            {
                // Randomly choose next state
                float rand = UnityEngine.Random.value;
                if (rand < 0.6f)
                    TransitionToState(AIState.Wander);
                else if (rand < 0.8f && patrolPoints != null && patrolPoints.Length > 0)
                    TransitionToState(AIState.Patrol);
                else
                    SetIdleTimer();
            }
        }
        
        protected virtual void UpdateWanderState()
        {
            if (!agent.pathStatus.Equals(NavMeshPathStatus.PathComplete) || 
                (agent.remainingDistance <= agent.stoppingDistance && stateTimer <= 0))
            {
                SetWanderDestination();
            }
        }
        
        protected virtual void UpdateFollowState()
        {
            if (currentTarget == null)
            {
                TransitionToState(AIState.Idle);
                return;
            }
            
            agent.SetDestination(currentTarget.position);
            
            // Check if target is too far
            if (Vector3.Distance(transform.position, currentTarget.position) > detectionRadius * 1.5f)
            {
                TransitionToState(AIState.Wander);
            }
        }
        
        protected virtual void UpdateFleeState()
        {
            if (currentTarget == null || stateTimer <= 0)
            {
                TransitionToState(AIState.Wander);
                return;
            }
            
            // Move away from threat
            Vector3 fleeDirection = transform.position - currentTarget.position;
            Vector3 fleePosition = transform.position + fleeDirection.normalized * detectionRadius;
            agent.SetDestination(fleePosition);
        }
        
        protected virtual void UpdateInteractState()
        {
            if (currentTarget == null || stateTimer <= 0)
            {
                TransitionToState(AIState.Idle);
                return;
            }
            
            // Face the interaction target
            Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(directionToTarget),
                rotationSpeed * Time.deltaTime
            );
        }
        
        protected virtual void UpdateReactState()
        {
            if (stateTimer <= 0)
            {
                TransitionToState(AIState.Idle);
            }
        }
        
        protected virtual void UpdatePatrolState()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                TransitionToState(AIState.Idle);
                return;
            }
            
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (stateTimer <= 0)
                {
                    // Move to next patrol point
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    stateTimer = patrolWaitTime;
                }
            }
        }
        
        protected virtual void UpdateAnimator()
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", agent.velocity.magnitude / moveSpeed);
                animator.SetInteger("State", (int)currentState);
            }
        }
        
        public virtual void TransitionToState(AIState newState)
        {
            currentState = newState;
            
            switch (newState)
            {
                case AIState.Idle:
                    SetIdleTimer();
                    agent.isStopped = true;
                    break;
                    
                case AIState.Wander:
                    SetWanderDestination();
                    agent.isStopped = false;
                    break;
                    
                case AIState.Follow:
                    agent.isStopped = false;
                    stateTimer = UnityEngine.Random.Range(5f, 10f);
                    break;
                    
                case AIState.Flee:
                    agent.isStopped = false;
                    stateTimer = UnityEngine.Random.Range(3f, 6f);
                    break;
                    
                case AIState.Interact:
                    agent.isStopped = true;
                    stateTimer = UnityEngine.Random.Range(2f, 5f);
                    break;
                    
                case AIState.React:
                    agent.isStopped = true;
                    stateTimer = UnityEngine.Random.Range(1f, 3f);
                    break;
                    
                case AIState.Patrol:
                    agent.isStopped = false;
                    if (patrolPoints != null && patrolPoints.Length > 0)
                    {
                        currentPatrolIndex = 0;
                        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    }
                    break;
            }
        }
        
        protected virtual void SetIdleTimer()
        {
            stateTimer = UnityEngine.Random.Range(2f, 5f);
        }
        
        protected virtual void SetWanderDestination()
        {
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1);
            agent.SetDestination(hit.position);
            stateTimer = UnityEngine.Random.Range(minWanderTime, maxWanderTime);
        }
        
        protected virtual void InitializeAffinities()
        {
            // Set default affinities for each character type
            characterAffinities[typeof(Characters.Inaya)] = 0.8f;
            characterAffinities[typeof(Characters.Anshad)] = 0.6f;
            characterAffinities[typeof(Characters.Shibna)] = 0.9f;
            characterAffinities[typeof(Characters.Ilan)] = 0.7f;
            characterAffinities[typeof(Characters.Iwaan)] = 0.8f;
        }
        
        public virtual void OnCharacterProximity(Character character)
        {
            if (character == null) return;
            
            float affinity = 0f;
            if (characterAffinities.TryGetValue(character.GetType(), out affinity))
            {
                float reactionThreshold = UnityEngine.Random.value;
                if (reactionThreshold < affinity * sociabilityWeight)
                {
                    currentTarget = character.transform;
                    TransitionToState(AIState.Follow);
                }
            }
        }
        
        public virtual void OnThreatDetected(Transform threat)
        {
            float fleeThreshold = UnityEngine.Random.value;
            if (fleeThreshold < cautionWeight)
            {
                currentTarget = threat;
                TransitionToState(AIState.Flee);
            }
        }
        
        public virtual void OnInteractionAvailable(Transform interactable)
        {
            float interactThreshold = UnityEngine.Random.value;
            if (interactThreshold < curiosityWeight)
            {
                currentTarget = interactable;
                TransitionToState(AIState.Interact);
            }
        }
        
        protected virtual void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
            // Draw interaction radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
            
            // Draw wander radius
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, wanderRadius);
        }
    }
} 