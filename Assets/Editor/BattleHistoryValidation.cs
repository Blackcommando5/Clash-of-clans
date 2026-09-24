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
public static class BattleHistoryValidation
{
    const string Pending = "BattleHistory.Pending";
    static BattleHistoryValidation() { EditorApplication.update += Tick; }
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Click(string name) => GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static string Row(int index=0) => GameObject.Find("Battle History Text " + index).GetComponent<Text>().text;
    static void Begin(VillageState state, int count=8)
    {
        Check(state.TrySetArmyCount("Raider",count,out _) && state.TryBeginCampaign(0,out _), "Prepare and commit roster");
    }
    static PracticeBattle Victory()
    {
        var battle=new PracticeBattle();
        for(int i=0;i<8;i++)battle.Deploy(i%3);
        while(battle.Outcome==PracticeOutcome.Running)battle.Step();
        Check(battle.Outcome==PracticeOutcome.Victory,"Victory fixture");return battle;
    }
    static void Invalid(VillageState source, Action<CampaignHistoryEntry> edit)
    {
        var bad=source.Copy();edit(bad.battleHistory[0]);
        Check(!VillageState.TryDeserialize(JsonUtility.ToJson(bad),out _),"Malformed history rejected");
    }
    static void Rules()
    {
        var s=VillageState.Create(1000);Check(s.battleHistory.Count==0 && s.IsValid(),"Fresh empty history");
        Begin(s);string run=s.campaignRunId;var win=Victory();
        Check(!s.TryFinishCampaign("stale",win.Record(),out _) && s.battleHistory.Count==0,"Rejected result creates no history");
        Check(s.TryFinishCampaign(run,win.Record(),out _) && s.battleHistory.Count==1,"Verified result recorded");
        var entry=s.battleHistory[0];
        Check(entry.runId==run && entry.outcome==PracticeOutcome.Victory && entry.stars==3 && entry.destruction==100 &&
            entry.durationTicks==win.Tick && entry.completedUtc>0 && entry.raiders==8 && !entry.resolved && entry.goldAwarded==0,"Verified detail and pending status");
        Check(!s.TryFinishCampaign(run,win.Record(),out _) && s.battleHistory.Count==1,"Repeated finish cannot duplicate history");
        var missing=s.Copy();missing.battleHistory.Clear();Check(!missing.IsValid(),"Completed v9 attack requires matching history");
        Check(VillageSave.TryWrite(s,out _) && VillageSave.TryLoad(out s,out _) && s.battleHistory[0].runId==run,"Pending history saved and loaded");
        var copy=s.Copy();copy.battleHistory[0].stars=0;Check(s.battleHistory[0].stars==3,"Copy isolates history entries");
        s.gold=s.GoldCapacity;string before=JsonUtility.ToJson(s);
        Check(!s.TryClaimCampaign(run,out _) && before==JsonUtility.ToJson(s),"Storage-blocked claim leaves history unchanged");
        s.gold=1000;Check(s.TryClaimCampaign(run,out _) && s.battleHistory.Count==1 && s.battleHistory[0].resolved &&
            s.battleHistory[0].goldAwarded==500 && s.battleHistory[0].elixirAwarded==300,"Claim updates original entry with actual payout");
        Check(VillageSave.TryWrite(s,out _) && VillageSave.TryLoad(out s,out _) && !s.TryClaimCampaign(run,out _) && s.gold==1500,"Claimed history reload and duplicate payout rejection");
        Invalid(s,e=>e.runId="bad");Invalid(s,e=>e.mission=99);Invalid(s,e=>e.stars=4);Invalid(s,e=>e.raiders=int.MaxValue);
        Invalid(s,e=>e.completedUtc=long.MaxValue);Invalid(s,e=>e.durationTicks=1801);Invalid(s,e=>e.destruction=101);
        Invalid(s,e=>e.goldAwarded=1);Invalid(s,e=>e.resolved=false);Invalid(s,e=>e.abandoned=true);
        var bad=s.Copy();bad.battleHistory.Add(bad.battleHistory[0]);Check(!bad.IsValid(),"Duplicate run IDs rejected");
        bad=s.Copy();bad.battleHistory=null;Check(!bad.IsValid(),"Null history rejected");
        Begin(s);run=s.campaignRunId;Check(s.TryFinishCampaign(run,win.Record(),out _) && s.TryClaimCampaign(run,out _) && s.battleHistory[0].goldAwarded==0,"Repeat victory records no reward");
        foreach(var outcome in new[]{PracticeOutcome.Surrendered,PracticeOutcome.Timeout,PracticeOutcome.Defeat})
        {
            Begin(s,1);var battle=new PracticeBattle(false,1);
            if(outcome==PracticeOutcome.Surrendered)battle.Surrender();
            else { if(outcome==PracticeOutcome.Defeat)battle.Deploy(1);while(battle.Outcome==PracticeOutcome.Running)battle.Step(); }
            Check(battle.Outcome==outcome && s.TryFinishCampaign(s.campaignRunId,battle.Record(),out _),"Record "+outcome);
            Check(s.battleHistory[0].outcome==outcome && s.TryClaimCampaign(s.campaignRunId,out _) && s.battleHistory[0].goldAwarded==0,"Resolve "+outcome);
        }
        Begin(s);run=s.campaignRunId;
        Check(!s.TryAbandonCampaign("stale",out _) && s.TryAbandonCampaign(run,out _) && s.battleHistory[0].abandoned && s.battleHistory[0].durationTicks==-1 && s.battleHistory[0].resolved,"Abandoned entry does not invent combat details");
        string newest="";
        for(int i=0;i<25;i++){Begin(s);newest=s.campaignRunId;Check(s.TryAbandonCampaign(newest,out _),"Abandon for retention");}
        Check(s.battleHistory.Count==20 && s.battleHistory[0].runId==newest && !s.battleHistory.Exists(e=>e.runId==run) && s.IsValid(),"Bounded newest-first retention");
        Check(VillageSave.TryWrite(s,out _) && VillageSave.TryLoad(out s,out _) && s.battleHistory.Count==20,"Bounded history reload");

        var mixed=VillageState.Create(1000);mixed.buildings.Add(new PlacedBuilding{kind="Barracks",x=5,z=5,level=2});
        Check(mixed.TrySetArmyCount("Raider",2,out _) && mixed.TrySetArmyCount("Archer",1,out _) && mixed.TrySetArmyCount("Tank",1,out _) && mixed.TryBeginCampaign(0,out _),"Mixed roster fixture");
        var mixedBattle=new PracticeBattle(false,4,1,1);mixedBattle.Surrender();
        Check(mixed.TryFinishCampaign(mixed.campaignRunId,mixedBattle.Record(),out _) && VillageSave.TryWrite(mixed,out _) && VillageSave.TryLoad(out mixed,out _) &&
            mixed.battleHistory[0].raiders==2 && mixed.battleHistory[0].archers==1 && mixed.battleHistory[0].tanks==1,"All committed types recorded even without deployment");

        var legacy=VillageState.Create(1000);legacy.tutorialHintsPaused=true;Begin(legacy);
        Check(legacy.TryFinishCampaign(legacy.campaignRunId,win.Record(),out _),"Legacy pending seed");
        legacy.version=8;legacy.battleHistory=null;string old=JsonUtility.ToJson(legacy);
        PlayerPrefs.DeleteKey("Kingdoms.Village.pre-v9");PlayerPrefs.SetString(VillageSave.Key,old);
        Check(VillageSave.TryLoad(out var migrated,out _) && migrated.version==9 && migrated.battleHistory.Count==1 &&
            migrated.battleHistory[0].durationTicks==-1 && migrated.battleHistory[0].completedUtc==0 && migrated.tutorialHintsPaused &&
            PlayerPrefs.GetString("Kingdoms.Village.pre-v9")==old,"v8 pending migration preserves preferences and backup, marks unavailable details");
        Check(migrated.TryClaimCampaign(migrated.campaignRunId,out _) && migrated.battleHistory[0].goldAwarded==500,"Legacy pending reward remains claimable");
        legacy=VillageState.Create(1000);legacy.version=8;
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(legacy),out migrated) && migrated.battleHistory.Count==0,"v8 fresh migration does not invent attacks");
        legacy.version=9;Begin(legacy);legacy.version=8;
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(legacy),out migrated) && migrated.battleHistory.Count==0 && migrated.HasCampaignRun &&
            migrated.TryAbandonCampaign(migrated.campaignRunId,out _) && migrated.battleHistory.Count==1,"v8 interrupted migration retains attack until abandonment");
        for(int version=1;version<=8;version++)
        {
            var oldState=VillageState.Create(1000);oldState.version=version;
            Check(VillageState.TryDeserialize(JsonUtility.ToJson(oldState),out migrated) && migrated.version==VillageState.SaveVersion && migrated.battleHistory.Count==0 && migrated.gold==oldState.gold,"Legacy migration generation "+version);
        }
        // Existing rules exercise mixed rosters, all migration generations, rewards, tutorials and economy.
        foreach(var type in new[]{typeof(CampaignValidation),typeof(MixedArmyValidation),typeof(TankValidation),typeof(TutorialValidation)})
            type.GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(ResourceMilestoneValidation).GetMethod("StateChecks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(ResourceMilestoneValidation).GetMethod("ProgressionChecks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")
            throw new InvalidOperationException("Use isolated validation project and identity.");
        try
        {
            Rules();EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            Check(VillageSave.TryWrite(VillageState.Create(VillageState.Now),out _) && PlayerProfile.TrySaveName("HistoryChief",out _),"Seed UI");
            SessionState.SetInt("BattleHistory.Step",0);SessionState.SetString("BattleHistory.Start",DateTime.UtcNow.Ticks.ToString());
            SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name,int width=1600,int height=702) => typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height});
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("BattleHistory.Start","0")))).TotalSeconds<180,"UI timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("BattleHistory.Step",0);
            if(step==0)
            {
                game.OpenCampaign();Click("Open Battle History");Check(Row().Contains("No campaign results"),"Empty history UI");Capture("history-empty.png");
                Click("History Back");Click("Campaign Prepare Army");Click("Fill Army");Click("Open Campaign");Click("Campaign Gate");game.BeginPracticeAttack();
                for(int i=0;i<8;i++)game.DeployPracticeRaider(i%3);
                while(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)game.CurrentPracticeBattle.Step();
                SessionState.SetInt("BattleHistory.Step",1);return;
            }
            if(step==1)
            {
                if(game.State.campaignOutcome==PracticeOutcome.Running)return;
                Check(game.State.battleHistory.Count==1,"Live battle saves history");game.ClosePracticeBattle();game.OpenCampaign();Click("Open Battle History");
                Check(Row().Contains("Reward pending") && Row().Contains("100% destruction"),"Pending details UI");Capture("history-pending.png");
                Click("History Back");Click("Resolve Campaign");Click("Open Battle History");
                Check(Row().Contains("Claimed: 500 gold + 300 elixir"),"Claim refreshes history");Capture("history-claimed.png");
                Check(game.State.battleHistory.Count==1,"Viewing history never duplicates entries");
                game.CloseProfile();SessionState.SetInt("BattleHistory.Step",2);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==2)
            {
                game.OpenBattleHistory();Check(Row().Contains("Claimed: 500 gold") && game.State.battleHistory.Count==1,"Claim history survives scene reload");
                game.CloseProfile();for(int i=0;i<3;i++){Begin(game.State);game.State.TryAbandonCampaign(game.State.campaignRunId,out _);}
                Check(VillageSave.TryWrite(game.State,out _),"Save paging fixtures");game.OpenBattleHistory();
                Check(Row().Contains("Abandoned") && !GameObject.Find("History Newer").GetComponent<Button>().interactable,"Newest page");
                Click("History Older");Check(Row(1).Contains("Claimed: 500 gold") && !GameObject.Find("History Older").GetComponent<Button>().interactable,"Older page and terminal boundary");
                Capture("history-older.png");Capture("history-older-16x9.png",1280,720);
                Click("History Newer");Check(Row().Contains("Abandoned"),"Newer navigation");Click("History Back");Check(GameObject.Find("Campaign Gate")!=null,"Return to campaign");
                game.CloseProfile();Check(!game.ProfileOpen && !game.cameraController.InputBlocked,"Close releases camera");
                Finish("PASS: verified completion details; pending/save/reload/claim continuity; duplicate finish and payout rejection; storage-full atomicity; deep-copy isolation; malformed history rejection; repeat wins, defeat, surrender, timeout and abandoned attacks; newest-first 20-entry retention and reload; v8 fresh/interrupted/pending migration and original backup; campaign, mixed army, Tank, tutorial and resource/progression rule regressions; live empty/history entry, campaign victory, pending and claimed UI, scene reload, paging boundaries, return and camera release. Screenshots 1600x702 and 1280x720. Unity "+Application.unityVersion+". Physical phone and rebuilt APK pending. History summaries only; replays remain in memory.",0);
            }
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code)
    {
        SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"BattleHistoryValidation.txt"),message);EditorApplication.Exit(code);
    }
}
