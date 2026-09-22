using UnityEngine;
using UnityEngine.UI;
using LittleEmber.Audio;

namespace LittleEmber.UI
{
    /// <summary>
    /// Hooks a soft click onto every Button under a menu root — menus are built
    /// by editor scripts, so a runtime sweep keeps scene YAML untouched.
    /// </summary>
    public static class UiSfx
    {
        public static void HookButtons(Component root, float volume = 0.55f)
        {
            if (root == null) return;
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                b.onClick.AddListener(() => AudioManager.PlaySfxName("ui", volume));
        }

        /// <summary>Every Button in the scene, including inactive sub-panels.</summary>
        public static void HookAll(float volume = 0.55f)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                b.onClick.AddListener(() => AudioManager.PlaySfxName("ui", volume));
        }
    }
}
