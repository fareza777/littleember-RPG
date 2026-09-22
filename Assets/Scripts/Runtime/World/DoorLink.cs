using UnityEngine;
using LittleEmber.Core;

namespace LittleEmber.World
{
    /// <summary>
    /// House door: wires its Interactable to fade into another scene, spawning
    /// Pip at targetSpawn there. All serialized fields (scene-safe).
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class DoorLink : MonoBehaviour
    {
        public string targetScene;
        public Vector2 targetSpawn;
        public Vector2 returnSpawn; // where ExitZone in the target scene brings Pip back to

        void Awake()
        {
            var inter = GetComponent<Interactable>();
            inter.label = "ENTER";
            inter.radius = 1.7f;
            inter.onUse = () =>
            {
                if (string.IsNullOrEmpty(targetScene)) return;
                SceneFlow.PendingSpawn = targetSpawn;
                SceneFlow.ReturnSpawn = returnSpawn;
                SceneFlow.Load(targetScene);
            };
        }
    }
}
