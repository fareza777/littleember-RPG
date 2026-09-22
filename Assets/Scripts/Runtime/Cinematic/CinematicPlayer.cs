using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LittleEmber.Ads;
using LittleEmber.Audio;
using LittleEmber.Core;

namespace LittleEmber.Cine
{
    /// <summary>The prologue narration. VO lines (prologue_01..07.mp3) speak exactly these words.</summary>
    public static class StoryLines
    {
        public static readonly string[] Prologue =
        {
            "Long ago, the world was warm, and every hearth burned bright with the First Flame.",
            "In the village of Emberholt, the Kindling Festival was one sleep away.",
            "But on that night, something ancient stirred beneath the roots of the world.",
            "The Prince of Dusk has returned, and he is gathering every last light for himself.",
            "One by one, the Everflames went out. The cold crept in, and the dark learned to walk.",
            "Now only one lantern still burns — and it was entrusted to the smallest hands in the village.",
            "Keep it lit, little ember. Carry it far. The dawn is counting on you."
        };
    }

    [System.Serializable]
    public class CinePanel
    {
        public Sprite image;
        public AudioClip vo;
        [TextArea(2, 4)] public string text;
        public float zoomFrom = 1.03f;
        public float zoomTo = 1.12f;
        public Vector2 panFrom;
        public Vector2 panTo;
    }

    /// <summary>
    /// Letterbox cinematic: each slot = dark cover-blurred background + fit-width
    /// foreground plate (square plates stay intact, no ugly crop). Typewriter runs
    /// at a fixed comfortable pace; the panel lasts max(VO, text) + a short hold,
    /// so narration and caption always end together. Tap once = complete line,
    /// tap again = next panel (VO stops). SKIP = skip all.
    /// </summary>
    public class CinematicPlayer : MonoBehaviour
    {
        [Header("Slots (A/B crossfade pair)")]
        public CanvasGroup slotA;
        public CanvasGroup slotB;
        public Image foreA;
        public Image foreB;
        public Image backA;
        public Image backB;

        [Header("Content")]
        public List<CinePanel> panels = new List<CinePanel>();
        public Text caption;
        public Button skipButton;
        public string nextScene = "Emberholt_Village";
        public float charsPerSecond = 42f;

        bool _skipAll;
        bool _endPanel;
        bool _textComplete;

        void Start()
        {
            if (skipButton != null) skipButton.onClick.AddListener(() => _skipAll = true);
            StartCoroutine(Play());
        }

        void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return; // SKIP button
            if (!_textComplete) _textComplete = true;
            else _endPanel = true;
        }

        IEnumerator Play()
        {
            AudioManager.DuckMusic(true); // prologue music sits under the VO
            for (int i = 0; i < panels.Count; i++)
            {
                if (_skipAll) break;
                bool useA = i % 2 == 0;
                yield return ShowPanel(
                    panels[i],
                    useA ? slotA : slotB, useA ? foreA : foreB, useA ? backA : backB,
                    useA ? slotB : slotA);
            }

            AudioManager.StopVO();
            AudioManager.DuckMusic(false);
            AdsManager.TryShowInterstitial();
            SceneFlow.Load(nextScene);
        }

        IEnumerator ShowPanel(CinePanel p, CanvasGroup slot, Image fore, Image back, CanvasGroup outSlot)
        {
            // --- letterbox layout ---
            fore.sprite = p.image;
            back.sprite = p.image;
            slot.transform.SetSiblingIndex(1); // topmost slot, still below scrim/caption/skip
            var tex = p.image != null ? p.image.texture : null;
            if (tex != null)
            {
                // background: cover the whole reference screen, dimmed
                float cover = Mathf.Max(1080f / tex.width, 1920f / tex.height);
                back.rectTransform.sizeDelta = new Vector2(tex.width * cover, tex.height * cover);
                back.rectTransform.localScale = Vector3.one * 1.06f;
                // foreground: fit width, sit in the upper-middle band
                float fit = 1020f / tex.width;
                fore.rectTransform.sizeDelta = new Vector2(tex.width * fit, tex.height * fit);
            }

            // --- timing: VO and text end together ---
            string full = p.text ?? "";
            float voLen = p.vo != null ? p.vo.length : 0f;
            float textTime = full.Length / charsPerSecond;
            float len = Mathf.Max(voLen, textTime) + 0.7f;

            // crossfade in
            float t = 0f;
            slot.alpha = 0f;
            while (t < 0.85f && !_skipAll)
            {
                t += Time.unscaledDeltaTime;
                slot.alpha = Mathf.Clamp01(t / 0.85f);
                if (outSlot != null) outSlot.alpha = 1f - slot.alpha;
                yield return null;
            }
            slot.alpha = 1f;
            if (outSlot != null) outSlot.alpha = 0f;

            // VO + typewriter + ken burns
            AudioManager.PlayVO(p.vo);
            _endPanel = false;
            _textComplete = false;
            if (caption != null) caption.text = "";

            var rt = fore.rectTransform;
            Vector2 basePos = rt.anchoredPosition;
            int chars = 0;

            t = 0f;
            while (t < len && !_skipAll && !_endPanel)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / len);
                float kText = textTime > 0f ? Mathf.Clamp01(t / textTime) : 1f;

                float z = Mathf.Lerp(p.zoomFrom, p.zoomTo, k);
                rt.localScale = new Vector3(z, z, 1f);
                rt.anchoredPosition = basePos + Vector2.Lerp(p.panFrom, p.panTo, k);

                int target = _textComplete ? full.Length : Mathf.Min(full.Length, Mathf.RoundToInt(kText * full.Length));
                if (target != chars && caption != null)
                {
                    chars = target;
                    caption.text = full.Substring(0, chars);
                }
                yield return null;
            }

            if (caption != null) caption.text = full;

            // beat before the next plate
            float hold = 0f;
            while (hold < 0.6f && !_skipAll && !_endPanel)
            {
                hold += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_endPanel) AudioManager.StopVO();
        }
    }
}
