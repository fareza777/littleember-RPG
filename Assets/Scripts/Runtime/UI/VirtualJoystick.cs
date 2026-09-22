using UnityEngine;
using UnityEngine.EventSystems;

namespace LittleEmber.UI
{
    /// <summary>
    /// Fixed-position virtual joystick for the left thumb (portrait layout).
    /// Attach to the joystick background Image (raycast target); the handle is visual only.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform handle;
        [Tooltip("Max handle travel in reference pixels (1080x1920 canvas).")]
        public float radius = 110f;
        [Range(0f, 0.5f)] public float deadzone = 0.15f;

        public Vector2 Value { get; private set; }

        RectTransform background;

        void Awake()
        {
            background = (RectTransform)transform;
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out Vector2 local);
            Vector2 v = Vector2.ClampMagnitude(local / radius, 1f);
            if (v.magnitude < deadzone) v = Vector2.zero;
            Value = v;
            if (handle != null) handle.anchoredPosition = v * radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
    }
}
