using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ShopAcceptanceBuild
{
    public static void Queue()
    {
        Directory.CreateDirectory("Builds/AcceptanceWebGL");
        File.WriteAllText("Builds/AcceptanceWebGL/build-queued.json", JsonUtility.ToJson(new Queued { queuedUtc = DateTime.UtcNow.ToString("o"), success = false }, true));
        UnityEngine.Debug.Log("[ShopAcceptanceBuild] Acceptance WebGL build queued. Build() runs on the next editor update; watch for build-started.json.");
        EditorApplication.delayCall += Build;
    }

    private static void Build()
    {
        const string status = "Builds/AcceptanceWebGL/build-result.json";
        Directory.CreateDirectory("Builds/AcceptanceWebGL");
        File.WriteAllText("Builds/AcceptanceWebGL/build-started.json", JsonUtility.ToJson(new Queued { queuedUtc = DateTime.UtcNow.ToString("o"), success = false }, true));
        UnityEngine.Debug.Log("[ShopAcceptanceBuild] Acceptance WebGL build started.");
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        int oldQuality = QualitySettings.GetQualityLevel();
        try
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.SetQualityLevel(Array.IndexOf(QualitySettings.names, "Mobile"), true);
            GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>().stripUnusedVariants = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Game/Scenes/ShopPrototype.unity" },
                locationPathName = "Builds/AcceptanceWebGL", target = BuildTarget.WebGL,
                options = BuildOptions.DetailedBuildReport,
                extraScriptingDefines = new[] { "SHOP_ACCEPTANCE" }
            });
            File.WriteAllText(status, JsonUtility.ToJson(new Result
            {
                success = report.summary.result == BuildResult.Succeeded,
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                bytes = report.summary.totalSize, seconds = report.summary.totalTime.TotalSeconds
            }, true));
        }
        catch (Exception error)
        {
            File.WriteAllText(status, JsonUtility.ToJson(new Result { success = false, error = error.ToString() }, true));
        }
        finally { QualitySettings.SetQualityLevel(oldQuality, true); }
    }

    [Serializable]
    private sealed class Result
    {
        public bool success;
        public int errors, warnings;
        public ulong bytes;
        public double seconds;
        public string error;
    }

    [Serializable]
    private sealed class Queued
    {
        public bool success;
        public string queuedUtc;
    }
}
