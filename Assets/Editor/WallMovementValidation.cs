using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

[InitializeOnLoad]
public static class WallMovementValidation
{
    const string Pending="WallMovement.Pending";
    static WallMovementValidation(){EditorApplication.update+=Tick;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>Button(name).onClick.Invoke();
    static void Reject(VillageState state,int index,int x,int z)
    {
        string before=JsonUtility.ToJson(state);
        Check(!state.TryMoveConnectedWalls(index,x,z,out _) && JsonUtility.ToJson(state)==before,"Rejected move preserves all data");
    }
    static VillageState Seed(long now)
    {
        var state=VillageState.Create(now);state.buildings[0].level=2;
        state.TryPlaceWallRow(5,5,3,0,now,out _);state.TryPlaceWallRow(7,6,2,1,now,out _);
        state.TryPlace("Wall",8,8,now,out _);state.TryPlace("GoldMine",10,5,now,out _);
        state.buildings[1].level=3;state.buildings[2].level=2;
        Check(state.TryStartUpgrade(2,now,out _) && state.IsValid(),"Seed wall timer");return state;
    }
    static void Rules()
    {
        var state=Seed(1000);var members=state.ConnectedWalls(3);
        Check(members.Count==5 && members[0]==1 && members[4]==5,"L section and stable order from middle anchor");
        Check(state.ConnectedWalls(6).Count==1 && state.ConnectedWalls(0).Count==0 && state.ConnectedWalls(-1).Count==0 && state.ConnectedWalls(int.MaxValue).Count==0,"Diagonal, nonwall and invalid selection");
        Reject(state,0,5,5);Reject(state,-1,5,5);Reject(state,3,int.MaxValue,0);Reject(state,3,0,int.MinValue);
        Reject(state,3,-22,5);Reject(state,3,5,21);Reject(state,3,22,5);Reject(state,3,5,-23);
        Reject(state,3,10,5);Reject(state,3,8,6);Reject(state,3,0,0);
        int gold=state.gold,elixir=state.elixir;long finish=state.buildings[2].upgradeFinishes;
        Check(state.TryMoveConnectedWalls(3,6,5,out _) && state.IsValid(),"Translate into own former cells");
        Check(state.buildings[1].x==4 && state.buildings[3].x==6 && state.buildings[5].z==7 && state.buildings[6].x==8,"Move all and only connected walls");
        Check(state.gold==gold && state.elixir==elixir && state.BusyBuilders==1 && state.buildings[1].level==3 && state.buildings[2].upgradeFinishes==finish,"No economy, level or timer changes");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(state),out var loaded) && loaded.buildings[1].x==4 && loaded.buildings[2].upgradeFinishes==finish,"Moved timers and positions round trip");
        loaded.Accrue(finish);Check(loaded.buildings[2].level==3 && loaded.IsValid(),"Upgrade completes at new cell");
        Check(state.TryMoveConnectedWalls(3,8,5,out _) && state.ConnectedWalls(3).Count==6,"Moving beside separate section connects it on next selection");
        var ring=VillageState.Create(1000);
        for(int x=-3;x<=2;x++)for(int z=-3;z<=2;z++)if(x==-3 || x==2 || z==-3 || z==2)ring.TryPlace("Wall",x,z,1000,out _);
        Check(ring.ConnectedWalls(1).Count==20 && ring.CanMoveConnectedWalls(1,-3,-3,out _),"Enclosure excludes its interior Town Hall");
        Check(ring.TryMoveConnectedWalls(1,5,5,out _) && ring.buildings[0].x==-2 && ring.IsValid(),"Closed loop moves without enclosed building");
        var branch=VillageState.Create(1000);branch.TryPlaceWallRow(5,5,3,0,1000,out _);branch.TryPlaceWallRow(6,6,3,1,1000,out _);
        Check(branch.ConnectedWalls(2).Count==6 && branch.TryMoveConnectedWalls(2,0,10,out _) && branch.IsValid(),"Branch moves as one section");
        var maximum=VillageState.Create(1000);maximum.buildings[0].level=3;maximum.gold=10000;
        for(int x=-22;x<=2;x++)for(int z=-22;z<=-20;z++)Check(maximum.TryPlace("Wall",x,z,1000,out _),"Maximum section setup");
        Check(maximum.ConnectedWalls(1).Count==75 && maximum.TryMoveConnectedWalls(1,-21,-19,out _) && maximum.IsValid(),"All 75 allowed walls translate");
        var single=VillageState.Create(1000);single.TryPlace("Wall",5,5,1000,out _);
        foreach(var edge in new[]{new Vector2Int(-22,-22),new Vector2Int(21,21),new Vector2Int(-22,21),new Vector2Int(21,-22)})Check(single.TryMoveConnectedWalls(1,edge.x,edge.y,out _) && single.IsValid(),"Inclusive border corners");
        typeof(WallUpgradeValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project and identity.");
        try
        {
            Rules();Check(VillageSave.TryWrite(Seed(VillageState.Now),out _) && PlayerProfile.TrySaveName("WallMover",out _),"Seed UI");
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");SessionState.SetInt("WallMovement.Step",0);SessionState.SetString("WallMovement.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name,int width=1600,int height=702)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height});
    static void Begin(VillageGameplay game){game.SelectBuilding(3);game.OpenBuildingDetails();Click("Move Connected Walls");}
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("WallMovement.Start","0")))).TotalSeconds<180,"UI timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("WallMovement.Step",0);
            if(step==0)
            {
                game.SelectBuilding(3);game.OpenBuildingDetails();Check(Button("Move Connected Walls").GetComponentInChildren<Text>().text.Contains("5 WALLS"),"Connected count in details");Capture("wall-move-action.png");
                string original=JsonUtility.ToJson(game.State);Click("Move Connected Walls");
                Check(game.IsPlacing && !game.DetailsOpen && game.cameraController.InputBlocked && Button("Confirm Placement").GetComponentInChildren<Text>().text=="MOVE WALLS","Group mode and camera lock");
                var ghosts=GameObject.Find("Connected Wall Preview").GetComponentsInChildren<WallSegment>();Check(ghosts.Length==5,"Every selected wall previewed");
                Check(GameObject.Find("Connected Wall Footprints").transform.childCount==5 && GameObject.Find("Wall Row Controls")==null,"Exact shape footprints without purchase controls");
                foreach(var collider in GameObject.Find("Connected Wall Preview").GetComponentsInChildren<Collider>())Check(!collider.enabled,"Ghosts cannot intercept taps");
                Check(Mathf.Approximately(ghosts[0].transform.localScale.y,1.4f),"Mixed levels preserved in preview");
                game.SetPreviewCell(9,5);Capture("wall-move-preview.png");Capture("wall-move-preview-16x9.png",1280,720);Check(Button("Confirm Placement").interactable,"Free valid destination");
                game.SetPreviewCell(10,5);Check(!Button("Confirm Placement").interactable,"Collision disables confirmation");game.ConfirmPlacement();Check(JsonUtility.ToJson(game.State)==original,"Invalid confirmation cannot change any data");Capture("wall-move-blocked.png");
                game.CancelPlacement();Check(!game.IsPlacing && !game.cameraController.InputBlocked && GameObject.Find("Wall (5, 5)")!=null && JsonUtility.ToJson(game.State)==original,"Cancel restores original section without spending");
                Begin(game);game.SetPreviewCell(6,5);
                string disk=PlayerPrefs.GetString(VillageSave.Key);game.State.elixir=-1;game.ConfirmPlacement();Check(game.IsPlacing && game.State.buildings[1].x==5 && PlayerPrefs.GetString(VillageSave.Key)==disk,"Failed save retains old cells and preview");game.State.elixir=350;
                long finish=game.State.buildings[2].upgradeFinishes;game.ConfirmPlacement();Check(!game.IsPlacing && game.State.buildings[1].x==4 && game.State.buildings[5].x==6 && game.State.buildings[6].x==8 && game.State.buildings[2].upgradeFinishes==finish,"Whole section saves with original timer");
                string committed=JsonUtility.ToJson(game.State);game.ConfirmPlacement();Check(JsonUtility.ToJson(game.State)==committed,"Duplicate confirmation inert");
                Check(GameObject.Find("Connected Wall Preview")==null && GameObject.Find("Connected Wall Footprints")==null && GameObject.Find("Wall (4, 5)")!=null,"Ghost cleanup and committed world positions");
                SessionState.SetString("WallMovement.Timer",finish.ToString());SessionState.SetInt("WallMovement.Step",1);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==1)
            {
                Check(game.State.buildings[1].x==4 && game.State.buildings[2].upgradeFinishes.ToString()==SessionState.GetString("WallMovement.Timer","") && game.State.BusyBuilders==1,"Saved group and timer survive reload");
                game.SelectBuilding(1);game.BeginSelectedMove();Check(GameObject.Find("Connected Wall Preview")==null && Button("Confirm Placement").GetComponentInChildren<Text>().text=="MOVE","Single move remains available");game.CancelPlacement();
                game.BeginPlacement("Wall");Check(GameObject.Find("Wall Row Controls")!=null && Button("Confirm Placement").GetComponentInChildren<Text>().text=="BUILD","Purchase mode restored");game.CancelPlacement();
                game.SelectBuilding(0);game.OpenBuildingDetails();Check(GameObject.Find("Move Connected Walls")==null,"Wall action hidden for other buildings");game.CloseBuildingDetails();
                Begin(game);game.SetPreviewCell(9,9);SessionState.SetInt("WallMovement.Step",2);SceneManager.LoadScene("Main Scene");return;
            }
            Check(game.State.buildings[1].x==4 && !game.IsPlacing && GameObject.Find("Wall (4, 5)")!=null,"Scene teardown discards unconfirmed preview");Capture("wall-move-saved.png");
            Finish("PASS: cardinal connected sections, middle anchors, branches, loops, isolated/diagonal walls and maximum 75-wall section; exact-shape collision including enclosed buildings; self-overlap, four borders, malformed targets and atomic rejection; joining sections; free translation preserves levels, builders and upgrade timers; save round trip and upgrade completion. Live UI validates count, whole-section ghosts and footprints, mixed levels, disabled ghost colliders, valid/blocked previews, cancellation, failed-save rollback, duplicate confirmation, world refresh, restart persistence, single-move/purchase isolation and unconfirmed scene teardown. Wall-upgrade, wall-row, economy/progression, recovery, replay, history, campaign, mixed-army, Tank and tutorial rule regressions passed. Unity "+Application.unityVersion+". Screenshots at 1600x702 and 1280x720. Physical touch and APK rebuild not tested.",0);
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"WallMovementValidation.txt"),message);EditorApplication.Exit(code);}
}
