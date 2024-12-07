using UnityEngine;
using Forever.Core;

namespace Forever.Characters
{
    public abstract class Character : MonoBehaviour
    {
        [Header("Character Info")]
        public string characterName;
        public CharacterType characterType;
        
        [Header("Stats")]
        public float maxHealth = 100f;
        public float currentHealth;
        public float moveSpeed = 5f;
        public float jumpForce = 8f;
        
        [Header("Special Ability")]
        public float specialAbilityCooldown = 5f;
        public float currentCooldown;
        
        protected Rigidbody rb;
        protected Animator animator;
        protected bool isGrounded;
        protected bool isActive;
        
        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            currentHealth = maxHealth;
        }
        
        protected virtual void Update()
        {
            if (!isActive) return;
            
            if (currentCooldown > 0)
                currentCooldown -= Time.deltaTime;
                
            HandleMovement();
            HandleAbilities();
        }
        
        protected virtual void HandleMovement()
        {
            Vector2 input = InputManager.Instance.MovementInput;
            Vector3 movement = new Vector3(input.x, 0f, input.y).normalized;
            
            if (movement.magnitude > 0.1f)
            {
                transform.forward = movement;
                rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
                animator?.SetBool("IsMoving", true);
            }
            else
            {
                animator?.SetBool("IsMoving", false);
            }
            
            if (InputManager.Instance.JumpPressed && isGrounded)
            {
                Jump();
            }
        }
        
        protected virtual void HandleAbilities()
        {
            if (InputManager.Instance.SpecialAbilityPressed && currentCooldown <= 0)
            {
                UseSpecialAbility();
            }
        }
        
        protected virtual void Jump()
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            animator?.SetTrigger("Jump");
        }
        
        public virtual void TakeDamage(float damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
            animator?.SetTrigger("Hit");
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        public virtual void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
        
        protected virtual void Die()
        {
            animator?.SetTrigger("Die");
            isActive = false;
            // Additional death logic in derived classes
        }
        
        public virtual void Activate()
        {
            isActive = true;
            gameObject.SetActive(true);
        }
        
        public virtual void Deactivate()
        {
            isActive = false;
            gameObject.SetActive(false);
        }
        
        protected abstract void UseSpecialAbility();
        
        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
                animator?.SetBool("IsGrounded", true);
            }
        }
        
        protected virtual void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = false;
                animator?.SetBool("IsGrounded", false);
            }
        }
    }
    
    public enum CharacterType
    {
        Anshad,  // Ingenuity
        Shibna,  // Empathy
        Inaya,   // Agility
        Ilan,    // Logic
        Iwaan    // Creativity
    }
} 