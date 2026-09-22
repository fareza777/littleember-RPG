using UnityEngine;

namespace LittleEmber.Player
{
    /// <summary>
    /// Swaps Pip's cloak color by exchanging the AnimatorController
    /// (index 0 = base controller, 1..4 = generated override controllers
    /// for color_2..color_5 — the four unlockable cloaks).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HeroColorSwap : MonoBehaviour
    {
        [Tooltip("0 = color_1 base, 1..4 = color_2..color_5 overrides")]
        public RuntimeAnimatorController[] colors;

        public void SetColor(int index)
        {
            if (colors == null || colors.Length == 0) return;
            index = Mathf.Clamp(index, 0, colors.Length - 1);
            if (colors[index] != null)
                GetComponent<Animator>().runtimeAnimatorController = colors[index];
        }
    }
}
