using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LittleEmber.World
{
    /// <summary>
    /// Warm Perlin flicker for any Light2D (lamps, bonfires, torches).
    /// Deterministic offset per instance so lights don't pulse in sync.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class FlickerLight2D : MonoBehaviour
    {
        [Range(0f, 1f)] public float amount = 0.16f;
        public float speed = 2.6f;

        Light2D l;
        float baseIntensity;
        float seed;

        void Awake()
        {
            l = GetComponent<Light2D>();
            baseIntensity = l.intensity;
            seed = (transform.position.x * 12.9898f + transform.position.y * 78.233f) % 97f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.unscaledTime * speed);
            l.intensity = baseIntensity * (1f - amount * 0.5f + amount * n);
        }
    }
}
