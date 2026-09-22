using UnityEngine;

namespace LittleEmber.Core
{
    /// <summary>Mobile boot tuning: 60 fps target, no vSync cap.</summary>
    public static class BootTuning
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // belt-and-suspenders portrait lock: hard "Portrait" alone renders the
            // world rotated 90° on some devices (seen on Redmi/MIUI), so we lock
            // via auto-rotation mask (ProjectSettings) AND force it here at boot.
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }
    }
}
