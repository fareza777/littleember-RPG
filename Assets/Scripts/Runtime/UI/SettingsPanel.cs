using UnityEngine;
using UnityEngine.UI;
using LittleEmber.Core;

namespace LittleEmber.UI
{
    /// <summary>
    /// Settings: volume sliders, quality segment buttons (Low/Balanced/High),
    /// vibration toggle, two-tap save-data reset. Binds straight to SettingsData.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public Slider masterSlider;
        public Slider musicSlider;
        public Slider sfxSlider;
        public Slider voSlider;
        public Button[] qualityButtons = new Button[0]; // 0=Low 1=Balanced 2=High
        public Toggle vibrationToggle;
        public Button resetButton;
        public Text resetLabel;
        public Button backButton;

        public System.Action onBack;

        static readonly Color QualityOn = new Color(0.85f, 0.55f, 0.20f);
        static readonly Color QualityOff = new Color(0.16f, 0.20f, 0.30f);

        bool _loading;
        float _resetArmUntil = -1f;

        void OnEnable()
        {
            _loading = true;
            if (masterSlider != null) masterSlider.value = SettingsData.Master;
            if (musicSlider != null) musicSlider.value = SettingsData.Music;
            if (sfxSlider != null) sfxSlider.value = SettingsData.Sfx;
            if (voSlider != null) voSlider.value = SettingsData.Vo;
            if (vibrationToggle != null) vibrationToggle.isOn = SettingsData.Vibration;
            RefreshQualityButtons();
            DisarmReset();
            _loading = false;
        }

        void Start()
        {
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(v => { if (!_loading) SettingsData.SetMaster(v); });
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(v => { if (!_loading) SettingsData.SetMusic(v); });
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(v => { if (!_loading) SettingsData.SetSfx(v); });
            if (voSlider != null) voSlider.onValueChanged.AddListener(v => { if (!_loading) SettingsData.SetVo(v); });
            for (int i = 0; i < qualityButtons.Length; i++)
            {
                int idx = i;
                if (qualityButtons[idx] != null)
                    qualityButtons[idx].onClick.AddListener(() => { SettingsData.SetQuality(idx); RefreshQualityButtons(); });
            }
            if (vibrationToggle != null) vibrationToggle.onValueChanged.AddListener(b => { if (!_loading) SettingsData.SetVibration(b); });
            if (resetButton != null) resetButton.onClick.AddListener(OnResetTapped);
            if (backButton != null) backButton.onClick.AddListener(() => onBack?.Invoke());
        }

        void Update()
        {
            if (_resetArmUntil > 0f && Time.unscaledTime > _resetArmUntil) DisarmReset();
        }

        void RefreshQualityButtons()
        {
            for (int i = 0; i < qualityButtons.Length; i++)
            {
                if (qualityButtons[i] == null) continue;
                var img = qualityButtons[i].GetComponent<Image>();
                if (img != null) img.color = i == SettingsData.Quality ? QualityOn : QualityOff;
            }
        }

        void OnResetTapped()
        {
            if (Time.unscaledTime < _resetArmUntil)
            {
                SaveSystem.DeleteAll();
                SettingsData.ResetAll();
                _loading = false; // let OnEnable run its own loading guard
                OnEnable();
                if (resetLabel != null) resetLabel.text = "SAVE DATA ERASED";
            }
            else
            {
                _resetArmUntil = Time.unscaledTime + 3f;
                if (resetLabel != null) resetLabel.text = "TAP AGAIN TO ERASE";
            }
        }

        void DisarmReset()
        {
            _resetArmUntil = -1f;
            if (resetLabel != null) resetLabel.text = "ERASE SAVE DATA";
        }
    }
}
