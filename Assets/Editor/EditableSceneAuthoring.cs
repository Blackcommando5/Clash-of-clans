using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Kingdoms.UI;

[CustomEditor(typeof(VillageGameplay))]
public sealed class VillageGameplayEditor : Editor
{
    int wallX,wallZ,wallLength=1;
    bool wallAlongZ;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var game=(VillageGameplay)target;
        if(Application.isPlaying)return;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Edit scene screens",EditorStyles.boldLabel);
        if(game.HasEditableInterface && GUILayout.Button("Apply screenshot reference interface"))
        {
            Undo.RegisterFullObjectHierarchyUndo(game.gameObject,"Apply screenshot reference interface");
            game.BuildScreenshotReferenceInterface();
            EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }
        if(!game.HasEditableInterface && GUILayout.Button("Create editable interface"))
        {
            Undo.RegisterFullObjectHierarchyUndo(game.gameObject,"Create village interface");
            game.BuildEditableInterface();game.CreateEditorVillage();
            EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }
        EditorGUILayout.LabelField("Starter wall layout",EditorStyles.boldLabel);
        wallX=EditorGUILayout.IntField("Grid X",wallX);wallZ=EditorGUILayout.IntField("Grid Z",wallZ);
        wallLength=EditorGUILayout.IntSlider("Length",wallLength,1,25);
        wallAlongZ=EditorGUILayout.Toggle("Along Z",wallAlongZ);
        if(game.wallPrefab!=null && GUILayout.Button("Add wall line"))
        {
            for(int i=0;i<wallLength;i++)game.AddEditorWall(wallX+(wallAlongZ?0:i),wallZ+(wallAlongZ?i:0));
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }
        string[] screens={"Village HUD","Shop","Building Details","Placement","My Profile","My Clan","Clans","Social","My Buildings","Attack","Settings","Builder Base","Clan Capital"};
        for(int i=0;i<screens.Length;i++)
            if(GUILayout.Button("Show "+screens[i]))
            {
                Undo.RegisterFullObjectHierarchyUndo(game.gameObject,"Preview screen");
                game.PreviewEditorScreen(i);
                var naming=FindFirstObjectByType<NewPlayerOnboarding>();if(naming!=null)naming.PreviewEditorDialog(false);
                EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            }
        if(GUILayout.Button("Show Name Dialog"))
        {
            game.PreviewEditorScreen(0);
            var naming=FindFirstObjectByType<NewPlayerOnboarding>();
            if(naming!=null){naming.BuildEditableDialog();naming.PreviewEditorDialog(true);EditorUtility.SetDirty(naming);}
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }
    }
}

public static class EditableSceneAuthoring
{
    [MenuItem("Kingdoms/Create Editable Scene Interface")]
    public static void Create()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();
        if(game==null)throw new InvalidOperationException("Open Main Scene first.");
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play mode before authoring.");
        Undo.RegisterFullObjectHierarchyUndo(game.gameObject,"Create editable village");
        game.BuildEditableInterface();game.CreateEditorVillage();
        var naming=UnityEngine.Object.FindFirstObjectByType<NewPlayerOnboarding>();
        naming.BuildEditableDialog();EditorUtility.SetDirty(naming);
        EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        Selection.activeGameObject=game.gameObject;
    }

    public static void Batch()
    {
        string root=Path.GetDirectoryName(Application.dataPath);
        if(!Application.dataPath.Replace(Path.DirectorySeparatorChar,'/').EndsWith("/.utmp/ResourceValidation/Assets"))
            throw new InvalidOperationException("Batch authoring requires the isolated project.");
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            Create();
            WallAuthoring.Build();
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();
            var buttons=game.GetComponentsInChildren<Button>(true);
            if(buttons.Length<30)throw new Exception("Expected editable menu buttons.");
            // Prove authored geometry survives save/reload and runtime event binding.
            var badge=Array.Find(game.GetComponentsInChildren<RectTransform>(true),x=>x.name=="Open Profile");
            var position=badge.anchoredPosition;
            badge.anchoredPosition+=new Vector2(7,3);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();
            badge=Array.Find(game.GetComponentsInChildren<RectTransform>(true),x=>x.name=="Open Profile");
            if(badge.anchoredPosition!=position+new Vector2(7,3))throw new Exception("Scene edit not preserved.");
            int before=game.GetComponentsInChildren<Transform>(true).Length;
            typeof(VillageGameplay).GetMethod("BindEditableInterface",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
            if(game.GetComponentsInChildren<Transform>(true).Length!=before)throw new Exception("Binding rebuilt UI.");
            if(badge.anchoredPosition!=position+new Vector2(7,3))throw new Exception("Binding overwrote authored layout.");
            badge.anchoredPosition=position;
            game.PreviewEditorScreen(4);
            Canvas.ForceUpdateCanvases();
            game.PreviewEditorScreen(0);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(root,"editable-result.txt"),"PASS: scene authored, "+buttons.Length+" editable buttons, scene reload, custom RectTransform persistence and runtime binding without UI recreation. Play mode smoke test still required.");
            EditableScenePlayValidation.Begin();
        }
        catch(Exception e)
        {
            File.WriteAllText(Path.Combine(root,"editable-result.txt"),"FAIL: "+e);
            Debug.LogException(e);EditorApplication.Exit(1);
        }
    }
}



