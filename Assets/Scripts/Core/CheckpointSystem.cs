using UnityEngine;
using System.Collections.Generic;
using Forever.Characters;

namespace Forever.Core
{
    public class CheckpointSystem : MonoBehaviour
    {
        public static CheckpointSystem Instance { get; private set; }

        [System.Serializable]
        public class CheckpointData
        {
            public Vector3 position;
            public Quaternion rotation;
            public string checkpointId;
            public bool isActivated;
            public GameObject visualObject;
        }

        [Header("Checkpoint Settings")]
        public List<CheckpointData> checkpoints = new List<CheckpointData>();
        public float activationRadius = 5f;
        public GameObject checkpointActivationEffect;
        public AudioClip checkpointActivationSound;

        [Header("Respawn Settings")]
        public float respawnDelay = 2f;
        public GameObject respawnEffect;
        
        private CheckpointData currentCheckpoint;
        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeCheckpoints();
        }

        private void InitializeCheckpoints()
        {
            foreach (var checkpoint in checkpoints)
            {
                if (checkpoint.visualObject != null)
                {
                    // Add visual indicator for inactive checkpoint
                    var renderer = checkpoint.visualObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.SetColor("_EmissionColor", Color.gray);
                    }
                }
            }

            // Set initial checkpoint
            if (checkpoints.Count > 0)
            {
                SetCheckpoint(checkpoints[0]);
            }
        }

        public void SetCheckpoint(CheckpointData checkpoint)
        {
            if (currentCheckpoint != null && currentCheckpoint.visualObject != null)
            {
                // Dim previous checkpoint
                var prevRenderer = currentCheckpoint.visualObject.GetComponent<Renderer>();
                if (prevRenderer != null)
                {
                    prevRenderer.material.SetColor("_EmissionColor", Color.gray);
                }
            }

            currentCheckpoint = checkpoint;
            checkpoint.isActivated = true;

            // Activate visual effects
            if (checkpoint.visualObject != null)
            {
                var renderer = checkpoint.visualObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.SetColor("_EmissionColor", Color.cyan);
                }
            }

            if (checkpointActivationEffect != null)
            {
                Instantiate(checkpointActivationEffect, checkpoint.position, Quaternion.identity);
            }

            if (checkpointActivationSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(checkpointActivationSound);
            }

            // Save progress
            SaveSystem.Instance.SaveGame();
        }

        public void RespawnPlayer()
        {
            if (currentCheckpoint == null) return;

            StartCoroutine(RespawnSequence());
        }

        private System.Collections.IEnumerator RespawnSequence()
        {
            // Disable player input
            var currentCharacter = GameManager.Instance.currentCharacter;
            if (currentCharacter != null)
            {
                currentCharacter.enabled = false;
            }

            // Fade out effect
            // TODO: Implement screen fade

            yield return new WaitForSeconds(respawnDelay);

            // Respawn at checkpoint
            if (currentCharacter != null)
            {
                if (respawnEffect != null)
                {
                    Instantiate(respawnEffect, currentCheckpoint.position, Quaternion.identity);
                }

                currentCharacter.transform.position = currentCheckpoint.position;
                currentCharacter.transform.rotation = currentCheckpoint.rotation;
                currentCharacter.enabled = true;

                // Reset character state
                var characterHealth = currentCharacter.GetComponent<IHealable>();
                if (characterHealth != null)
                {
                    characterHealth.Heal(100f);
                }
            }

            // Fade in effect
            // TODO: Implement screen fade
        }

        public void SaveCheckpointState()
        {
            if (SaveSystem.Instance != null)
            {
                // Save current checkpoint data
                var saveData = new SaveSystem.GameSaveData();
                // TODO: Implement checkpoint state saving
                SaveSystem.Instance.SaveGame();
            }
        }

        public void LoadCheckpointState()
        {
            // TODO: Implement checkpoint state loading
        }

        private void OnDrawGizmos()
        {
            // Visualize checkpoints in editor
            foreach (var checkpoint in checkpoints)
            {
                Gizmos.color = checkpoint.isActivated ? Color.cyan : Color.gray;
                Gizmos.DrawWireSphere(checkpoint.position, activationRadius);
            }
        }
    }

    // Checkpoint trigger component
    public class CheckpointTrigger : MonoBehaviour
    {
        public string checkpointId;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var checkpoint = CheckpointSystem.Instance.checkpoints.Find(c => c.checkpointId == checkpointId);
                if (checkpoint != null && !checkpoint.isActivated)
                {
                    CheckpointSystem.Instance.SetCheckpoint(checkpoint);
                }
            }
        }
    }
} 