using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

[InitializeOnLoad]
public static class EnemyLayoutValidation
{
    const string Pending="EnemyLayout.Pending";
    static EnemyLayoutValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static VillageState Seed()
    {
        var s=VillageState.Create(VillageState.Now);s.elixir=10000;s.buildings[0].level=2;
        Check(s.TryPlace("Barracks",5,5,s.lastProduction,out _) && s.TryPlace("ArmyCamp",9,5,s.lastProduction,out _) && s.TryPlace("ArmyCamp",9,10,s.lastProduction,out _),"Seed facilities");
        foreach(var b in s.buildings)if(b.kind=="Barracks" || b.kind=="ArmyCamp")b.level=2;
        s.elixir=1000;s.campaignCleared=3;Prepare(s);Check(s.IsValid(),"Valid expanded seed");return s;
    }
    static void Prepare(VillageState s){Check(s.TrySetArmyCount("Raider",8,out _) && s.TrySetArmyCount("Archer",4,out _) && s.TrySetArmyCount("Tank",4,out _),"Prepare 32-space mixed army");}
    static void Rules()
    {
        var s=Seed();Check(s.CampaignUnlocked(2) && !s.CampaignUnlocked(3),"Sequential mission lock");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out var legacy) && legacy.campaignCleared==3 && legacy.version==VillageState.SaveVersion,"Existing v7 progress retained");
        for(int id=0;id<4;id++)
        {
            var layout=EnemyLayoutCatalog.Find(id);var a=new PracticeBattle(layout);var b=new PracticeBattle(layout);
            a.Buildings[0].HitPoints=0;Check(b.Buildings[0].HitPoints==600 && layout.Buildings[0].HitPoints==600,"Layout and encounter isolation");
            Check(!b.DeployAt(layout.MinX-1,layout.MinZ) && !b.DeployAt(layout.MaxX+1,layout.MinZ) && !b.DeployAt(0,layout.MaxZ+1),"Layout-specific deployment bounds");
            Check(b.DeployAt(layout.MinX,layout.MinZ) && b.DeployAt(layout.MaxX,layout.MaxZ),"Layout inclusive corners");
        }
        foreach(int id in new[]{2,3})
        {
            var layout=EnemyLayoutCatalog.Find(id);var battle=new PracticeBattle(layout,16,4,4);var hashes=new List<ulong>();
            for(int i=0;i<4;i++)Check(battle.DeployAt(-600+i*400,-1050,"Tank"),"Tank ground position");
            for(int i=0;i<8;i++)Check(battle.Deploy(i%3),"Raider lanes");hashes.Add(battle.StateHash());
            while(battle.Outcome==PracticeOutcome.Running){battle.Step();if(battle.Tick==10)for(int i=0;i<4;i++)battle.Deploy(i%3,"Archer");hashes.Add(battle.StateHash());}
            Check(battle.Outcome==PracticeOutcome.Victory && battle.TotalBuildings==(id==2 ? 4 : 5),"New layout victory "+id);
            var replay=new PracticeReplay(battle.Record());Check(replay.Battle.Layout==layout && replay.Battle.StateHash()==hashes[0],"Replay preserves layout snapshot");
            while(!replay.Finished){replay.Step();Check(replay.Battle.StateHash()==hashes[replay.Battle.Tick],"New layout every-tick replay");}Check(replay.Matches,"Layout replay match");
            Check(s.TryBeginCampaign(id,out _),"New mission commit");string run=s.campaignRunId;
            var wrong=new PracticeBattle(id==3,16,4,4);wrong.Surrender();Check(!s.TryFinishCampaign(run,wrong.Record(),out _),"Different layout with same enclosure rejected");
            Check(s.TryFinishCampaign(run,battle.Record(),out _) && VillageState.TryDeserialize(JsonUtility.ToJson(s),out s),"New pending reward reload");
            int gold=s.gold,elixir=s.elixir;var mission=CampaignCatalog.Find(id);
            s.gold=s.GoldCapacity;string full=JsonUtility.ToJson(s);Check(!s.TryClaimCampaign(run,out _) && full==JsonUtility.ToJson(s),"Full storage defers reward unchanged");s.gold=gold;
            Check(s.TryClaimCampaign(run,out _) && !s.TryClaimCampaign(run,out _) && s.gold==gold+mission.Gold && s.elixir==elixir+mission.Elixir,"New reward exactly once");
            Prepare(s);Check(s.TryBeginCampaign(id,out _) && s.TryFinishCampaign(s.campaignRunId,battle.Record(),out _) && s.TryClaimCampaign(s.campaignRunId,out _) && s.gold==gold+mission.Gold,"Repeat mission gives no extra reward");Prepare(s);
        }
        Check(s.campaignCleared==15 && VillageState.TryDeserialize(JsonUtility.ToJson(s),out _),"Four-mission completion persists");
        foreach(int invalid in new[]{2,4,5,8,16}){var copy=s.Copy();copy.campaignCleared=invalid;Check(!copy.IsValid(),"Nonsequential mask rejected");}
        var storage=new PracticeBattle(EnemyLayoutCatalog.Hillfort);foreach(var b in storage.Buildings)if(b.Kind!="GoldStorage" && b.Kind!="Wall")b.HitPoints=0;storage.Step();Check(storage.Outcome==PracticeOutcome.Running && storage.Destruction==80 && storage.Stars==2,"Storage counts toward victory and stars");
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated project.");
        try{Rules();EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");Check(VillageSave.TryWrite(Seed(),out _),"UI seed");PlayerProfile.TrySaveName("LayoutChief",out _);SessionState.SetInt("EnemyLayout.Step",0);SessionState.SetString("EnemyLayout.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,1600,702});
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("EnemyLayout.Start","0")))).TotalSeconds<180,"Timeout");if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;int step=SessionState.GetInt("EnemyLayout.Step",0);
            if(step==0){game.OpenCampaign();Check(GameObject.Find("Campaign Crossfire").GetComponent<Button>().interactable && !GameObject.Find("Campaign Hillfort").GetComponent<Button>().interactable,"New mission menu locks");Capture("expanded-campaign.png");Click("Campaign Crossfire");SessionState.SetInt("EnemyLayout.Step",1);return;}
            if(step==1){Check(game.ScoutingPractice && game.CurrentPracticeBattle.Layout.Id==2 && game.CurrentPracticeBattle.TotalBuildings==4 && !game.State.HasCampaignRun,"Crossfire scouting snapshot");Check(GameObject.Find("Practice Scout Text").GetComponent<Text>().text.Contains("4 buildings | 3 defenses"),"Dynamic scouting counts");Capture("crossfire-scout.png");game.BeginPracticeAttack();for(int i=0;i<8;i++)game.DeployPracticeRaider(i%3);Click("Select Battle Tanks");Check(game.TryDeployPracticeAtScreen(game.viewCamera.WorldToScreenPoint(new Vector3(0,0,-10.5f))),"Ground input uses new zone");for(int i=0;i<3;i++)game.DeployPracticeRaider(i);Click("Select Battle Archers");for(int i=0;i<4;i++)game.DeployPracticeRaider(i%3);while(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)game.CurrentPracticeBattle.Step();SessionState.SetInt("EnemyLayout.Step",2);return;}
            if(step==2){if(game.State.campaignOutcome==PracticeOutcome.Running)return;Check(game.State.campaignOutcome==PracticeOutcome.Victory,"Crossfire rendered victory saved");game.WatchPracticeReplay();SessionState.SetInt("EnemyLayout.Step",3);return;}
            if(step==3){if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;Check(game.PracticeReplayMatches && game.CurrentPracticeBattle.Layout.Id==2,"Rendered replay preserves new layout");game.ClosePracticeBattle();game.OpenCampaign();Click("Resolve Campaign");Check(game.State.CampaignCleared(2) && !game.State.HasCampaignRun,"Crossfire UI reward");game.OpenArmyPreparation();Click("Fill Army");game.OpenCampaign();Click("Campaign Hillfort");SessionState.SetInt("EnemyLayout.Step",4);return;}
            Check(game.ScoutingPractice && game.CurrentPracticeBattle.Layout.Id==3 && game.CurrentPracticeBattle.TotalBuildings==5 && GameObject.Find("Practice GoldStorage")!=null,"Hillfort scout/model");Check(GameObject.Find("Deployment Zone").GetComponent<LineRenderer>().sharedMaterial!=null && GameObject.Find("Deployment Zone").GetComponent<LineRenderer>().sharedMaterial.color.g>.9f,"Outline material survives mission switch");Capture("hillfort-scout.png");int army=game.State.ArmyCount;game.ClosePracticeBattle();Check(game.State.ArmyCount==army && !game.State.HasCampaignRun,"Return from new scout preserves roster");
            Finish("PASS: four immutable authored layouts; isolated battle copies; layout-specific bounds/corners; both new mixed-army victories and every-tick replay equality; same-enclosure wrong-layout rejection; mission locks; pending result reload; full-storage deferral; once-only rewards and repeat-clear protection; four-mission save progress; invalid masks; storage victory/star accounting; real campaign menu, dynamic scouting, ground/lane deployment, result/replay/claim, next mission unlock and home return. Unity "+Application.unityVersion+". No external layout import, phone testing or APK rebuild.",0);
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"EnemyLayoutValidation.txt"),message);EditorApplication.Exit(code);}
}
