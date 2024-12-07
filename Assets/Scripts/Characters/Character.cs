using UnityEngine;
using Forever.Core;

namespace Forever.Characters
{
    public abstract class Character : MonoBehaviour
    {
        [Header("Character Info")]
        public string characterName;
        public CharacterType characterType;
        
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float jumpForce = 8f;
        protected Rigidbody rb;
        protected Animator animator;
        protected bool isGrounded;

        [Header("Special Ability")]
        public float specialAbilityCooldown = 5f;
        protected float currentCooldown;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
        }

        protected virtual void Update()
        {
            if (currentCooldown > 0)
                currentCooldown -= Time.deltaTime;

            HandleMovement();
            HandleAbilities();
        }

        protected virtual void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;
            
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
        }

        protected virtual void HandleAbilities()
        {
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                Jump();
            }

            if (Input.GetKeyDown(KeyCode.E) && currentCooldown <= 0)
            {
                UseSpecialAbility();
            }
        }

        protected virtual void Jump()
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        public virtual void Activate()
        {
            gameObject.SetActive(true);
            enabled = true;
        }

        public virtual void Deactivate()
        {
            gameObject.SetActive(false);
            enabled = false;
        }

        protected abstract void UseSpecialAbility();

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
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