using UnityEngine;
using UnityEngine.UI;
using LittleEmber.Core;
using LittleEmber.Player;

namespace LittleEmber.UI
{
    /// <summary>
    /// First-run tutorial cards in the greybox HUD: move, attack, roll, then two
    /// lore/mechanic beats (torches = safe ground, lantern dims when hurt).
    /// Runs once (SettingsData.OnboardingDone); tapping the card skips a step.
    /// </summary>
    public class OnboardingHints : MonoBehaviour
    {
        public GameObject card;
        public Text hintText;
        public Text progressText;
        public VirtualJoystick joystick;
        public PipController pip;

        enum Step { Move, Attack, Roll, Torches, Lantern, Done }
        Step _step = Step.Move;
        float _progress; // seconds accumulated for the current step

        static readonly string[] Texts =
        {
            "Use the stick to walk. Push it far to run.",
            "Tap ATK to swing. Chain three hits for a combo.",
            "Tap ROLL to dash — you cannot be hurt mid-roll.",
            "Torchlight marks safe ground. Follow the lit path.",
            "Your lantern dims when you are hurt. Guard your little flame."
        };

        void Start()
        {
            if (SettingsData.OnboardingDone || card == null)
            {
                if (card != null) card.SetActive(false);
                enabled = false;
                return;
            }
            card.SetActive(true);
            ApplyStep();
            if (pip != null)
            {
                pip.AttackPressed += OnAttack;
                pip.RollPressed += OnRoll;
            }
            var btn = card.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => Advance());
        }

        void OnDestroy()
        {
            if (pip != null)
            {
                pip.AttackPressed -= OnAttack;
                pip.RollPressed -= OnRoll;
            }
        }

        void OnAttack() { if (_step == Step.Attack) Advance(); }
        void OnRoll() { if (_step == Step.Roll) Advance(); }

        void Update()
        {
            switch (_step)
            {
                case Step.Move:
                    if (joystick != null && joystick.Value.magnitude > 0.25f)
                    {
                        _progress += Time.deltaTime;
                        if (_progress > 0.5f) Advance();
                    }
                    break;
                case Step.Torches:
                case Step.Lantern:
                    _progress += Time.deltaTime;
                    if (_progress > 4.5f) Advance();
                    break;
            }
        }

        void Advance()
        {
            if (_step >= Step.Done) return;
            _step++;
            _progress = 0f;
            if (_step == Step.Done)
            {
                SettingsData.MarkOnboardingDone();
                card.SetActive(false);
                enabled = false;
                return;
            }
            ApplyStep();
        }

        void ApplyStep()
        {
            if (hintText != null) hintText.text = Texts[(int)_step];
            if (progressText != null) progressText.text = $"{(int)_step + 1} / {Texts.Length}  ·  tap card to skip";
        }
    }
}
