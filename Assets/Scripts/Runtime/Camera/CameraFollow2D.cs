using UnityEngine;

namespace LittleEmber.CameraWork
{
    /// <summary>
    /// Smooth follow for portrait mode. Camera sits slightly ahead of Pip's facing
    /// so the player sees more of where they are going (important in tall view).
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.15f;
        public Vector2 lookAhead = new Vector2(0.8f, 1.2f);
        public bool useBounds;
        public Vector2 minBounds;
        public Vector2 maxBounds;

        Vector3 velocity;
        Vector2 lastFacing = Vector2.down;

        void LateUpdate()
        {
            if (target == null) return;

            var pc = target.GetComponent<LittleEmber.Player.PipController>();
            if (pc != null && pc.Facing.sqrMagnitude > 0.01f) lastFacing = pc.Facing;

            Vector2 focus = (Vector2)target.position + Vector2.Scale(lastFacing, lookAhead);
            Vector3 goal = new Vector3(focus.x, focus.y, transform.position.z);
            Vector3 p = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);

            if (useBounds)
            {
                var cam = GetComponent<Camera>();
                if (cam != null && cam.orthographic)
                {
                    float halfH = cam.orthographicSize;
                    float halfW = halfH * cam.aspect;
                    p.x = Mathf.Clamp(p.x, minBounds.x + halfW, maxBounds.x - halfW);
                    p.y = Mathf.Clamp(p.y, minBounds.y + halfH, maxBounds.y - halfH);
                }
            }
            transform.position = p;
        }
    }
}
