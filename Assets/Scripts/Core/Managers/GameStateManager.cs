using UnityEngine;

namespace SurvivalGame.Core.Managers
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Inventory,
        Crafting,
        Building,
        Trading
    }

    public class GameStateManager : ManagerBase
    {
        [Header("Game State Settings")]
        [SerializeField] private GameState _initialState = GameState.MainMenu;

        public GameState CurrentState { get; private set; }
        public GameState PreviousState { get; private set; }

        public delegate void GameStateChangedEventHandler(GameState newState, GameState previousState);
        public event GameStateChangedEventHandler OnStateChanged;

        public override void Initialize()
        {
            base.Initialize();
            ChangeState(_initialState);
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            PreviousState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameStateManager] State changed from {PreviousState} to {CurrentState}");

            OnStateChanged?.Invoke(CurrentState, PreviousState);

            switch (CurrentState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                case GameState.Paused:
                case GameState.Inventory:
                case GameState.Crafting:
                case GameState.Building:
                case GameState.Trading:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                case GameState.MainMenu:
                case GameState.GameOver:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
            else if (CurrentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }

        public void ReturnToPreviousState()
        {
            ChangeState(PreviousState);
        }

        public bool IsPaused()
        {
            return CurrentState == GameState.Paused ||
                   CurrentState == GameState.Inventory ||
                   CurrentState == GameState.Crafting ||
                   CurrentState == GameState.Trading;
        }

        public bool CanPlayerMove()
        {
            return CurrentState == GameState.Playing || CurrentState == GameState.Building;
        }

        public bool IsInUI()
        {
            return CurrentState == GameState.Inventory ||
                   CurrentState == GameState.Crafting ||
                   CurrentState == GameState.Trading ||
                   CurrentState == GameState.Paused;
        }
    }
}
