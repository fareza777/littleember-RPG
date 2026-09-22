using System.Collections;
using UnityEngine;
using LittleEmber.Core;

namespace LittleEmber.UI
{
    /// <summary>
    /// Studio splash: logo + title fade in with a soft ember pulse, then MainMenu.
    /// Tap skips the hold (fade-in always plays through once).
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        public CanvasGroup logoGroup;
        public RectTransform logoImage;
        public RectTransform glow;
        public float holdSeconds = 2.6f;
        public string nextScene = "MainMenu";

        void Start()
        {
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            if (logoGroup != null)
            {
                logoGroup.alpha = 0f;
                float t = 0f;
                while (t < 1.0f)
                {
                    t += Time.unscaledDeltaTime;
                    logoGroup.alpha = Mathf.Clamp01(t / 1.0f);
                    yield return null;
                }
                logoGroup.alpha = 1f;
            }

            float hold = 0f;
            while (hold < holdSeconds)
            {
                hold += Time.unscaledDeltaTime;
                if (logoImage != null)
                {
                    float s = 1f + Mathf.Sin(Time.unscaledTime * 2.1f) * 0.022f;
                    logoImage.localScale = new Vector3(s, s, 1f);
                }
                if (glow != null)
                {
                    float g = 1f + Mathf.Sin(Time.unscaledTime * 1.4f) * 0.06f;
                    glow.localScale = new Vector3(g, g, 1f);
                }
                if (Input.GetMouseButtonDown(0)) break;
                yield return null;
            }

            SceneFlow.Load(nextScene);
        }
    }
}
