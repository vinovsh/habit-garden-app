using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HabitGarden.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("Habit Garden/Prepare project")]
        public static void Prepare() {
            Directory.CreateDirectory("Assets/Settings");Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Assets/Resources");AssetDatabase.Refresh();
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/GardenPipeline.asset");
            if(pipeline==null) {
                var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,"Assets/Settings/GardenRenderer.asset");
                pipeline=UniversalRenderPipelineAsset.Create(renderer);pipeline.renderScale=1;pipeline.supportsHDR=false;pipeline.msaaSampleCount=2;pipeline.shadowDistance=8;
                AssetDatabase.CreateAsset(pipeline,"Assets/Settings/GardenPipeline.asset");
            }
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            // Keep runtime generated Lit materials in player shader dependencies.
            if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/GardenLit.mat")==null)AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Lit")),"Assets/Resources/GardenLit.mat");
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");var lit=Shader.Find("Universal Render Pipeline/Lit");bool present=false;
            for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==lit)present=true;
            if(!present){int index=shaders.arraySize;shaders.InsertArrayElementAtIndex(index);shaders.GetArrayElementAtIndex(index).objectReferenceValue=lit;graphics.ApplyModifiedPropertiesWithoutUndo();}
            PlayerSettings.companyName="RenderMind";PlayerSettings.productName="Habit Garden";PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.rendermind.habitgarden");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel28;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.applicationEntry=AndroidApplicationEntry.Activity;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.SetPropertyInt("activeInputHandler",0,BuildTargetGroup.Android);
            string path="Assets/Scenes/Bootstrap.unity";
            if(!File.Exists(path)){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.SaveScene(scene,path);}
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};AssetDatabase.SaveAssets();
        }
        [MenuItem("Habit Garden/Build test APK")]
        public static void BuildApk() {
            Prepare();Directory.CreateDirectory("Builds");EditorUserBuildSettings.buildAppBundle=false;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/Bootstrap.unity"},locationPathName="Builds/HabitGarden-test.apk",target=BuildTarget.Android,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android APK build failed: "+report.summary.result);
        }
    }
}
