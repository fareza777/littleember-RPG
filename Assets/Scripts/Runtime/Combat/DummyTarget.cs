using System.Collections;
using UnityEngine;

namespace LittleEmber.Combat
{
    /// <summary>
    /// Training dummy: unkillable, reacts to hits with a white flash + squash.
    /// Juice spec (design doc): 1-frame-ish white flash, small scale punch.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DummyTarget : MonoBehaviour, IDamageable
    {
        public float flashTime = 0.07f;
        public float punchScale = 0.85f;
        public int hits { get; private set; }

        SpriteRenderer sr;
        Vector3 baseScale;
        Color baseColor;
        Coroutine routine;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            baseColor = sr.color;
        }

        public void ApplyDamage(int amount, Vector2 knockback)
        {
            hits++;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Punch());
        }

        IEnumerator Punch()
        {
            sr.color = new Color(4f, 4f, 4f, 1f); // overbright = white flash on sprite material
            transform.localScale = baseScale * punchScale;
            yield return new WaitForSeconds(flashTime);
            sr.color = baseColor;
            transform.localScale = baseScale;
            routine = null;
        }
    }
}
