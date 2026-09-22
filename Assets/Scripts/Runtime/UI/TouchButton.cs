using UnityEngine;
using UnityEngine.EventSystems;

namespace LittleEmber.UI
{
    /// <summary>
    /// uGUI button for touch combat: exposes one-shot presses (ConsumePress)
    /// and hold state (IsHeld). Squishes slightly while held for feedback.
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsHeld { get; private set; }
        bool pressedThisFrame;
        Vector3 baseScale;

        void Awake()
        {
            baseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressedThisFrame = true;
            IsHeld = true;
            transform.localScale = baseScale * 0.9f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
            transform.localScale = baseScale;
        }

        public bool ConsumePress()
        {
            if (!pressedThisFrame) return false;
            pressedThisFrame = false;
            return true;
        }
    }
}
