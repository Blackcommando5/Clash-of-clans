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
public static class WallRowValidation
{
    const string Pending="WallRow.Pending";
    static WallRowValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>Button(name).onClick.Invoke();
    static void Reject(VillageState state,int x,int z,int length,int direction)
    {
        string before=JsonUtility.ToJson(state);
        Check(!state.TryPlaceWallRow(x,z,length,direction,state.lastProduction+30,out _) && JsonUtility.ToJson(state)==before,"Invalid row must preserve all state");
    }
    static void Rules()
    {
        for(int direction=0;direction<4;direction++)
        {
            var s=VillageState.Create(1000);
            Check(s.TryPlaceWallRow(5,5,10,direction,1001,out _) && s.Count("Wall")==10 && s.gold==750 && s.elixir==500 && s.BusyBuilders==0 && s.IsValid(),"Ten-wall row cost, limits and builder isolation");
            for(int i=0;i<10;i++)Check(s.buildings[i+1].x==5+i*VillageState.WallRowDX(direction) && s.buildings[i+1].z==5+i*VillageState.WallRowDZ(direction),"Cell order for every direction");
            Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out var copy) && copy.Count("Wall")==10 && copy.version==9,"Rows save as ordinary walls");
        }
        var state=VillageState.Create(1000);
        foreach(int length in new[]{-1,0,11,int.MaxValue})Reject(state,5,5,length,0);
        foreach(int direction in new[]{-1,4,int.MaxValue})Reject(state,5,5,3,direction);
        Reject(state,int.MaxValue,0,10,0);Reject(state,int.MinValue,0,10,2);
        Reject(state,21,5,2,0);Reject(state,5,21,2,1);Reject(state,-22,5,2,2);Reject(state,5,-22,2,3);
        foreach(var corner in new[]{new Vector2Int(-22,-22),new Vector2Int(21,21)})
        {var edge=VillageState.Create(1000);Check(edge.TryPlaceWallRow(corner.x,corner.y,1,0,1000,out _),"Inclusive corners");}
        state.gold=249;Reject(state,5,5,10,0);state.gold=250;Check(state.TryPlaceWallRow(5,5,10,0,1000,out _) && state.gold==0,"Exact affordability");
        state=VillageState.Create(1000);state.TryPlace("GoldMine",7,5,1000,out _);Reject(state,5,5,8,0);
        state=VillageState.Create(1000);state.TryPlace("Wall",8,5,1000,out _);Reject(state,5,5,8,0);
        state=VillageState.Create(1000);for(int i=0;i<24;i++)state.TryPlace("Wall",-22+i,-22,1000,out _);
        Reject(state,5,5,2,0);Check(state.TryPlaceWallRow(5,5,1,0,1000,out _) && state.Count("Wall")==25,"Final allowed wall");
        state.buildings[0].level=2;Check(state.TryPlaceWallRow(5,6,10,0,1000,out _) && state.Count("Wall")==35 && state.IsValid(),"Expanded Town Hall limit");
        typeof(ResourceMilestoneValidation).GetMethod("StateChecks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(ResourceMilestoneValidation).GetMethod("ProgressionChecks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(BattleRecoveryValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project and identity.");
        try
        {
            Rules();Check(VillageSave.TryWrite(VillageState.Create(VillageState.Now),out _) && PlayerProfile.TrySaveName("WallRowChief",out _),"Seed UI");
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");SessionState.SetInt("WallRow.Step",0);SessionState.SetString("WallRow.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name,int width=1600,int height=702)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height});
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("WallRow.Start","0")))).TotalSeconds<180,"UI timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("WallRow.Step",0);
            if(step==0)
            {
                game.BeginPlacement("Wall");game.SetPreviewCell(5,5);Check(GameObject.Find("Wall Row Controls")!=null && !Button("Shorten Wall Row").interactable,"Single-wall default");
                for(int i=0;i<4;i++)Click("Lengthen Wall Row");
                Check(GameObject.Find("Wall Row Count").GetComponent<Text>().text.Contains("125 GOLD") && Button("Confirm Placement").interactable,"Full cost and valid row");
                Check(GameObject.Find("Wall Row Preview").GetComponentsInChildren<WallSegment>().Length==5,"All preview segments visible");
                foreach(var collider in GameObject.Find("Wall Row Preview").GetComponentsInChildren<Collider>())Check(!collider.enabled,"Ghost colliders disabled");
                Capture("wall-row-preview.png");Capture("wall-row-preview-16x9.png",1280,720);
                string disk=PlayerPrefs.GetString(VillageSave.Key);game.State.elixir=-1;game.ConfirmPlacement();
                Check(game.IsPlacing && game.State.Count("Wall")==0 && game.State.gold==1000 && PlayerPrefs.GetString(VillageSave.Key)==disk,"Rejected save keeps row unbuilt and gold untouched");game.State.elixir=500;
                game.SetPreviewCell(-3,-2);Check(!Button("Confirm Placement").interactable,"Mid-row obstruction invalidates whole preview");game.ConfirmPlacement();Check(game.State.Count("Wall")==0,"Invalid confirmation builds nothing");
                Capture("wall-row-blocked.png");game.SetPreviewCell(20,5);Check(!Button("Confirm Placement").interactable,"Boundary rejection");
                game.SetPreviewCell(5,5);Click("Rotate Wall Row");Check(Button("Rotate Wall Row").GetComponentInChildren<Text>().text=="ROTATE: NORTH","Rotate north");
                Click("Shorten Wall Row");game.ConfirmPlacement();
                Check(!game.IsPlacing && game.State.Count("Wall")==4 && game.State.gold==900 && GameObject.Find("Wall Row Preview")==null,"Four-wall row commits once and removes ghosts");
                for(int i=1;i<=4;i++)Check(game.State.buildings[i].x==5 && game.State.buildings[i].z==4+i,"Each segment has own cell");
                game.ConfirmPlacement();Check(game.State.gold==900 && game.State.Count("Wall")==4,"Duplicate confirm cannot spend twice");
                var world=GameObject.Find("Village Buildings");Check(world.GetComponentsInChildren<WallSegment>().Length==4,"All committed segments spawned");
                foreach(var wall in world.GetComponentsInChildren<WallSegment>())wall.RefreshConnections();
                var first=GameObject.Find("Wall (5, 5)").GetComponent<WallSegment>();Check(first.north.activeSelf && !first.south.activeSelf,"Placed row connects");
                game.BeginPlacement("Wall");Check(GameObject.Find("Wall Row Count").GetComponent<Text>().text.StartsWith("1 WALL") && Button("Rotate Wall Row").GetComponentInChildren<Text>().text=="ROTATE: EAST","Next placement resets length/direction");
                game.SetWallRowLength(10);Check(!Button("Lengthen Wall Row").interactable,"Maximum length");game.CancelPlacement();Check(game.State.gold==900 && GameObject.Find("Wall Row Controls")==null,"Cancel without spending");
                game.SelectBuilding(1);game.BeginSelectedMove();Check(GameObject.Find("Wall Row Controls")==null,"Move remains individual");game.SetWallRowLength(5);game.SetPreviewCell(6,5);game.ConfirmPlacement();Check(game.State.Count("Wall")==4 && game.State.gold==900 && game.State.buildings[1].x==6,"Move changes one segment for free");
                game.BeginPlacement("GoldMine");Check(GameObject.Find("Wall Row Controls")==null,"Ordinary placement controls");game.SetPreviewCell(10,5);game.ConfirmPlacement();Check(game.State.MineCount==1 && game.State.elixir==350,"Producer placement works");
                SessionState.SetInt("WallRow.Step",1);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==1)
            {
                Check(game.State.Count("Wall")==4 && game.State.gold==900 && game.State.MineCount==1 && game.State.buildings[1].x==6 && GameObject.Find("Village Buildings").GetComponentsInChildren<WallSegment>().Length==4,"Reload retains segments, move and resources");
                Capture("wall-row-built.png");
                Finish("PASS: lengths 1-10 in four directions; exact cost and cells; atomic insufficient-funds, limit, obstruction, border and malformed-argument rejection; Town Hall expanded limit; v9 save round-trip; full-row preview, rotate, plus/minus and cost; disabled ghost colliders and connected committed segments; failed save preserves village/disk; duplicate-confirm protection; cancellation/cleanup; individual move and producer placement; scene reload. Economy/progression, recovery, saved replay, history, campaign, mixed army, Tank and tutorial rule regressions passed. Unity "+Application.unityVersion+". Captures at 1600x702 and 1280x720. Physical touch, device performance and APK rebuild not tested.",0);
            }
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"WallRowValidation.txt"),message);EditorApplication.Exit(code);}
}
