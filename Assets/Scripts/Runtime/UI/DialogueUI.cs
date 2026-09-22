using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using LittleEmber.Audio;
using LittleEmber.Player;

namespace LittleEmber.UI
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speaker;
        public Sprite portrait;
        [TextArea(2, 4)] public string text;
    }

    /// <summary>
    /// Bottom-anchored dialogue box: portrait (left, gold ring), speaker name,
    /// typewriter body, blinking continue ember. Tap anywhere on the panel to
    /// complete the current line / advance. Locks Pip's controls while open.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("Wired by scene builder")]
        public GameObject panel;
        public Image portraitImage;
        public Image portraitRing;
        public Text nameText;
        public Text bodyText;
        public Text continueHint;
        public PipController pip;

        [Header("Feel")]
        public float charsPerSecond = 45f;

        public static bool IsOpen { get; private set; }

        DialogueLine[] lines;
        int index;
        bool typing;
        Coroutine typeRoutine;
        System.Action onClosed;

        void Awake()
        {
            // Dialogue roots are saved inactive; Awake is deferred until the first
            // SetActive(true) inside Show() — self-hiding there would kill the box
            // before it ever appears (IsOpen/controlsLocked stuck forever).
            if (panel != null && !IsOpen) panel.SetActive(false);
        }

        void OnDestroy()
        {
            // scene unloaded with the box open — don't wedge talks in the next scene
            if (IsOpen) IsOpen = false;
        }

        public bool Show(DialogueLine[] dialogue, System.Action onClosed = null)
        {
            if (dialogue == null || dialogue.Length == 0 || IsOpen) return false;
            // never soft-lock the player: refuse to open half-wired
            if (panel == null || nameText == null || bodyText == null)
            {
                Debug.LogError($"[Dialogue] UI not fully wired (panel={panel != null}, name={nameText != null}, body={bodyText != null}) — talk aborted.");
                return false;
            }
            lines = dialogue;
            index = 0;
            this.onClosed = onClosed;
            IsOpen = true;
            if (pip != null)
            {
                pip.controlsLocked = true;
                if (pip.joystick != null) pip.joystick.Clear();
            }
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            AudioManager.DuckMusic(true);
            AudioManager.PlaySfxName("talk");
            StartLine();
            PlayOpenAnim();
            return true;
        }

        void Update()
        {
            // emergency escape (Android back button) — never trap the player
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
            // gentle blink on the continue ember once a line is fully typed
            if (continueHint != null && continueHint.enabled)
            {
                var c = continueHint.color;
                c.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
                continueHint.color = c;
            }
        }

        /// <summary>Called by the full-panel invisible button.</summary>
        public void OnPanelTapped()
        {
            if (!IsOpen) return;
            if (lines == null || lines.Length == 0) { Close(); return; }
            if (typing)
            {
                // complete instantly
                if (typeRoutine != null) StopCoroutine(typeRoutine);
                typing = false;
                bodyText.text = lines[index].text;
                if (continueHint != null) continueHint.enabled = true;
                return;
            }
            index++;
            if (index >= lines.Length) Close();
            else StartLine();
        }

        void StartLine()
        {
            var line = lines[index];
            nameText.text = line.speaker;
            bool hasPortrait = line.portrait != null && portraitImage != null;
            if (portraitImage != null) portraitImage.enabled = hasPortrait;
            if (portraitRing != null) portraitRing.enabled = hasPortrait;
            if (hasPortrait) portraitImage.sprite = line.portrait;
            if (continueHint != null) continueHint.enabled = false;
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typing = false;
            typeRoutine = StartCoroutine(TypeLine(line.text ?? ""));
        }

        IEnumerator TypeLine(string text)
        {
            typing = true;
            bodyText.text = "";
            int shown = 0;
            int charsUntilBlip = 0;
            float timer = 0f;
            while (shown < text.Length)
            {
                timer += Time.unscaledDeltaTime * charsPerSecond;
                int target = Mathf.Min(text.Length, (int)timer);
                if (target != shown)
                {
                    shown = target;
                    bodyText.text = text.Substring(0, shown);
                    // soft tick every few visible chars — classic JRPG voice blip
                    if (--charsUntilBlip <= 0 && !char.IsWhiteSpace(text[shown - 1]))
                    {
                        charsUntilBlip = 3;
                        AudioManager.PlaySfxName("blip", 0.45f);
                    }
                    // breathe on sentence punctuation instead of printing at a flat rate
                    char tail = text[shown - 1];
                    if (tail == '.' || tail == '!' || tail == '?' || tail == '—')
                        timer -= charsPerSecond * 0.16f;
                    else if (tail == ',' || tail == ';' || tail == ':')
                        timer -= charsPerSecond * 0.08f;
                }
                yield return null;
            }
            typing = false;
            if (continueHint != null) continueHint.enabled = true;
        }

        /// <summary>Panel slide-up + portrait pop on open (pure runtime, no scene wiring).</summary>
        void PlayOpenAnim()
        {
            var inner = panel != null ? panel.transform.Find("Panel") : null;
            if (inner == null) return;
            var rt = inner as RectTransform;
            if (rt == null) return;
            StartCoroutine(SlideIn(rt));
        }

        IEnumerator SlideIn(RectTransform rt)
        {
            Vector2 basePos = rt.anchoredPosition;
            float t = 0f;
            const float dur = 0.22f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float ease = 1f - (1f - k) * (1f - k) * (1f - k);
                rt.anchoredPosition = basePos + Vector2.down * (1f - ease) * 60f;
                yield return null;
            }
            rt.anchoredPosition = basePos;
        }

        void Close()
        {
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            if (pip != null) pip.controlsLocked = false;
            AudioManager.DuckMusic(false);
            var cb = onClosed;
            onClosed = null;
            cb?.Invoke();
        }
    }
}
