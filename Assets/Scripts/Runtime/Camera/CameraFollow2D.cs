using UnityEngine;
using LittleEmber.UI;

namespace LittleEmber.CameraWork
{
    /// <summary>
    /// Smooth follow for portrait mode. Camera sits slightly ahead of Pip's facing
    /// so the player sees more of where they are going (important in tall view).
    /// Eases in a subtle zoom while a dialogue is open, plus impulse shakes.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.15f;
        public Vector2 lookAhead = new Vector2(0.8f, 1.2f);
        public bool useBounds;
        public Vector2 minBounds;
        public Vector2 maxBounds;

        [Header("Feel")]
        [Tooltip("Ortho zoom factor while a dialogue box is open (1 = no zoom).")]
        public float dialogueZoom = 0.92f;

        static CameraFollow2D _active;
        static float _shakeAmp, _shakeDur, _shakeT;

        Vector3 velocity;
        Vector2 lastFacing = Vector2.down;
        Camera _cam;
        float _baseOrtho;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null) _baseOrtho = _cam.orthographicSize;
        }

        void OnEnable() { _active = this; }
        void OnDisable() { if (_active == this) _active = null; }

        /// <summary>Impulse camera shake (hit feedback). Keeps the strongest active shake.</summary>
        public static void Shake(float amplitude, float duration)
        {
            if (_active == null) return;
            if (_shakeT < _shakeDur && amplitude < _shakeAmp) return; // a stronger one is running
            _shakeAmp = amplitude;
            _shakeDur = duration;
            _shakeT = 0f;
        }

        void LateUpdate()
        {
            // dialogue zoom — pulls the frame in a touch while talking
            if (_cam != null && _cam.orthographic)
            {
                float targetSize = _baseOrtho * (DialogueUI.IsOpen ? dialogueZoom : 1f);
                _cam.orthographicSize = Mathf.MoveTowards(_cam.orthographicSize, targetSize, Time.deltaTime * 2.2f);
            }

            if (target == null) return;

            var pc = target.GetComponent<LittleEmber.Player.PipController>();
            if (pc != null && pc.Facing.sqrMagnitude > 0.01f) lastFacing = pc.Facing;

            Vector2 focus = (Vector2)target.position + Vector2.Scale(lastFacing, lookAhead);
            Vector3 goal = new Vector3(focus.x, focus.y, transform.position.z);
            Vector3 p = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);

            if (useBounds && _cam != null && _cam.orthographic)
            {
                float halfH = _cam.orthographicSize;
                float halfW = halfH * _cam.aspect;
                p.x = Mathf.Clamp(p.x, minBounds.x + halfW, maxBounds.x - halfW);
                p.y = Mathf.Clamp(p.y, minBounds.y + halfH, maxBounds.y - halfH);
            }

            if (_shakeT < _shakeDur)
            {
                _shakeT += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(_shakeT / _shakeDur);
                p += new Vector3((Mathf.PerlinNoise(_shakeT * 37f, 0.3f) - 0.5f) * 2f * _shakeAmp * k,
                                 (Mathf.PerlinNoise(0.7f, _shakeT * 41f) - 0.5f) * 2f * _shakeAmp * k, 0f);
            }

            transform.position = p;
        }
    }
}
