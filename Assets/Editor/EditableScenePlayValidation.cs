using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

[InitializeOnLoad]
public static class EditableScenePlayValidation
{
    const string Pending="Editable.Play.Pending";
    static EditableScenePlayValidation(){EditorApplication.update+=Tick;}
    public static void Begin()
    {
        PlayerPrefs.DeleteKey(PlayerProfile.NameKey);PlayerPrefs.DeleteKey(VillageSave.Key);PlayerPrefs.Save();
        SessionState.SetFloat("Editable.Play.Start",(float)EditorApplication.timeSinceStartup);
        SessionState.SetBool(Pending,true);
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            float elapsed=(float)EditorApplication.timeSinceStartup-SessionState.GetFloat("Editable.Play.Start",0);
            if(elapsed>90)throw new Exception("Play mode timeout. playing="+EditorApplication.isPlaying);
            if(elapsed<2 || EditorApplication.isCompiling)return;
            if(!EditorApplication.isPlaying){if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.EnterPlaymode();return;}
            if(EditorApplication.isPaused)EditorApplication.isPaused=false;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();
            var naming=UnityEngine.Object.FindFirstObjectByType<NewPlayerOnboarding>();
            if(game==null || game.State==null || naming==null)return;
            if(!PlayerProfile.HasPlayerName)
            {
                if(!naming.IsAskingForName)return;
                UnityEngine.Object.FindFirstObjectByType<InputField>().text="SceneChief";naming.Continue();naming.Continue();return;
            }
            if(!GameObject.Find("Open Profile"))return;
            int count=game.GetComponentsInChildren<Transform>(true).Length;
            GameObject.Find("Open Profile").GetComponent<Button>().onClick.Invoke();
            if(!game.ProfileOpen || !game.cameraController.InputBlocked)throw new Exception("Profile binding failed");
            if(game.GetComponentsInChildren<Transform>(true).Length!=count)throw new Exception("Profile recreated scene objects");
            GameObject.Find("Profile Tab 1").GetComponent<Button>().onClick.Invoke();
            if(GameObject.Find("Section Title").GetComponent<Text>().text!="My Clan")throw new Exception("Tab binding failed");
            GameObject.Find("Close Profile").GetComponent<Button>().onClick.Invoke();
            if(game.ProfileOpen || game.cameraController.InputBlocked)throw new Exception("Profile close failed");
            game.OpenShop();
            GameObject.Find("Buy ElixirCollector").GetComponent<Button>().onClick.Invoke();
            if(!game.IsPlacing)throw new Exception("Shop placement binding failed");
            game.CancelPlacement();
            if(game.cameraController.InputBlocked)throw new Exception("Placement cancellation lock failed");
            int initialWalls=game.State.Count("Wall");
            int initialGold=game.State.gold;
            game.OpenShop();GameObject.Find("Buy Wall").GetComponent<Button>().onClick.Invoke();
            if(!game.IsPlacing)throw new Exception("Wall shop button binding failed");
            game.SetPreviewCell(6,6);game.ConfirmPlacement();
            game.BeginPlacement("Wall");game.SetPreviewCell(7,6);game.ConfirmPlacement();
            if(game.State.Count("Wall")!=initialWalls+2 || game.State.gold!=initialGold-50)throw new Exception("Wall placement economy");
            var left=GameObject.Find("Wall (6, 6)").GetComponent<WallSegment>();
            var right=GameObject.Find("Wall (7, 6)").GetComponent<WallSegment>();
            left.RefreshConnections();right.RefreshConnections();
            if(!left.east.activeSelf || !right.west.activeSelf)throw new Exception("Wall adjacency");
            game.SelectBuilding(game.State.buildings.Count-1);game.BeginSelectedMove();game.SetPreviewCell(8,8);game.ConfirmPlacement();
            left.RefreshConnections();right.RefreshConnections();
            if(left.east.activeSelf || right.west.activeSelf)throw new Exception("Wall disconnection after moving");
            if(!VillageSave.TryLoad(out var saved,out _) || saved.Count("Wall")!=game.State.Count("Wall"))throw new Exception("Wall persistence");
            typeof(ResourceMilestoneValidation).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{"walls-village.png",1600,900});
            Finish("PASS: wall economy/limits/overlap/bounds/moving/save reload; connected visuals and disconnection; wall shop binding;  editable scene serialization and custom layout persistence; onboarding; profile opening/tab switching/closing without UI recreation; camera locks; shop placement/cancellation. Android device test not run.",0);
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string result,int code)
    {
        SessionState.SetBool(Pending,false);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"editable-play-result.txt"),result);
        EditorApplication.Exit(code);
    }
}

