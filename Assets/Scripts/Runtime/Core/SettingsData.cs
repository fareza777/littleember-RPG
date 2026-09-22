using System;
using UnityEngine;

namespace LittleEmber.Core
{
    /// <summary>
    /// PlayerPrefs-backed settings: volumes, quality tier, vibration, onboarding flag.
    /// Loads before first scene; quality is re-applied after scene load so it wins
    /// over BootTuning's defaults regardless of init order.
    /// </summary>
    public static class SettingsData
    {
        const string KMaster = "vol_master";
        const string KMusic = "vol_music";
        const string KSfx = "vol_sfx";
        const string KVo = "vol_vo";
        const string KQuality = "quality";
        const string KVibration = "vibration";
        const string KOnboarding = "onboarding_done";

        public static float Master { get; private set; } = 1f;
        public static float Music { get; private set; } = 0.8f;
        public static float Sfx { get; private set; } = 1f;
        public static float Vo { get; private set; } = 1f;
        /// <summary>0 = battery (30fps), 1 = balanced (60fps), 2 = performance (120fps).</summary>
        public static int Quality { get; private set; } = 1;
        public static bool Vibration { get; private set; } = true;
        public static bool OnboardingDone { get; private set; }

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadFromPrefs()
        {
            Master = PlayerPrefs.GetFloat(KMaster, 1f);
            Music = PlayerPrefs.GetFloat(KMusic, 0.8f);
            Sfx = PlayerPrefs.GetFloat(KSfx, 1f);
            Vo = PlayerPrefs.GetFloat(KVo, 1f);
            Quality = PlayerPrefs.GetInt(KQuality, 1);
            Vibration = PlayerPrefs.GetInt(KVibration, 1) == 1;
            OnboardingDone = PlayerPrefs.GetInt(KOnboarding, 0) == 1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplyAfterSceneLoad() => ApplyQuality();

        public static void ApplyQuality()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Quality == 0 ? 30 : (Quality == 2 ? 120 : 60);
        }

        public static void SetMaster(float v) { Master = Clamp01(v); PlayerPrefs.SetFloat(KMaster, Master); Changed?.Invoke(); }
        public static void SetMusic(float v) { Music = Clamp01(v); PlayerPrefs.SetFloat(KMusic, Music); Changed?.Invoke(); }
        public static void SetSfx(float v) { Sfx = Clamp01(v); PlayerPrefs.SetFloat(KSfx, Sfx); Changed?.Invoke(); }
        public static void SetVo(float v) { Vo = Clamp01(v); PlayerPrefs.SetFloat(KVo, Vo); Changed?.Invoke(); }
        public static void SetQuality(int q) { Quality = Mathf.Clamp(q, 0, 2); PlayerPrefs.SetInt(KQuality, Quality); ApplyQuality(); Changed?.Invoke(); }
        public static void SetVibration(bool on) { Vibration = on; PlayerPrefs.SetInt(KVibration, on ? 1 : 0); Changed?.Invoke(); }

        public static void MarkOnboardingDone()
        {
            OnboardingDone = true;
            PlayerPrefs.SetInt(KOnboarding, 1);
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            LoadFromPrefs();
            ApplyQuality();
            Changed?.Invoke();
        }

        public static void Vibrate()
        {
            if (Vibration) Handheld.Vibrate();
        }

        static float Clamp01(float v) => Mathf.Clamp01(v);
    }
}
