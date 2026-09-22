using UnityEngine;
using UnityEngine.UI;
using LittleEmber.Ads;

namespace LittleEmber.UI
{
    /// <summary>
    /// About: credits, Rate (Play Store), Share (Android sheet), Support (rewarded ad), Back.
    /// </summary>
    public class AboutPanel : MonoBehaviour
    {
        public Button rateButton;
        public Button shareButton;
        public Button supportAdButton;
        public Text supportLabel;
        public Button backButton;
        public Text creditsText;

        public System.Action onBack;

        const string PackageId = "com.littleember.game";

        void Start()
        {
            if (rateButton != null)
                rateButton.onClick.AddListener(() => Application.OpenURL("market://details?id=" + PackageId));
            if (shareButton != null)
                shareButton.onClick.AddListener(Share);
            if (supportAdButton != null)
                supportAdButton.onClick.AddListener(SupportWithAd);
            if (backButton != null)
                backButton.onClick.AddListener(() => onBack?.Invoke());

            if (creditsText != null && string.IsNullOrEmpty(creditsText.text))
            {
                creditsText.text =
                    "A tiny lantern against the coming dark.\n\n" +
                    "Design, code && story — LittleEmber Studio\n" +
                    "Pixel art — Super Retro Collection (Gifs)\n" +
                    "Narration — ElevenLabs voice synthesis\n" +
                    "Engine — Unity\n\n" +
                    "Every lantern lit pushes the Long Dark\nback a little further.";
            }
        }

        void OnEnable()
        {
            if (supportLabel != null)
                supportLabel.text = AdsManager.RewardedReady ? "WATCH AN AD TO SUPPORT US" : "SUPPORT US (AD LOADING…)";
        }

        void Share()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var intent = new AndroidJavaObject("android.content.Intent"))
                {
                    intent.Call<AndroidJavaObject>("setAction", "android.intent.action.SEND");
                    intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT",
                        "I'm playing LITTLE EMBER — a tiny lantern against the dark. " +
                        "https://play.google.com/store/apps/details?id=" + PackageId);
                    intent.Call<AndroidJavaObject>("setType", "text/plain");
                    using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share LITTLE EMBER"))
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        activity.Call("startActivity", chooser);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[About] Share failed: " + e.Message);
            }
#else
            Debug.Log("[About] Share (editor stub).");
#endif
        }

        void SupportWithAd()
        {
            bool shown = AdsManager.ShowRewarded();
            if (supportLabel != null)
                supportLabel.text = shown ? "THANK YOU, EMBER-KEEPER!" : "NO AD AVAILABLE — THANKS ANYWAY!";
        }
    }
}
