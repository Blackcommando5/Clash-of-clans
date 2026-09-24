using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidDevelopmentBuild
{
    static string Argument(string name,string fallback)
    {
        var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]==name)return args[i+1];return fallback;
    }
    [MenuItem("Kingdoms/Build Android Development APK")]
    public static void Run()
    {
        string root=Path.GetFullPath(Argument("-kingdomsBuildRoot",Path.Combine(Path.GetDirectoryName(Application.dataPath),"Builds","Android")));
        Directory.CreateDirectory(root);string reportPath=Path.Combine(root,"build-report.txt");
        try
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Install Android Build Support with SDK, NDK and OpenJDK for this Unity version.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)throw new InvalidOperationException("Select Android before building, or launch Unity with -buildTarget Android.");
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            if(scenes.Length!=2 || scenes[0]!="Assets/Scenes/WelcomeScene.unity" || scenes[1]!="Assets/Scenes/Main Scene.unity")throw new InvalidOperationException("Expected WelcomeScene then Main Scene in build settings.");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.kingdoms.prototype");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore=false;
            EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
            string apk=Path.Combine(root,"Kingdoms-development.apk");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=apk,target=BuildTarget.Android,options=BuildOptions.Development});
            string errors=string.Join("\n",report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error || m.type==LogType.Exception).Select(m=>m.content));
            string text="Result: "+report.summary.result+"\nUnity: "+Application.unityVersion+"\nSource: "+Argument("-kingdomsSourceRevision","working tree")+
                "\nPackage: "+PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)+"\nVersion: "+PlayerSettings.bundleVersion+" ("+PlayerSettings.Android.bundleVersionCode+")"+
                "\nBackend: IL2CPP\nArchitecture: ARM64\nDevelopment build: yes\nSigning: Android debug keystore\nScenes: "+string.Join(", ",scenes)+
                "\nAPK: "+apk+"\nBytes: "+report.summary.totalSize+"\nDuration: "+report.summary.totalTime+"\nWarnings: "+report.summary.totalWarnings+"\nErrors: "+report.summary.totalErrors+"\n"+errors;
            File.WriteAllText(reportPath,text);Debug.Log(text);
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Android build did not succeed. See "+reportPath);
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        catch(Exception e)
        {
            File.AppendAllText(reportPath,"\nFAIL: "+e);Debug.LogException(e);
            if(Application.isBatchMode)EditorApplication.Exit(1);else throw;
        }
    }
}
