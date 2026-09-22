using System;
using UnityEngine;

namespace LittleEmber.Combat
{
    /// <summary>
    /// Heart-based health. 1 point = 1 heart (design doc: start 3, max 11).
    /// Code-side events only; UI polls <see cref="current"/> for hearts.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Min(1)] public int maxHearts = 3;
        public int current { get; private set; }
        public bool invulnerable;
        public bool IsDead => current <= 0;

        public event Action<int> Changed;          // new value
        public event Action<int, Vector2> Damaged; // amount, knockback
        public event Action Died;

        void Awake()
        {
            if (current <= 0) current = maxHearts;
        }

        public void SetToMax()
        {
            current = maxHearts;
            Changed?.Invoke(current);
        }

        /// <summary>Restore-from-save setter (clamped to 1..max, fires Changed).</summary>
        public void SetCurrent(int value)
        {
            current = Mathf.Clamp(value, 1, maxHearts);
            Changed?.Invoke(current);
        }

        public bool Damage(int amount, Vector2 knockback = default)
        {
            if (IsDead || invulnerable || amount <= 0) return false;
            current = Mathf.Max(0, current - amount);
            Damaged?.Invoke(amount, knockback);
            Changed?.Invoke(current);
            if (current <= 0) Died?.Invoke();
            return true;
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            current = Mathf.Min(maxHearts, current + amount);
            Changed?.Invoke(current);
        }
    }
}
