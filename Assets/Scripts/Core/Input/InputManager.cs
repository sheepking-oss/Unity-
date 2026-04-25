using UnityEngine;
using SurvivalGame.Core.Managers;
using SurvivalGame.Core.Events;

namespace SurvivalGame.Core.Input
{
    public class InputManager : ManagerBase
    {
        [Header("Input Settings")]
        [SerializeField] private InputSettings _inputSettings;

        public InputSettings Settings => _inputSettings;

        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public float MouseX { get; private set; }
        public float MouseY { get; private set; }
        public bool IsMoving => Mathf.Abs(Horizontal) > 0.1f || Mathf.Abs(Vertical) > 0.1f;

        public bool JumpPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool AimHeld { get; private set; }

        public bool InventoryPressed { get; private set; }
        public bool CraftingPressed { get; private set; }
        public bool QuestLogPressed { get; private set; }
        public bool PausePressed { get; private set; }

        public bool BuildModePressed { get; private set; }
        public bool RotateBuildingPressed { get; private set; }

        public int QuickSlotIndexPressed { get; private set; } = -1;

        public override void Initialize()
        {
            base.Initialize();
            if (_inputSettings == null)
            {
                Debug.LogError("[InputManager] InputSettings is not assigned!");
            }
        }

        private void Update()
        {
            if (!IsInitialized) return;
            if (_inputSettings == null) return;

            UpdateMovementInput();
            UpdateActionInput();
            UpdateUIInput();
            UpdateBuildingInput();
            UpdateQuickSlotInput();
        }

        private void UpdateMovementInput()
        {
            Horizontal = UnityEngine.Input.GetAxis(_inputSettings.HorizontalAxis);
            Vertical = UnityEngine.Input.GetAxis(_inputSettings.VerticalAxis);
            MouseX = UnityEngine.Input.GetAxis(_inputSettings.MouseXAxis) * _inputSettings.MouseSensitivity;
            MouseY = UnityEngine.Input.GetAxis(_inputSettings.MouseYAxis) * _inputSettings.MouseSensitivity;

            if (_inputSettings.InvertYAxis)
            {
                MouseY = -MouseY;
            }
        }

        private void UpdateActionInput()
        {
            JumpPressed = UnityEngine.Input.GetKeyDown(_inputSettings.JumpKey);
            SprintHeld = UnityEngine.Input.GetKey(_inputSettings.SprintKey);
            CrouchHeld = UnityEngine.Input.GetKey(_inputSettings.CrouchKey);
            InteractPressed = UnityEngine.Input.GetKeyDown(_inputSettings.InteractKey);
            AttackPressed = UnityEngine.Input.GetKeyDown(_inputSettings.AttackKey);
            AimHeld = UnityEngine.Input.GetKey(_inputSettings.AimKey);
        }

        private void UpdateUIInput()
        {
            InventoryPressed = UnityEngine.Input.GetKeyDown(_inputSettings.InventoryKey);
            CraftingPressed = UnityEngine.Input.GetKeyDown(_inputSettings.CraftingKey);
            QuestLogPressed = UnityEngine.Input.GetKeyDown(_inputSettings.QuestLogKey);
            PausePressed = UnityEngine.Input.GetKeyDown(_inputSettings.PauseKey);
        }

        private void UpdateBuildingInput()
        {
            BuildModePressed = UnityEngine.Input.GetKeyDown(_inputSettings.BuildModeKey);
            RotateBuildingPressed = UnityEngine.Input.GetKeyDown(_inputSettings.RotateBuildingKey);
        }

        private void UpdateQuickSlotInput()
        {
            QuickSlotIndexPressed = -1;
            for (int i = 0; i < _inputSettings.QuickSlotKeys.Length; i++)
            {
                if (UnityEngine.Input.GetKeyDown(_inputSettings.QuickSlotKeys[i]))
                {
                    QuickSlotIndexPressed = i;
                    break;
                }
            }
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            if (_inputSettings != null)
            {
                _inputSettings.MouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
            }
        }

        public void SetInvertYAxis(bool invert)
        {
            if (_inputSettings != null)
            {
                _inputSettings.InvertYAxis = invert;
            }
        }
    }
}
