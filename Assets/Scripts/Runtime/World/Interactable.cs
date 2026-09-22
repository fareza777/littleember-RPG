using System.Collections.Generic;
using UnityEngine;

namespace LittleEmber.World
{
    /// <summary>
    /// Proximity interactable (NPC talk, house door, signpost). Registers itself
    /// in a static list; ContextActionButton finds the nearest one in range and
    /// surfaces its label. Use() runs the wired action.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public string label = "TALK";
        public float radius = 1.6f;
        public System.Action onUse;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Use() => onUse?.Invoke();

        public static Interactable Nearest(Vector2 pos)
        {
            Interactable best = null;
            float bestD = float.MaxValue;
            foreach (var i in All)
            {
                if (i == null || i.onUse == null) continue;
                float d = Vector2.Distance(pos, i.transform.position);
                if (d <= i.radius && d < bestD) { bestD = d; best = i; }
            }
            return best;
        }
    }
}
