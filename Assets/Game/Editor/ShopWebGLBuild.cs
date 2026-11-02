using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Run with -batchmode -buildTarget WebGL -executeMethod ShopWebGLBuild.Build.
public static class ShopWebGLBuild
{
    public static void Build()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (pipeline == null) throw new InvalidOperationException("Missing WebGL render pipeline.");
        // Give shader preprocessing a default pipeline as well as a quality override.
        GraphicsSettings.defaultRenderPipeline = pipeline;
        int previousQuality = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(Array.IndexOf(QualitySettings.names, "Mobile"), true);
        var stripping = GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
        stripping.stripUnusedVariants = true;
        EditorUtility.SetDirty(AssetDatabase.LoadMainAssetAtPath("Assets/Settings/UniversalRenderPipelineGlobalSettings.asset"));
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        AssetDatabase.SaveAssets();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Game/Scenes/ShopPrototype.unity" },
            locationPathName = "Builds/WebGL", target = BuildTarget.WebGL,
            options = BuildOptions.DetailedBuildReport
        });
        QualitySettings.SetQualityLevel(previousQuality, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"SHOP_WEBGL_BUILD: {report.summary.result}, errors={report.summary.totalErrors}, warnings={report.summary.totalWarnings}, bytes={report.summary.totalSize}");
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("WebGL build failed.");
    }
}
