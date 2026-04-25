namespace SurvivalGame.Core.Interfaces
{
    public interface IDamagable
    {
        void TakeDamage(float damage);
        void Heal(float amount);
        bool IsAlive { get; }
    }
}
