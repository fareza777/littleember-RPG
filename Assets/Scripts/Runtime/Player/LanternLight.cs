using UnityEngine;
using UnityEngine.Rendering.Universal;
using LittleEmber.Combat;

namespace LittleEmber.Player
{
    /// <summary>
    /// Pip's lantern: warm flickering URP 2D point light.
    /// Design-doc mechanic: "the flame gutters when Pip is afraid" —
    /// the lantern dims at 1 heart (it was never oil).
    /// </summary>
    public class LanternLight : MonoBehaviour
    {
        public Light2D lantern;
        public Health hpSource;

        [Header("Base values")]
        public float baseIntensity = 1.15f;
        public float baseOuterRadius = 5.5f;

        [Header("Flicker")]
        public float flickerAmount = 0.08f;
        public float flickerSpeed = 9f;

        [Header("Low-HP dim (story mechanic)")]
        [Range(0.2f, 1f)] public float lowHpRadiusMultiplier = 0.65f;
        [Range(0.2f, 1f)] public float lowHpIntensityMultiplier = 0.7f;

        float seed;
        float dimSmoothed = 1f;

        void Awake()
        {
            if (lantern == null) lantern = GetComponentInChildren<Light2D>();
            seed = transform.position.x * 7.13f + transform.position.y * 3.71f;
        }

        void Update()
        {
            if (lantern == null) return;

            float targetDim = (hpSource != null && !hpSource.IsDead && hpSource.current <= 1) ? 1f : 0f;
            // dimSmoothed: 0 = full, 1 = dimmed
            dimSmoothed = Mathf.MoveTowards(dimSmoothed, targetDim, Time.deltaTime * 1.5f);

            float n = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
            float intensityMult = Mathf.Lerp(1f, lowHpIntensityMultiplier, dimSmoothed);
            float radiusMult = Mathf.Lerp(1f, lowHpRadiusMultiplier, dimSmoothed);

            lantern.intensity = baseIntensity * intensityMult * (1f - flickerAmount + n * flickerAmount * 2f);
            lantern.pointLightOuterRadius = baseOuterRadius * radiusMult * (0.97f + 0.06f * n);
        }
    }
}
