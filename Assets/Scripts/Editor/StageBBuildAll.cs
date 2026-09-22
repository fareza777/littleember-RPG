using UnityEditor;
using UnityEngine;

namespace LittleEmber.EditorTools
{
    /// <summary>
    /// Stage B batch entry: build the real Emberholt village scene, then the
    /// interiors, then the greybox (kept as a dev/test scene), then the shell.
    /// Shell runs last so it owns the final EditorBuildSettings scene order:
    /// Splash, MainMenu, Cinematic, Emberholt_Village, Interior_Elder,
    /// Interior_Home, Emberholt_Greybox.
    /// Run: -executeMethod LittleEmber.EditorTools.StageBBuildAll.Run
    /// NOTE: the full combined run has flaked once (editor died after the
    /// village save); if that happens, run the steps individually via
    /// GreyboxAndShell / the LittleEmber menu items.
    /// </summary>
    public static class StageBBuildAll
    {
        [MenuItem("LittleEmber/StageB/Run All (Village + Interiors + Greybox + Shell)")]
        public static void Run()
        {
            VillageSceneBuilder.Build();
            InteriorSceneBuilder.Build();
            GreyboxAndShell();
            Debug.Log("[StageB] Full Stage B build finished.");
        }

        [MenuItem("LittleEmber/StageB/Run Greybox + Shell Only")]
        public static void GreyboxAndShell()
        {
            GreyboxSceneBuilder.Build();
            ShellSceneBuilder.BuildAll();
            Debug.Log("[StageB] Greybox + shell finished.");
        }
    }
}
