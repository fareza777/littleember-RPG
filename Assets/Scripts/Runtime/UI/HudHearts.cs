using UnityEngine;
using UnityEngine.UI;
using LittleEmber.Combat;

namespace LittleEmber.UI
{
    /// <summary>Heart row HUD. Polls Health each frame (3–11 images, negligible cost).</summary>
    public class HudHearts : MonoBehaviour
    {
        public Health target;
        public Image[] hearts;
        public Sprite fullHeart;
        public Sprite emptyHeart;

        void Update()
        {
            if (target == null || hearts == null) return;
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                bool full = i < target.current;
                bool exists = i < target.maxHearts;
                hearts[i].enabled = exists;
                if (exists) hearts[i].sprite = full ? fullHeart : emptyHeart;
            }
        }
    }
}
