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
public static class WallUpgradeValidation
{
    const string Pending="WallUpgrade.Pending";
    static WallUpgradeValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>Button(name).onClick.Invoke();
    static void Rules()
    {
        var state=VillageState.Create(1000);
        Check(state.TryPlaceWallRow(5,5,3,0,1000,out _),"Seed row");
        Check(state.TryStartUpgrade(1,1000,out _) && state.gold==875 && state.BusyBuilders==1 && state.IsValid(),"Level 2 costs 50 gold and one builder");
        Check(!state.TryStartUpgrade(1,1000,out _) && state.gold==875,"No duplicate start");
        Check(state.TryStartUpgrade(2,1000,out _) && !state.CanUpgrade(3,out _),"Walls share both builders");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(state),out var loaded) && loaded.BusyBuilders==2,"Active timers round trip");
        loaded.Accrue(1029);Check(loaded.buildings[1].level==1,"No early completion");
        loaded.Accrue(1030);Check(loaded.buildings[1].level==2 && loaded.BusyBuilders==0 && loaded.IsValid(),"Thirty-second completion");
        Check(!loaded.CanUpgrade(1,out string reason) && reason.Contains("Town Hall"),"Level 3 requires Town Hall 2");
        loaded.buildings[0].level=2;
        Check(loaded.TryStartUpgrade(1,1030,out _) && loaded.gold==700 && loaded.buildings[1].upgradeFinishes==1150,"Level 3 costs 125 gold and 120 seconds");
        loaded.Accrue(2000);Check(loaded.buildings[1].level==3 && !loaded.CanUpgrade(1,out _) && loaded.IsValid(),"Offline completion and maximum tier");
        Check(loaded.TryMove(1,6,6,out _) && loaded.buildings[1].level==3,"Move retains level");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(loaded),out var restored) && restored.buildings[1].level==3,"Completed levels round trip");
        restored.buildings[1].level=4;Check(!restored.IsValid(),"Reject unsupported wall tier");
        restored=loaded.Copy();restored.buildings[0].level=1;Check(!restored.IsValid(),"Reject completed tier beyond Town Hall allowance");
        restored=state.Copy();restored.buildings[1].upgradeFinishes++;Check(!restored.IsValid(),"Reject malformed timer");
        restored=state.Copy();restored.buildings[1].level=3;Check(!restored.IsValid(),"Reject active upgrade beyond maximum");
        Check(state.TryCancelUpgrade(1,1001,out _) && state.gold==850 && state.buildings[1].level==1 && state.BusyBuilders==1,"Cancel gives half back and preserves old tier");
        Check(!state.TryCancelUpgrade(1,1001,out _) && state.gold==850,"Cannot refund twice");
        state.gold=state.GoldCapacity-10;
        Check(state.TryCancelUpgrade(2,1001,out _) && state.gold==state.GoldCapacity,"Refund capped by storage");
        state.gold=49;Check(!state.TryStartUpgrade(3,1001,out _) && state.gold==49,"Insufficient gold rejected");
        state.gold=50;Check(state.TryStartUpgrade(3,1001,out _) && state.gold==0,"Exact affordability");
        Check(state.TryMove(3,8,6,out _) && state.buildings[3].upgradeFinishes==1031,"Active wall can move without losing timer");
        state.Accrue(1031);Check(!state.TryCancelUpgrade(3,1031,out _) && state.gold==0 && state.buildings[3].level==2,"Completion cannot refund");
        var legacy=VillageState.Create(1000);legacy.TryPlace("Wall",5,5,1000,out _);
        for(int version=3;version<=9;version++)
        {
            legacy.version=version;
            Check(VillageState.TryDeserialize(JsonUtility.ToJson(legacy),out var migrated) && migrated.buildings[1].level==1 && migrated.CanUpgrade(1,out _),"Existing wall saves still load at version "+version);
        }
        typeof(WallRowValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(WallAuthoring).GetMethod("StateChecks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project and identity.");
        try
        {
            Rules();WallAuthoring.RenderPortrait();
            var state=VillageState.Create(VillageState.Now);state.buildings[0].level=2;
            state.TryPlaceWallRow(5,5,3,0,VillageState.Now,out _);state.buildings[2].level=2;state.buildings[3].level=3;
            Check(VillageSave.TryWrite(state,out _) && PlayerProfile.TrySaveName("WallChief",out _),"Seed UI");
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");SessionState.SetInt("WallUpgrade.Step",0);SessionState.SetString("WallUpgrade.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name,int width=1600,int height=702)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height});
    static WallSegment Wall(int x,int z)=>GameObject.Find("Wall ("+x+", "+z+")").GetComponent<WallSegment>();
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("WallUpgrade.Start","0")))).TotalSeconds<180,"UI timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("WallUpgrade.Step",0);
            if(step==0)
            {
                game.SendMessage("RefreshHUD");
                Check(Mathf.Approximately(Wall(6,5).transform.localScale.y,1.2f) && Mathf.Approximately(Wall(7,5).transform.localScale.y,1.4f),"Distinct level heights restored");
                Wall(6,5).RefreshConnections();Check(Wall(6,5).east.activeSelf && Wall(6,5).west.activeSelf,"Mixed levels connect");
                var block=new MaterialPropertyBlock();Wall(7,5).transform.Find("Coping").GetComponent<Renderer>().GetPropertyBlock(block);Check(block.GetColor("_BaseColor").r>.8f,"Fortified trim uses instance color");
                Capture("wall-upgrade-levels.png");
                game.SelectBuilding(1);game.OpenBuildingDetails();
                Check(Button("Confirm Upgrade").interactable && Button("Confirm Upgrade").GetComponentInChildren<Text>().text.Contains("50 GOLD") && GameObject.Find("Upgrade Duration").GetComponent<Text>().text.Contains("30s"),"Level 2 cost and duration shown");
                Check(GameObject.Find("Details Stats").GetComponent<Text>().text.Contains("not used in battles"),"Home combat limitation visible");
                Check(GameObject.Find("Upgrade Model").GetComponent<RawImage>().texture!=null,"Wall model portrait available");
                Capture("wall-upgrade-details.png");Capture("wall-upgrade-details-16x9.png",1280,720);
                string disk=PlayerPrefs.GetString(VillageSave.Key);int gold=game.State.gold;game.State.elixir=-1;game.StartSelectedUpgrade();
                Check(game.DetailsOpen && game.State.gold==gold && game.State.BusyBuilders==0 && PlayerPrefs.GetString(VillageSave.Key)==disk,"Failed save cannot charge or start wall upgrade");game.State.elixir=500;
                game.StartSelectedUpgrade();Check(game.State.gold==gold-50 && game.State.BusyBuilders==1 && !game.DetailsOpen,"UI commits upgrade");
                Click("Builders");Check(Button("Builder Job 0").GetComponentInChildren<Text>().text.Contains("Wall"),"Wall in builder queue");Click("Builder Job 0");
                Click("Confirm Upgrade");Check(game.State.BusyBuilders==1,"Cancellation requires confirmation");Capture("wall-upgrade-cancel.png");
                Click("Confirm Upgrade");Check(game.State.gold==gold-25 && game.State.BusyBuilders==0,"Confirmed refund");
                game.SelectBuilding(1);game.OpenBuildingDetails();game.StartSelectedUpgrade();
                SessionState.SetInt("WallUpgrade.Step",1);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==1)
            {
                Check(game.State.BusyBuilders==1 && game.State.buildings[1].level==1,"Active wall upgrade survives scene reload");
                game.State.Accrue(game.State.buildings[1].upgradeFinishes);game.SendMessage("RefreshHUD");
                Check(game.State.buildings[1].level==2 && Mathf.Approximately(Wall(5,5).transform.localScale.y,1.2f),"Completion updates wall model");
                game.SelectBuilding(3);game.OpenBuildingDetails();Check(!Button("Confirm Upgrade").interactable && Button("Confirm Upgrade").GetComponentInChildren<Text>().text=="MAX LEVEL","Maximum tier UI");game.CloseBuildingDetails();
                game.BeginSelectedMove();Check(Mathf.Approximately(GameObject.Find("Move Preview").transform.localScale.y,1.4f),"Move preview retains wall tier");game.SetPreviewCell(8,5);game.ConfirmPlacement();
                Check(game.State.buildings[3].level==3 && game.State.buildings[3].x==8,"Move retains upgraded data");
                SessionState.SetInt("WallUpgrade.Step",2);SceneManager.LoadScene("Main Scene");return;
            }
            Check(game.State.buildings[1].level==2 && game.State.buildings[3].level==3 && game.State.BusyBuilders==0 && Mathf.Approximately(Wall(8,5).transform.localScale.y,1.4f),"Completed and moved tiers survive reload");
            Finish("PASS: wall levels 1-3; 50/125 gold and 30/120 second upgrades; shared builder limits; Town Hall gating; duplicate, insufficient-funds and malformed-state rejection; offline completion; storage-capped half refunds and completion/cancellation race; movement during upgrades; existing versions 3-9 wall saves and active/completed save round trips. Live UI validates cost/time, explicit home-combat limitation, rejected-save rollback, builder queue, two-click cancellation, mixed-level connections, distinct height/instance color, move preview and scene reload. Wall-row, wall-authoring, economy/progression, recovery, replay, history, campaign, mixed-army, Tank and tutorial rule regressions passed. Unity "+Application.unityVersion+". Screenshots at 1600x702 and 1280x720. Home walls do not affect battles; physical phone and APK rebuild not tested.",0);
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"WallUpgradeValidation.txt"),message);EditorApplication.Exit(code);}
}
