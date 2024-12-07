using UnityEngine;
using System.Collections.Generic;

namespace Forever.Characters
{
    public class Shibna : Character
    {
        [Header("Empathy Abilities")]
        public float healingRadius = 8f;
        public float healingAmount = 25f;
        public float communicationRange = 15f;
        public LayerMask creatureLayer;
        
        private List<IInteractable> communicatingCreatures = new List<IInteractable>();
        private bool isHealingActive = false;

        protected override void Awake()
        {
            base.Awake();
            characterType = CharacterType.Shibna;
            characterName = "Shibna";
        }

        protected override void Update()
        {
            base.Update();
            HandleCommunication();
        }

        private void HandleCommunication()
        {
            // Detect nearby creatures that can be communicated with
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, communicationRange, creatureLayer);
            foreach (var hitCollider in hitColliders)
            {
                IInteractable creature = hitCollider.GetComponent<IInteractable>();
                if (creature != null && !communicatingCreatures.Contains(creature))
                {
                    communicatingCreatures.Add(creature);
                    // Show visual indicator that creature can be communicated with
                    creature.ShowInteractionPrompt();
                }
            }

            // Remove creatures that are out of range
            communicatingCreatures.RemoveAll(creature =>
            {
                if (creature == null) return true;
                
                bool outOfRange = Vector3.Distance(transform.position, ((MonoBehaviour)creature).transform.position) > communicationRange;
                if (outOfRange)
                {
                    creature.HideInteractionPrompt();
                }
                return outOfRange;
            });
        }

        protected override void UseSpecialAbility()
        {
            if (currentCooldown <= 0)
            {
                ActivateHealing();
                currentCooldown = specialAbilityCooldown;
            }
        }

        private void ActivateHealing()
        {
            isHealingActive = true;
            
            // Create healing effect
            // TODO: Add particle system for healing visualization
            
            // Heal all nearby characters and creatures
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, healingRadius, creatureLayer);
            foreach (var hitCollider in hitColliders)
            {
                IHealable healable = hitCollider.GetComponent<IHealable>();
                if (healable != null)
                {
                    healable.Heal(healingAmount);
                }
            }

            animator?.SetTrigger("Heal");
            
            // Deactivate healing after one frame
            isHealingActive = false;
        }

        public void CommunicateWith(IInteractable creature)
        {
            if (!communicatingCreatures.Contains(creature))
                return;

            // Start dialogue or interaction
            creature.Interact();
            animator?.SetTrigger("Communicate");
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize healing radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, healingRadius);
            
            // Visualize communication range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, communicationRange);
        }
    }

    public interface IHealable
    {
        void Heal(float amount);
    }

    public interface IInteractable
    {
        void Interact();
        void ShowInteractionPrompt();
        void HideInteractionPrompt();
    }
} 