using UnityEngine;

namespace SurvivalGame.Core.Managers
{
    public abstract class ManagerBase : Singleton<ManagerBase>
    {
        [Header("Manager Settings")]
        public bool InitializeOnAwake = true;
        public bool IsInitialized { get; protected set; }

        public override void Awake()
        {
            base.Awake();
            if (InitializeOnAwake && !IsInitialized)
            {
                Initialize();
            }
        }

        public virtual void Initialize()
        {
            IsInitialized = true;
            Debug.Log($"[ManagerBase] {GetType().Name} initialized.");
        }

        public virtual void Shutdown()
        {
            IsInitialized = false;
            Debug.Log($"[ManagerBase] {GetType().Name} shutdown.");
        }
    }
}
