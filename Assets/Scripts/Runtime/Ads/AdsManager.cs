using System;
using UnityEngine;
#if UNITY_ANDROID || UNITY_EDITOR
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace LittleEmber.Ads
{
    /// <summary>
    /// AdMob wrapper. Uses GOOGLE TEST IDS — swap for real units before release.
    /// Banner: main menu bottom. Interstitial: between chapters (>= 3 min cooldown).
    /// Rewarded: hint / revive / support hooks (RewardEarned event).
    /// Everything is a logged no-op in the Editor and off-Android.
    /// Consent (UMP) is gathered once before MobileAds.Initialize.
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance { get; private set; }

        // ---- Google AdMob TEST ids (https://developers.google.com/admob/unity/test-ads) ----
        const string BannerId = "ca-app-pub-3940256099942544/6300978111";
        const string InterstitialId = "ca-app-pub-3940256099942544/1033173712";
        const string RewardedId = "ca-app-pub-3940256099942544/5224354917";

        const float InterstitialCooldownSec = 180f;

#if UNITY_ANDROID || UNITY_EDITOR
        BannerView _banner;
        InterstitialAd _interstitial;
        RewardedAd _rewarded;
#endif
        float _lastInterstitialAt = -9999f;
        bool _initialized;

        public static event Action RewardEarned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[AdsManager]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AdsManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GatherConsentThenInit();
        }

        // ------------------------------------------------------------------ init + consent

        void GatherConsentThenInit()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                ConsentInformation.Update(new ConsentRequestParameters(), (FormError updateError) =>
                {
                    if (updateError != null)
                        Debug.LogWarning("[Ads] Consent update failed: " + updateError.Message);

                    if (ConsentInformation.CanRequestAds())
                    {
                        ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
                        {
                            if (formError != null)
                                Debug.LogWarning("[Ads] Consent form failed: " + formError.Message);
                            InitAds();
                        });
                    }
                    else
                    {
                        InitAds();
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Ads] Consent flow crashed, continuing without: " + e.Message);
                InitAds();
            }
#else
            Debug.Log("[Ads] Editor/non-Android: ads are stubbed.");
#endif
        }

        void InitAds()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                MobileAds.Initialize(_ =>
                {
                    _initialized = true;
                    LoadInterstitial();
                    LoadRewarded();
                    Debug.Log("[Ads] MobileAds initialized.");
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Ads] MobileAds.Initialize failed: " + e.Message);
            }
#endif
        }

        // ------------------------------------------------------------------ banner

        public static void ShowBanner()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Instance == null || !Instance._initialized) return;
            try
            {
                if (Instance._banner == null)
                {
                    Instance._banner = new BannerView(BannerId, AdSize.Banner, AdPosition.Bottom);
                    Instance._banner.LoadAd(new AdRequest());
                }
                Instance._banner.Show();
            }
            catch (Exception e) { Debug.LogWarning("[Ads] Banner: " + e.Message); }
#endif
        }

        public static void HideBanner()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Instance == null || Instance._banner == null) return;
            try { Instance._banner.Hide(); }
            catch (Exception e) { Debug.LogWarning("[Ads] HideBanner: " + e.Message); }
#endif
        }

        // ------------------------------------------------------------------ interstitial

        void LoadInterstitial()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                InterstitialAd.Load(InterstitialId, new AdRequest(), (ad, error) =>
                {
                    if (error != null) { Debug.LogWarning("[Ads] Interstitial load: " + error.GetMessage()); return; }
                    _interstitial = ad;
                });
            }
            catch (Exception e) { Debug.LogWarning("[Ads] Interstitial load crashed: " + e.Message); }
#endif
        }

        /// <summary>Show between chapters. Respects cooldown; returns true if an ad actually showed.</summary>
        public static bool TryShowInterstitial()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Instance == null || !Instance._initialized) return false;
            if (Time.unscaledTime - Instance._lastInterstitialAt < InterstitialCooldownSec) return false;
            var ad = Instance._interstitial;
            if (ad == null || !ad.CanShowAd()) return false;
            try
            {
                Instance._interstitial = null;
                Instance._lastInterstitialAt = Time.unscaledTime;
                ad.OnAdFullScreenContentClosed += () =>
                {
                    ad.Destroy();
                    Instance.LoadInterstitial();
                };
                ad.Show();
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Ads] Interstitial show: " + e.Message); return false; }
#else
            return false;
#endif
        }

        // ------------------------------------------------------------------ rewarded

        void LoadRewarded()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                RewardedAd.Load(RewardedId, new AdRequest(), (ad, error) =>
                {
                    if (error != null) { Debug.LogWarning("[Ads] Rewarded load: " + error.GetMessage()); return; }
                    _rewarded = ad;
                });
            }
            catch (Exception e) { Debug.LogWarning("[Ads] Rewarded load crashed: " + e.Message); }
#endif
        }

        public static bool RewardedReady
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Instance != null && Instance._rewarded != null && Instance._rewarded.CanShowAd();
#else
                return false;
#endif
            }
        }

        /// <summary>Show a rewarded ad; RewardEarned fires on completion. Returns false if unavailable.</summary>
        public static bool ShowRewarded()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Instance == null || !Instance._initialized) return false;
            var ad = Instance._rewarded;
            if (ad == null || !ad.CanShowAd()) return false;
            try
            {
                Instance._rewarded = null;
                ad.OnAdFullScreenContentClosed += () =>
                {
                    ad.Destroy();
                    Instance.LoadRewarded();
                };
                ad.Show((Reward reward) => RewardEarned?.Invoke());
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Ads] Rewarded show: " + e.Message); return false; }
#else
            Debug.Log("[Ads] Rewarded stub (editor).");
            return false;
#endif
        }
    }
}
