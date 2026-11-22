using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Builds the AnimatorController for the MPFB Marika model from the takes baked
// into Assets/Game/Resources/Customers/Marika/Marika.fbx. Run via
// ShopMarikaModelBuilder.Generate() after rebuilding the FBX; the .controller
// asset is committed, the game only loads it.
public static class ShopMarikaModelBuilder
{
    private const string Folder = "Assets/Game/Resources/Customers/Marika";
    private const string Fbx = Folder + "/Marika.fbx";
    private const string ControllerPath = Folder + "/Marika.controller";

    public static string Generate()
    {
        AssetDatabase.Refresh();
        var importer = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        if (importer == null) return "Missing " + Fbx + "; run Tools/Art/Build-Marika.py first.";
        importer.animationType = ModelImporterAnimationType.Human;
        importer.SaveAndReimport();
        AnimationClip walk = null, idle = null, present = null, complain = null;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Fbx))
        {
            var clip = o as AnimationClip;
            if (clip == null || clip.name.Contains("__preview__")) continue;
            if (clip.name.Contains("MarikaWalk")) walk = clip;
            else if (clip.name.Contains("MarikaIdle")) idle = clip;
            else if (clip.name.Contains("MarikaPresent")) present = clip;
            else if (clip.name.Contains("MarikaComplain")) complain = clip;
        }
        if (walk == null || idle == null || present == null || complain == null)
            return "Missing takes in " + Fbx + ".";
        foreach (var clip in new[] { walk, idle })
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        while (controller.layers.Length > 0) controller.RemoveLayer(0);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Present", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Complain", AnimatorControllerParameterType.Bool);
        controller.AddLayer("Base");
        var layer = controller.layers[0];
        var idleState = layer.stateMachine.AddState("Idle");
        idleState.motion = idle;
        var walkState = layer.stateMachine.AddState("Walk");
        walkState.motion = walk;
        var presentState = layer.stateMachine.AddState("Present");
        presentState.motion = present;
        presentState.speed = 1f;
        var complainState = layer.stateMachine.AddState("Complain");
        complainState.motion = complain;
        layer.stateMachine.defaultState = idleState;
        AddTransition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, .15f);
        AddTransition(walkState, idleState, "Speed", AnimatorConditionMode.Less, .15f);
        AddBoolTransition(idleState, presentState, "Present", true);
        AddBoolTransition(walkState, presentState, "Present", true);
        AddBoolTransition(presentState, idleState, "Present", false);
        AddBoolTransition(idleState, complainState, "Complain", true);
        AddBoolTransition(complainState, idleState, "Complain", false);
        AssetDatabase.SaveAssets();
        return "Marika controller generated with Walk/Idle/Present/Complain; scene unchanged.";
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, string param,
        AnimatorConditionMode mode, float threshold)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = .15f;
        transition.AddCondition(mode, threshold, param);
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string param, bool value)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = .15f;
        transition.conditions = new[]
        {
            new AnimatorCondition
            {
                mode = value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                parameter = param,
                threshold = 0f
            }
        };
    }
}
