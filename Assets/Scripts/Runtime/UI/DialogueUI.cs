using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
            if (panel != null) panel.SetActive(false);
        }

        public void Show(DialogueLine[] dialogue, System.Action onClosed = null)
        {
            if (dialogue == null || dialogue.Length == 0 || IsOpen) return;
            // never soft-lock the player: refuse to open half-wired
            if (panel == null || nameText == null || bodyText == null)
            {
                Debug.LogError($"[Dialogue] UI not fully wired (panel={panel != null}, name={nameText != null}, body={bodyText != null}) — talk aborted.");
                return;
            }
            lines = dialogue;
            index = 0;
            this.onClosed = onClosed;
            IsOpen = true;
            if (pip != null) pip.controlsLocked = true;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            StartLine();
        }

        void Update()
        {
            // emergency escape (Android back button) — never trap the player
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
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
            typeRoutine = StartCoroutine(TypeLine(line.text));
        }

        IEnumerator TypeLine(string text)
        {
            typing = true;
            bodyText.text = "";
            int shown = 0;
            float timer = 0f;
            while (shown < text.Length)
            {
                timer += Time.unscaledDeltaTime * charsPerSecond;
                int target = Mathf.Min(text.Length, (int)timer);
                if (target != shown)
                {
                    shown = target;
                    bodyText.text = text.Substring(0, shown);
                }
                yield return null;
            }
            typing = false;
            if (continueHint != null) continueHint.enabled = true;
        }

        void Close()
        {
            IsOpen = false;
            panel.SetActive(false);
            if (pip != null) pip.controlsLocked = false;
            var cb = onClosed;
            onClosed = null;
            cb?.Invoke();
        }
    }
}
