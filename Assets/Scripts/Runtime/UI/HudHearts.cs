using UnityEngine;
using UnityEngine.UI;
using LittleEmber.Combat;

namespace LittleEmber.UI
{
    /// <summary>
    /// Heart row HUD. Polls Health each frame (3–11 images, negligible cost).
    /// Hearts pulse briefly when their fill state changes (damage / heal feedback)
    /// and the row shivers when the last heart is hit.
    /// </summary>
    public class HudHearts : MonoBehaviour
    {
        public Health target;
        public Image[] hearts;
        public Sprite fullHeart;
        public Sprite emptyHeart;

        RectTransform[] _rts;
        float[] _pulseT;
        bool[] _lastFull;
        float _lowBeat;
        int _lastHp = -1;
        Image _flash;
        float _flashT;
        static Sprite _vignetteSprite;

        void Awake()
        {
            if (hearts == null) return;
            _rts = new RectTransform[hearts.Length];
            _pulseT = new float[hearts.Length];
            _lastFull = new bool[hearts.Length];
            for (int i = 0; i < hearts.Length; i++)
            {
                _rts[i] = hearts[i] != null ? (RectTransform)hearts[i].transform : null;
                _lastFull[i] = true;
            }
        }

        void Update()
        {
            if (target == null || hearts == null) return;
            bool low = target.current == 1 && !target.IsDead;
            _lowBeat += Time.unscaledDeltaTime;

            // took damage? pulse a soft red vignette around the screen edges
            if (_lastHp >= 0 && target.current < _lastHp) TriggerFlash();
            _lastHp = target.current;
            if (_flashT > 0f)
            {
                _flashT -= Time.unscaledDeltaTime;
                EnsureFlash();
                if (_flash != null)
                {
                    var c = _flash.color;
                    c.a = Mathf.Clamp01(_flashT / 0.35f) * 0.45f;
                    _flash.color = c;
                    if (_flashT <= 0f) _flash.enabled = false;
                }
            }

            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                bool full = i < target.current;
                bool exists = i < target.maxHearts;
                hearts[i].enabled = exists;
                if (exists) hearts[i].sprite = full ? fullHeart : emptyHeart;

                if (full != _lastFull[i]) { _pulseT[i] = 0f; _lastFull[i] = full; }
                float s = 1f;
                if (_pulseT[i] < 0.3f)
                {
                    _pulseT[i] += Time.unscaledDeltaTime;
                    // quick pop-out then settle (damage) — reads instantly on a small screen
                    s = 1f + Mathf.Sin(Mathf.Clamp01(_pulseT[i] / 0.3f) * Mathf.PI) * 0.45f;
                }
                else if (low && full)
                {
                    // remaining heart beats while Pip is on his last ember
                    s = 1f + Mathf.Sin(_lowBeat * 7f) * 0.07f;
                }
                if (_rts[i] != null) _rts[i].localScale = new Vector3(s, s, 1f);
            }
        }

        void TriggerFlash()
        {
            EnsureFlash();
            _flashT = 0.35f;
            if (_flash != null) _flash.enabled = true;
        }

        // full-screen red-edge vignette on the HUD canvas — built at runtime so
        // committed scenes need no new wiring
        void EnsureFlash()
        {
            if (_flash != null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            if (_vignetteSprite == null) _vignetteSprite = MakeVignette();
            var go = new GameObject("DamageFlash", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = _vignetteSprite;
            img.color = new Color(0.85f, 0.1f, 0.1f, 0f);
            img.raycastTarget = false;
            img.enabled = false;
            go.transform.SetSiblingIndex(0); // behind hearts so they stay readable
            _flash = img;
        }

        static Sprite MakeVignette()
        {
            const int N = 128;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = Mathf.Abs(x + 0.5f - N / 2f) / (N / 2f);
                    float v = Mathf.Abs(y + 0.5f - N / 2f) / (N / 2f);
                    float edge = Mathf.Max(u, v);
                    float a = Mathf.Clamp01((edge - 0.45f) / 0.55f);
                    px[y * N + x] = new Color(1f, 1f, 1f, a * a);
                }
            t.SetPixels(px); t.Apply();
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            t.name = "fx_vignette";
            return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
