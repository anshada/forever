using UnityEngine;
using System;

namespace Forever.Core
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        // Character switching events
        public event Action<int> OnCharacterSwitch;
        
        // Movement input
        public Vector2 MovementInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool SpecialAbilityPressed { get; private set; }
        
        // UI input
        public bool PausePressed { get; private set; }
        public bool InteractPressed { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Movement
            MovementInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            
            // Actions
            JumpPressed = Input.GetKeyDown(KeyCode.Space);
            SpecialAbilityPressed = Input.GetKeyDown(KeyCode.E);
            InteractPressed = Input.GetKeyDown(KeyCode.F);
            PausePressed = Input.GetKeyDown(KeyCode.Escape);

            // Character switching (1-5 keys)
            for (int i = 0; i < 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    OnCharacterSwitch?.Invoke(i);
                }
            }
        }

        public bool IsMoving()
        {
            return MovementInput.magnitude > 0.1f;
        }
    }
} 