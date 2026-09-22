using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LittleEmber.Core
{
    /// <summary>
    /// Scene transitions with a black fade. Self-bootstraps a persistent
    /// full-screen overlay canvas on first use; nothing needs wiring in scenes.
    /// </summary>
    public static class SceneFlow
    {
        class Runner : MonoBehaviour { }

        static Runner _runner;
        static CanvasGroup _fade;
        const float FadeTime = 0.35f;

        /// <summary>Set before Load() to place Pip at a door/spawn instead of the saved position.</summary>
        public static Vector2? PendingSpawn;

        /// <summary>Set by DoorLink so an interior's ExitZone knows which door to return Pip to.</summary>
        public static Vector2? ReturnSpawn;

        public static void Load(string sceneName)
        {
            EnsureRunner();
            _runner.StartCoroutine(DoLoad(sceneName));
        }

        static void EnsureRunner()
        {
            if (_runner != null) return;

            var go = new GameObject("[SceneFlow]");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();

            var cgo = new GameObject("Fade");
            cgo.transform.SetParent(go.transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var img = cgo.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = true;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _fade = cgo.AddComponent<CanvasGroup>();
            _fade.alpha = 0f;
            _fade.blocksRaycasts = false;
        }

        static IEnumerator DoLoad(string sceneName)
        {
            yield return FadeTo(1f);
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            yield return null; // let the new scene's Start() run once
            yield return FadeTo(0f);
        }

        static IEnumerator FadeTo(float target)
        {
            _fade.blocksRaycasts = true;
            float start = _fade.alpha;
            float t = 0f;
            while (t < FadeTime)
            {
                t += Time.unscaledDeltaTime;
                _fade.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / FadeTime));
                yield return null;
            }
            _fade.alpha = target;
            _fade.blocksRaycasts = target > 0.5f;
        }
    }
}
