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
public static class CampaignValidation
{
    const string Pending="CampaignValidation.Pending";
    static CampaignValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static PracticeBattle Win(bool sealedWalls,int army=8)
    {
        var battle=new PracticeBattle(sealedWalls,army);
        for(int i=0;i<army;i++)Check(battle.Deploy(i%3),"Campaign deployment");
        Check(!battle.Deploy(0),"Owned army deployment cap");
        for(int i=0;i<PracticeBattle.TimeLimitTicks && battle.Outcome==PracticeOutcome.Running;i++)battle.Step();
        Check(battle.Outcome==PracticeOutcome.Victory,"Campaign mission winnable");return battle;
    }
    static void Rules()
    {
        var s=VillageState.Create(1000);
        Check(s.IsValid() && !s.TryBeginCampaign(0,out _),"Empty army cannot attack");
        Check(s.TrySetArmyCount("Raider",8,out _),"Prepare starter roster");
        string legacy=JsonUtility.ToJson(s).Replace("\"version\":6","\"version\":4");
        Check(VillageState.TryDeserialize(legacy,out var migrated) && migrated.version==6 && migrated.ArmyHousing==8 && !migrated.HasCampaignRun && migrated.campaignCleared==0,"v4 roster retained");
        PlayerPrefs.DeleteKey("Kingdoms.Village.pre-v5");PlayerPrefs.SetString(VillageSave.Key,legacy);
        Check(VillageSave.TryLoad(out _,out _) && PlayerPrefs.GetString("Kingdoms.Village.pre-v5")==legacy,"v4 original backup");
        Check(!s.TryBeginCampaign(1,out _) && s.ArmyHousing==8,"Locked mission preserves roster");
        Check(s.TryBeginCampaign(0,out _) && s.ArmyHousing==0 && s.campaignArmy==8,"Start consumes whole roster");
        string run=s.campaignRunId;
        Check(!s.TryBeginCampaign(0,out _) && s.campaignRunId==run,"Cannot replace active attempt");
        Check(!s.TryClaimCampaign(run,out _),"Cannot claim active battle");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out s) && s.HasCampaignRun && s.campaignArmy==8,"Interrupted run persists");
        Check(!s.TryAbandonCampaign("stale",out _) && s.TryAbandonCampaign(run,out _) && s.ArmyHousing==0,"Abandon keeps troops spent");
        Check(s.TrySetArmyCount("Raider",8,out _) && s.TryBeginCampaign(0,out _),"Prepare again free");run=s.campaignRunId;
        var win=Win(false);
        Check(!s.TryFinishCampaign("stale",win.Record(),out _),"Wrong ticket rejected");
        var wrongArmy=new PracticeBattle(false,7);wrongArmy.Surrender();
        Check(!s.TryFinishCampaign(run,wrongArmy.Record(),out _),"Wrong roster rejected");
        var tampered=Win(false);tampered.Raiders[0].HitPoints=1;
        Check(!s.TryFinishCampaign(run,tampered.Record(),out _),"Unreproducible result rejected");
        Check(s.TryFinishCampaign(run,win.Record(),out _) && s.campaignStars==3,"Verified victory recorded");
        Check(!s.TryFinishCampaign(run,win.Record(),out _),"Result cannot overwrite completed attempt");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out s) && s.campaignOutcome==PracticeOutcome.Victory,"Pending victory survives restart");
        s.gold=s.GoldCapacity;string full=JsonUtility.ToJson(s);
        Check(!s.TryClaimCampaign(run,out _) && JsonUtility.ToJson(s)==full,"Full storage preserves full reward");
        s.gold=1000;int gold=s.gold,elixir=s.elixir;
        Check(s.TryClaimCampaign(run,out _) && s.gold==gold+500 && s.elixir==elixir+300 && s.CampaignUnlocked(1) && !s.HasCampaignRun,"First clear settles reward and progress together");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out s) && !s.TryClaimCampaign(run,out _) && s.gold==gold+500,"Duplicate after reload cannot pay twice");
        Check(s.TrySetArmyCount("Raider",8,out _) && s.TryBeginCampaign(0,out _),"Replay cleared mission");run=s.campaignRunId;gold=s.gold;
        Check(s.TryFinishCampaign(run,win.Record(),out _) && s.TryClaimCampaign(run,out _) && s.gold==gold,"Repeat clear gives no resources");
        Check(s.TrySetArmyCount("Raider",8,out _) && s.TryBeginCampaign(1,out _),"Second mission unlock");run=s.campaignRunId;
        Check(!s.TryFinishCampaign(run,win.Record(),out _),"Wrong layout rejected");
        var surrender=new PracticeBattle(true);surrender.Surrender();
        Check(s.TryFinishCampaign(run,surrender.Record(),out _) && s.TryClaimCampaign(run,out _) && !s.CampaignCleared(1) && s.gold==gold,"Surrender gives no reward");
        s.TrySetArmyCount("Raider",8,out _);s.TryBeginCampaign(1,out _);run=s.campaignRunId;
        Check(s.TryFinishCampaign(run,Win(true).Record(),out _) && s.TryClaimCampaign(run,out _) && s.campaignCleared==3 && s.gold==gold+1000,"Second mission reward");
        var large=Win(false,80);var replay=new PracticeReplay(large.Record());
        while(!replay.Finished)replay.Step();Check(replay.Matches && replay.Battle.ArmyBudget==80,"Expanded army replay budget");
        var invalid=s.Copy();invalid.campaignCleared=2;Check(!invalid.IsValid(),"Invalid unlock sequence rejected");
        invalid=s.Copy();invalid.campaignRunId="invalid";Check(!VillageState.TryDeserialize(JsonUtility.ToJson(invalid),out _),"Malformed run rejected");
        foreach(int count in new[]{0,81}){bool threw=false;try{new PracticeBattle(false,count);}catch(ArgumentOutOfRangeException){threw=true;}Check(threw,"Invalid combat budget rejected");}
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")
            throw new InvalidOperationException("Use isolated validation project and identity.");
        try
        {
            Rules();EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            var seed=VillageState.Create(VillageState.Now);seed.TrySetArmyCount("Raider",8,out _);
            Check(VillageSave.TryWrite(seed,out _) && PlayerProfile.TrySaveName("CampaignChief",out _),"Seed isolated UI");
            SessionState.SetInt("CampaignValidation.Step",0);SessionState.SetString("CampaignValidation.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,1600,702});
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("CampaignValidation.Start","0")))).TotalSeconds<180,"Play timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("CampaignValidation.Step",0);
            if(step==0)
            {
                game.OpenArmyPreparation();Click("Open Campaign");Capture("campaign-menu.png");Click("Campaign Gate");
                Check(game.CampaignBattleOpen && game.ScoutingPractice && game.State.ArmyHousing==8 && !game.State.HasCampaignRun,"Scouting preserves roster");
                Click("Return From Practice");Check(game.State.ArmyHousing==8,"Scouting return costs nothing");
                game.OpenCampaign();Click("Campaign Gate");Click("Begin Practice Attack");
                Check(game.State.HasCampaignRun && game.State.ArmyHousing==0 && game.CurrentPracticeBattle.ArmyBudget==8,"Start commits owned roster");
                Check(VillageSave.TryLoad(out var saved,out _) && saved.HasCampaignRun && saved.ArmyHousing==0,"Roster commitment saved before deployment");
                for(int i=0;i<8;i++)Click("Deploy Raider "+i%3);
                for(int i=0;i<1800 && game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running;i++)game.CurrentPracticeBattle.Step();
                SessionState.SetInt("CampaignValidation.Step",1);return;
            }
            if(step==1)
            {
                if(game.State.campaignOutcome==PracticeOutcome.Running)return;
                Check(game.State.campaignOutcome==PracticeOutcome.Victory && game.State.gold==1000,"Victory saved before reward claim");Capture("campaign-result.png");
                Click("Watch Practice Replay");Check(game.WatchingPracticeReplay,"Campaign replay starts");
                SessionState.SetInt("CampaignValidation.Step",2);return;
            }
            if(step==2)
            {
                if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;
                Check(game.PracticeReplayMatches && game.State.gold==1000,"Campaign replay reproduces without rewards");
                Click("Return From Practice");SessionState.SetInt("CampaignValidation.Step",3);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==3)
            {
                if(game.PracticeOpen)return;
                Check(game.State.campaignOutcome==PracticeOutcome.Victory && game.State.HasCampaignRun,"Unclaimed UI result survives reload");
                game.OpenCampaign();Click("Resolve Campaign");Check(game.State.gold==1500 && game.State.elixir==800 && game.State.CampaignCleared(0),"Claim UI pays once");
                Capture("campaign-claimed.png");
                Check(VillageSave.TryLoad(out var saved,out _) && saved.CampaignCleared(0) && !saved.HasCampaignRun,"Claim persisted");
                game.CloseProfile();game.SelectBuilding(0);game.OpenBuildingDetails();Click("Confirm Upgrade");
                Check(game.State.BusyBuilders==1 && game.State.gold==500,"Reward can fund village upgrade");
                game.CloseBuildingDetails();game.OpenArmyPreparation();Click("Fill Army");Click("Open Campaign");Click("Campaign Keep");Click("Begin Practice Attack");Click("Surrender Practice");
                Check(game.State.campaignOutcome==PracticeOutcome.Surrendered && game.State.gold==500,"Loss result gives no currency");
                Click("Claim Campaign Battle");Check(!game.State.HasCampaignRun && !game.State.CampaignCleared(1),"Dismiss loss UI");
                Click("Return From Practice");game.OpenPracticeBattle();Check(game.CurrentPracticeBattle.ArmyBudget==8 && !game.CampaignBattleOpen,"Practice remains independent");game.ClosePracticeBattle();
                Finish("PASS: v4-to-v6 roster migration and backup; empty/locked/duplicate start guards; durable whole-roster commitment; interrupted abandonment; recording verification, ticket/layout/army mismatch rejection; full-storage reward preservation; once-only first clear after reload; repeat/loss policy; both missions winnable; 80-unit deterministic replay; malformed saves/budgets; real campaign menu/scout/start/deploy/results/replay; pending-result scene reload and claim; reward-funded Town Hall upgrade; free re-preparation, second mission surrender/dismiss; practice isolation. Unity "+Application.unityVersion+". Fixed two-layout campaign; no server authority, persisted replay, APK or phone validation.",0);
            }
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string result,int code)
    {SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"CampaignValidation.txt"),result);EditorApplication.Exit(code);}
}
