using UnityEngine;
using System.Collections.Generic;

namespace Forever.Characters
{
    public class Anshad : Character
    {
        [Header("Ingenuity Abilities")]
        public float energyFieldRadius = 5f;
        public float energyFieldDuration = 8f;
        public float inventionRange = 10f;
        public LayerMask interactableLayer;
        
        private List<GameObject> inventedObjects = new List<GameObject>();
        private bool isEnergyFieldActive = false;
        private float currentFieldTime = 0f;

        protected override void Awake()
        {
            base.Awake();
            characterType = CharacterType.Anshad;
            characterName = "Anshad";
        }

        protected override void Update()
        {
            base.Update();
            UpdateEnergyField();
        }

        private void UpdateEnergyField()
        {
            if (isEnergyFieldActive)
            {
                currentFieldTime += Time.deltaTime;
                if (currentFieldTime >= energyFieldDuration)
                {
                    DeactivateEnergyField();
                }
            }
        }

        protected override void UseSpecialAbility()
        {
            if (currentCooldown <= 0)
            {
                ActivateEnergyField();
                currentCooldown = specialAbilityCooldown;
            }
        }

        private void ActivateEnergyField()
        {
            isEnergyFieldActive = true;
            currentFieldTime = 0f;
            
            // Create visual effect for energy field
            // TODO: Add particle system for energy field visualization
            
            // Find and enhance nearby mechanical objects
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, energyFieldRadius, interactableLayer);
            foreach (var hitCollider in hitColliders)
            {
                IEnhanceable enhanceable = hitCollider.GetComponent<IEnhanceable>();
                enhanceable?.Enhance();
            }

            animator?.SetTrigger("ActivateField");
        }

        private void DeactivateEnergyField()
        {
            isEnergyFieldActive = false;
            
            // Deactivate visual effects
            // TODO: Remove particle effects
            
            // Reset enhanced objects
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, energyFieldRadius, interactableLayer);
            foreach (var hitCollider in hitColliders)
            {
                IEnhanceable enhanceable = hitCollider.GetComponent<IEnhanceable>();
                enhanceable?.ResetEnhancement();
            }
        }

        public void CreateInvention(Vector3 position, InventionType type)
        {
            if (Vector3.Distance(transform.position, position) > inventionRange)
                return;

            // TODO: Instantiate invention prefab based on type
            GameObject invention = null;
            switch (type)
            {
                case InventionType.Bridge:
                    // invention = Instantiate(bridgePrefab, position, Quaternion.identity);
                    break;
                case InventionType.Platform:
                    // invention = Instantiate(platformPrefab, position, Quaternion.identity);
                    break;
                case InventionType.PowerSource:
                    // invention = Instantiate(powerSourcePrefab, position, Quaternion.identity);
                    break;
            }

            if (invention != null)
            {
                inventedObjects.Add(invention);
                animator?.SetTrigger("Create");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize energy field radius in editor
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, energyFieldRadius);
            
            // Visualize invention range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, inventionRange);
        }
    }

    public enum InventionType
    {
        Bridge,
        Platform,
        PowerSource
    }

    public interface IEnhanceable
    {
        void Enhance();
        void ResetEnhancement();
    }
} 