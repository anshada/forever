using UnityEngine;

namespace Forever.Characters
{
    public class Inaya : Character
    {
        [Header("Agility Abilities")]
        public float dashForce = 15f;
        public float wallRunDuration = 2f;
        public float doubleJumpForce = 6f;
        private bool canDoubleJump = true;
        private bool isWallRunning = false;

        protected override void Awake()
        {
            base.Awake();
            characterType = CharacterType.Inaya;
            characterName = "Inaya";
        }

        protected override void HandleMovement()
        {
            base.HandleMovement();
            HandleWallRun();
        }

        protected override void HandleAbilities()
        {
            base.HandleAbilities();
            
            // Double jump
            if (Input.GetKeyDown(KeyCode.Space) && !isGrounded && canDoubleJump)
            {
                DoubleJump();
            }
        }

        private void HandleWallRun()
        {
            if (isWallRunning)
            {
                // Apply wall run physics
                rb.useGravity = false;
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            }
            else
            {
                rb.useGravity = true;
            }
        }

        private void DoubleJump()
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * doubleJumpForce, ForceMode.Impulse);
            canDoubleJump = false;
            animator?.SetTrigger("DoubleJump");
        }

        protected override void UseSpecialAbility()
        {
            if (currentCooldown <= 0)
            {
                Dash();
                currentCooldown = specialAbilityCooldown;
            }
        }

        private void Dash()
        {
            Vector3 dashDirection = transform.forward;
            rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);
            animator?.SetTrigger("Dash");
        }

        private void OnCollisionEnter(Collision collision)
        {
            base.OnCollisionEnter(collision);
            
            if (collision.gameObject.CompareTag("Ground"))
            {
                canDoubleJump = true;
                isWallRunning = false;
            }
            else if (collision.gameObject.CompareTag("Wall"))
            {
                isWallRunning = true;
                Invoke(nameof(EndWallRun), wallRunDuration);
            }
        }

        private void EndWallRun()
        {
            isWallRunning = false;
        }
    }
} 