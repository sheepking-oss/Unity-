using System;

namespace SurvivalGame.Core.Interfaces
{
    public interface ISaveable
    {
        string SaveID { get; }
        object SaveState();
        void LoadState(object state);
    }
}
