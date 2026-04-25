using UnityEngine;

namespace SurvivalGame.Core.Input
{
    [CreateAssetMenu(fileName = "InputSettings", menuName = "SurvivalGame/Input/Input Settings")]
    public class InputSettings : ScriptableObject
    {
        [Header("Movement")]
        public string HorizontalAxis = "Horizontal";
        public string VerticalAxis = "Vertical";
        public string MouseXAxis = "Mouse X";
        public string MouseYAxis = "Mouse Y";

        [Header("Actions")]
        public KeyCode JumpKey = KeyCode.Space;
        public KeyCode SprintKey = KeyCode.LeftShift;
        public KeyCode CrouchKey = KeyCode.LeftControl;
        public KeyCode InteractKey = KeyCode.E;
        public KeyCode AttackKey = KeyCode.Mouse0;
        public KeyCode AimKey = KeyCode.Mouse1;

        [Header("UI")]
        public KeyCode InventoryKey = KeyCode.Tab;
        public KeyCode CraftingKey = KeyCode.C;
        public KeyCode QuestLogKey = KeyCode.L;
        public KeyCode PauseKey = KeyCode.Escape;

        [Header("Building")]
        public KeyCode BuildModeKey = KeyCode.B;
        public KeyCode RotateBuildingKey = KeyCode.R;

        [Header("Quick Slots")]
        public KeyCode[] QuickSlotKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };

        [Header("Mouse Settings")]
        public float MouseSensitivity = 2f;
        public bool InvertYAxis = false;
    }
}
