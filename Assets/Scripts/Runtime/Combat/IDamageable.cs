using UnityEngine;

namespace LittleEmber.Combat
{
    /// <summary>Anything Pip's sword (or future hazards) can hit.</summary>
    public interface IDamageable
    {
        void ApplyDamage(int amount, Vector2 knockback);
    }
}
