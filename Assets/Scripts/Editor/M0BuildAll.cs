using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LittleEmber.EditorTools
{
    /// <summary>
    /// One-shot M0 entry point, safe for batchmode:
    ///   Unity.exe -batchmode -quit -projectPath C:\LittleEmber
    ///     -executeMethod LittleEmber.EditorTools.M0BuildAll.Run -logFile Logs\m0.log
    /// </summary>
    public static class M0BuildAll
    {
        [MenuItem("LittleEmber/M0/0 - Generate Everything (animations + greybox)")]
        public static void Run()
        {
            HeroAnimatorBuilder.Generate();
            GreyboxSceneBuilder.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("[M0] DONE — hero animations generated, Emberholt greybox built.");
        }
    }

    /// <summary>Debug APK for device smoke-tests (release AAB + keystore is an M5 task).</summary>
    public static class ApkBuild
    {
        [MenuItem("LittleEmber/Build/Build Debug APK")]
        public static void Build()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new Exception("No enabled scenes in Build Settings.");

            Directory.CreateDirectory("Builds");
            string apk = Path.GetFullPath(Path.Combine("Builds", "LittleEmber_M0.apk"));
            var report = BuildPipeline.BuildPlayer(scenes, apk, BuildTarget.Android, BuildOptions.Development);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"APK build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
            Debug.Log($"[Build] APK OK → {apk} ({report.summary.totalSize / (1024 * 1024)} MB)");
        }
    }
}
