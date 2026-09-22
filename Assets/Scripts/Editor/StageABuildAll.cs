using UnityEditor;
using UnityEngine;

namespace LittleEmber.EditorTools
{
    /// <summary>
    /// Stage A batch entry: rebuild the greybox (with onboarding card), then the
    /// shell (splash/menu/cinematic/icons). Shell runs last so it owns the final
    /// EditorBuildSettings scene order: Splash, MainMenu, Cinematic, Greybox.
    /// Run: -executeMethod LittleEmber.EditorTools.StageABuildAll.Run
    /// </summary>
    public static class StageABuildAll
    {
        [MenuItem("LittleEmber/StageA/Run All (Greybox + Shell)")]
        public static void Run()
        {
            GreyboxSceneBuilder.Build();
            ShellSceneBuilder.BuildAll();
            Debug.Log("[StageA] Full Stage A build finished.");
        }
    }
}
