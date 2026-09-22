using UnityEngine;
using UnityEngine.UI;
using LittleEmber.Player;
using LittleEmber.World;

namespace LittleEmber.UI
{
    /// <summary>
    /// The floating context button (TALK / ENTER) above the attack button.
    /// Polls Interactable.Nearest around Pip, shows itself with the right label,
    /// and triggers the interactable on press. Hidden during dialogue.
    /// </summary>
    public class ContextActionButton : MonoBehaviour
    {
        public PipController pip;
        public CanvasGroup group;
        public Text label;
        public TouchButton button;

        Interactable current;
        float pollTimer;

        void Update()
        {
            pollTimer -= Time.unscaledDeltaTime;
            if (pollTimer <= 0f)
            {
                pollTimer = 0.12f;
                current = (!DialogueUI.IsOpen && pip != null) ? Interactable.Nearest(pip.transform.position) : null;
            }

            bool show = current != null && !DialogueUI.IsOpen;
            if (group != null)
            {
                group.alpha = show ? 1f : 0f;
                group.interactable = show;
                group.blocksRaycasts = show;
            }
            if (show && label != null && label.text != current.label)
                label.text = current.label;

            if (show && button != null && button.ConsumePress())
            {
                try { current.Use(); }
                catch (System.Exception e)
                {
                    // interaction must never soft-lock the player
                    Debug.LogError($"[Interact] {current.label} on {current.name} failed: {e}");
                    if (pip != null) pip.controlsLocked = false;
                }
            }
        }
    }
}
