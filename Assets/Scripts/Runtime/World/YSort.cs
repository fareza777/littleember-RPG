using UnityEngine;

namespace LittleEmber.World
{
    /// <summary>
    /// Top-down depth sort: sortingOrder follows -y every frame (movers) or once
    /// at Start (statics). Puts characters correctly behind/in front of houses,
    /// trees and props. 16 steps per world unit gives sub-tile resolution.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSort : MonoBehaviour
    {
        public bool isStatic;
        public int offset;

        SpriteRenderer sr;

        void Awake() => sr = GetComponent<SpriteRenderer>();
        void Start() { if (isStatic) Apply(); }

        void Update()
        {
            if (!isStatic) Apply();
        }

        void Apply() => sr.sortingOrder = -(int)(transform.position.y * 16f) + offset;
    }
}
