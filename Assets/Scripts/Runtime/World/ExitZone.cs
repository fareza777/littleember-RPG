using UnityEngine;
using LittleEmber.Core;
using LittleEmber.Player;

namespace LittleEmber.World
{
    /// <summary>
    /// Doorway trigger: when Pip steps in, fade to another scene and spawn him
    /// at returnSpawn there. Short cooldown so chained triggers don't loop.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ExitZone : MonoBehaviour
    {
        public string targetScene;
        public Vector2 targetSpawn;

        float cooldown = 0.6f;

        void Update() { if (cooldown > 0f) cooldown -= Time.unscaledDeltaTime; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (cooldown > 0f) return;
            if (other.GetComponentInParent<PipController>() == null) return;
            if (string.IsNullOrEmpty(targetScene)) return;
            cooldown = 999f;
            SceneFlow.PendingSpawn = SceneFlow.ReturnSpawn ?? targetSpawn;
            SceneFlow.ReturnSpawn = null;
            SceneFlow.Load(targetScene);
        }
    }
}
