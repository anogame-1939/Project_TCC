namespace AnoGame.Application.Damage
{
    public interface IDamageable
    {
        void TakeDamage(int damage);
        bool IsInvincible { get; }  // 無敵状態かどうか
        float CurrentHealth { get; } // 現在のHP
    }
}
