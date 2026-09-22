using UnityEngine;
using UnityEngine.UI;
using LittleEmber.Core;
using LittleEmber.Ads;

namespace LittleEmber.UI
{
    /// <summary>
    /// Main menu: New Game / Continue / Settings / About + name entry for new heroes.
    /// Banner ad shows here only (hidden on leave). Android Back navigates panels, quits at root.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject mainPanel;
        public GameObject namePanel;
        public GameObject settingsPanel;
        public GameObject aboutPanel;

        [Header("Main")]
        public Button newGameButton;
        public Button continueButton;
        public Button settingsButton;
        public Button aboutButton;
        public Text continueInfo;
        public Text versionLabel;

        [Header("Name Entry")]
        public InputField nameInput;
        public Text nameError;
        public Button beginButton;
        public Button nameBackButton;

        [Header("Sub-panel controllers")]
        public SettingsPanel settings;
        public AboutPanel about;

        [Header("Flow")]
        public string cinematicScene = "Cinematic";
        public string gameplaySceneFallback = "Emberholt_Village";

        void Start()
        {
            ShowMain();

            newGameButton.onClick.AddListener(ShowName);
            continueButton.onClick.AddListener(OnContinue);
            settingsButton.onClick.AddListener(ShowSettings);
            aboutButton.onClick.AddListener(ShowAbout);
            beginButton.onClick.AddListener(OnBegin);
            nameBackButton.onClick.AddListener(ShowMain);
            nameInput.onValueChanged.AddListener(_ => ValidateName());

            if (settings != null) settings.onBack = ShowMain;
            if (about != null) about.onBack = ShowMain;

            if (versionLabel != null)
                versionLabel.text = "v" + Application.version + "  ·  test ads";

            AdsManager.ShowBanner();
        }

        void OnDestroy()
        {
            AdsManager.HideBanner();
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (namePanel.activeSelf || settingsPanel.activeSelf || aboutPanel.activeSelf) ShowMain();
            else Application.Quit();
        }

        // ------------------------------------------------------------------ panels

        void SetPanels(bool main, bool name, bool set, bool about)
        {
            mainPanel.SetActive(main);
            namePanel.SetActive(name);
            settingsPanel.SetActive(set);
            aboutPanel.SetActive(about);
        }

        void ShowMain()
        {
            SetPanels(true, false, false, false);
            RefreshContinue();
        }

        void ShowName()
        {
            SetPanels(false, true, false, false);
            if (nameError != null) nameError.text = "";
            if (nameInput != null) nameInput.text = "";
            ValidateName();
        }

        void ShowSettings() => SetPanels(false, false, true, false);
        void ShowAbout() => SetPanels(false, false, false, true);

        void RefreshContinue()
        {
            int slot = SaveSystem.LatestSlot();
            bool has = slot >= 0;
            continueButton.interactable = has;
            if (continueInfo != null)
            {
                if (has)
                {
                    var d = SaveSystem.Peek(slot);
                    continueInfo.text = $"{d.heroName} · Lv {d.level} · {d.savedAt}";
                }
                else continueInfo.text = "";
            }
        }

        // ------------------------------------------------------------------ flow

        void ValidateName()
        {
            if (beginButton == null || nameInput == null) return;
            string n = nameInput.text.Trim();
            bool ok = n.Length == 0; // empty = default "Pip", always allowed
            if (n.Length > 0)
            {
                ok = n.Length <= 12;
                foreach (char c in n)
                    if (!char.IsLetterOrDigit(c) && c != ' ') { ok = false; break; }
            }
            if (nameError != null)
                nameError.text = ok || n.Length == 0 ? "" : "Letters and numbers only, max 12.";
            beginButton.interactable = ok;
        }

        void OnBegin()
        {
            string n = nameInput != null ? nameInput.text.Trim() : "";
            int slot = SaveSystem.PickSlotForNewGame();
            SaveSystem.NewGame(slot, n);
            GameSession.IsNewGame = true;
            GameSession.IsContinue = false;
            SceneFlow.Load(cinematicScene);
        }

        void OnContinue()
        {
            int slot = SaveSystem.LatestSlot();
            if (slot < 0 || !SaveSystem.Load(slot)) return;
            GameSession.IsNewGame = false;
            GameSession.IsContinue = true;
            string scene = SaveSystem.Current != null && !string.IsNullOrEmpty(SaveSystem.Current.sceneName)
                ? SaveSystem.Current.sceneName
                : gameplaySceneFallback;
            SceneFlow.Load(scene);
        }
    }
}
